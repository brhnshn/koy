using System.Windows;
using System.Windows.Interop;
using Koy.App.Features.Music;
using Koy.Core.Notch;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Koy.Core.Visibility;
using Koy.Platform.Media;
using Koy.Platform.Shell;

namespace Koy.App.Features.Notch;

public sealed class NotchFeature(AppSettings settings) : IFeature
{
    private NotchWindow? _window;
    private MediaService? _mediaService;
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

        _mediaService = new MediaService();
        _ = _mediaService.StartAsync();

        _windowTracker = new WindowTracker();
        _fullscreenDetector = new FullscreenDetector();
        _visibilityController = new VisibilityController(settings.NotchVisibility);

        var controller = new NotchController();
        var musicVm = new MusicViewModel(_mediaService);
        _viewModel = new NotchViewModel(settings, controller, musicVm, _visibilityController);

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

        // Görünür değilken (veya kenar gözetleme farenin ayrılmasını beklerken) ve tam ekran değilken 100 ms aralıkla kontrol (AGENTS.md #5)
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
            // Çentik yatay sınırları (30 px tolerans payı ile)
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
    }
}
