using System.Windows;
using System.Windows.Interop;
using Koy.App.Features.ControlCenter;
using Koy.App.Features.Music;
using Koy.App.Features.Status;
using Koy.Core.Notch;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Koy.Core.Visibility;
using Koy.Platform.Audio;
using Koy.Platform.Display;
using Koy.Platform.Media;
using Koy.Platform.Power;
using Koy.Platform.Radio;
using Koy.Platform.Shell;
using Koy.Platform.Weather;

namespace Koy.App.Features.Notch;

public sealed class NotchFeature(AppSettings settings) : IFeature
{
    private NotchWindow? _window;
    private MediaService? _mediaService;
    private AudioService? _audioService;
    private RadioService? _radioService;
    private BrightnessService? _brightnessService;
    private PowerService? _powerService;
    private WeatherService? _weatherService;
    private FocusAssistService? _focusAssistService;
    private WindowTracker? _windowTracker;
    private FullscreenDetector? _fullscreenDetector;
    private VisibilityController? _visibilityController;
    private NotchViewModel? _viewModel;
    private ITimer? _edgePeekTimer;
    private nint _windowHwnd;

    public void Start()
    {
        if (_window is not null)
            return;

        // Platform servisleri başlatılır
        _mediaService = new MediaService();
        _ = _mediaService.StartAsync();

        _audioService = new AudioService();

        _radioService = new RadioService();
        _ = _radioService.StartAsync();

        _brightnessService = new BrightnessService();
        _powerService = new PowerService();
        _weatherService = new WeatherService();
        _focusAssistService = new FocusAssistService();

        _windowTracker = new WindowTracker();
        _fullscreenDetector = new FullscreenDetector();
        _visibilityController = new VisibilityController(settings.NotchVisibility);

        // Denetleyici ve etkin paneller
        var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Music, NotchPanel.ControlCenter, NotchPanel.Status]);

        // Panel ViewModel'leri
        var musicVm = new MusicViewModel(_mediaService);
        var controlCenterVm = new ControlCenterViewModel(
            _audioService,
            _radioService,
            _brightnessService,
            _focusAssistService,
            settings);
        var statusVm = new StatusViewModel(_powerService, _weatherService, settings);

        _viewModel = new NotchViewModel(
            settings,
            controller,
            musicVm,
            controlCenterVm,
            statusVm,
            _visibilityController);

        _window = new NotchWindow { DataContext = _viewModel };
        _window.SourceInitialized += OnWindowSourceInitialized;
        _window.Show();

        // 100 ms kenar gözetleme (peek) zamanlayıcısı; yalnız gizliyken çalışır (AGENTS.md #5)
        _edgePeekTimer = TimeProvider.System.CreateTimer(
            _ => CheckEdgePeek(),
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);

        _visibilityController.Changed += OnVisibilityChanged;
        _windowTracker.ForegroundWindowChanged += OnForegroundWindowChanged;
        _windowTracker.Start();

        UpdateEdgeTimer();
    }

    private void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        if (_window is null || _visibilityController is null || _fullscreenDetector is null)
            return;

        _windowHwnd = new WindowInteropHelper(_window).Handle;
        _visibilityController.NotchHandle = _windowHwnd;
        _fullscreenDetector.NotchHandle = _windowHwnd;

        var source = PresentationSource.FromVisual(_window);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        var screenWidth = SystemParameters.PrimaryScreenWidth * dpiX;
        var notchWidth = 420 * dpiX;
        var notchHeight = 140 * dpiY;
        var left = (int)((screenWidth - notchWidth) / 2);
        var right = (int)(left + notchWidth);

        _visibilityController.NotchBounds = new WindowRect(left, 0, right, (int)notchHeight);
    }

    private void OnForegroundWindowChanged(object? sender, WindowInfo info)
    {
        if (_visibilityController is null || _fullscreenDetector is null)
            return;

        var isFullscreen = _fullscreenDetector.IsFullscreen();
        _visibilityController.SetFullscreen(isFullscreen);
        _visibilityController.UpdateForegroundWindow(info.Handle, info.Bounds);

        if (!isFullscreen && _windowHwnd != 0 && info.Handle != _windowHwnd)
        {
            WindowStyles.ReassertTopmost(_windowHwnd);
        }
    }

    private void OnVisibilityChanged(object? sender, VisibilityResult result)
    {
        UpdateEdgeTimer();
    }

    private void UpdateEdgeTimer()
    {
        if (_visibilityController is null || _edgePeekTimer is null)
            return;

        var shouldRun = (!_visibilityController.IsVisible || _visibilityController.IsMouseAtEdge) &&
                        !_visibilityController.IsFullscreen &&
                        _visibilityController.Mode != VisibilityMode.Fixed;

        if (shouldRun)
        {
            _edgePeekTimer.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }
        else
        {
            _edgePeekTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    private void CheckEdgePeek()
    {
        if (_visibilityController is null || _windowTracker is null)
            return;

        if (_visibilityController.IsFullscreen)
            return;

        if (_windowTracker.GetCursorPosition(out var x, out var y))
        {
            var bounds = _visibilityController.NotchBounds;
            var minX = bounds.Left - 30;
            var maxX = bounds.Right + 30;
            var isAtEdge = y >= 0 && y <= 2 && (bounds.IsEmpty || (x >= minX && x <= maxX));

            if (isAtEdge)
            {
                _visibilityController.SetMouseAtEdge(true);
            }
            else if (_visibilityController.IsMouseAtEdge && !_visibilityController.IsMouseOver)
            {
                _visibilityController.SetMouseAtEdge(false);
            }
        }
    }

    public void Stop()
    {
        _edgePeekTimer?.Dispose();
        _edgePeekTimer = null;

        if (_visibilityController is not null)
            _visibilityController.Changed -= OnVisibilityChanged;

        if (_windowTracker is not null)
        {
            _windowTracker.ForegroundWindowChanged -= OnForegroundWindowChanged;
            _windowTracker.Dispose();
            _windowTracker = null;
        }

        _window?.Close();
        _window = null;
        _windowHwnd = 0;

        _viewModel?.Dispose();
        _viewModel = null;

        _visibilityController?.Dispose();
        _visibilityController = null;

        _fullscreenDetector = null;

        _mediaService?.Dispose();
        _mediaService = null;

        _audioService?.Dispose();
        _audioService = null;

        _radioService?.Dispose();
        _radioService = null;

        _brightnessService?.Dispose();
        _brightnessService = null;

        _powerService?.Dispose();
        _powerService = null;

        _weatherService?.Dispose();
        _weatherService = null;

        _focusAssistService?.Dispose();
        _focusAssistService = null;
    }
}
