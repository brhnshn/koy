using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Koy.App.Features.Calendar;
using Koy.App.Features.ControlCenter;
using Koy.App.Features.Music;
using Koy.App.Features.Shelf;
using Koy.App.Features.Status;
using Koy.Core.Display;
using Koy.Core.Notch;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Koy.Core.Visibility;
using Koy.Platform.Audio;
using Koy.Platform.Display;
using Koy.Platform.Media;
using Koy.Platform.Power;
using Koy.Platform.Radio;
using Koy.Platform.Shelf;
using Koy.Platform.Shell;
using Koy.Platform.Weather;

namespace Koy.App.Features.Notch;

public sealed class NotchFeature(AppSettings settings) : IFeature
{
    private sealed class NotchWindowState : IDisposable
    {
        public required DisplayMonitor Monitor { get; set; }
        public required NotchWindow Window { get; init; }
        public required NotchViewModel ViewModel { get; init; }
        public required VisibilityController VisibilityController { get; init; }
        public required NotchController Controller { get; init; }
        public nint Hwnd { get; set; }

        public void Dispose()
        {
            Window.Close();
            ViewModel.Dispose();
            Controller.Dispose();
            VisibilityController.Dispose();
        }
    }

    private readonly Dictionary<string, NotchWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NotchWindowState> _windowStates = new(StringComparer.OrdinalIgnoreCase);

    private MediaService? _mediaService;
    private PowerService? _powerService;
    private WeatherService? _weatherService;
    private FocusAssistService? _focusAssistService;
    private ShelfService? _shelfService;
    private WindowTracker? _windowTracker;
    private FullscreenDetector? _fullscreenDetector;
    private MonitorService? _monitorService;
    private DispatcherTimer? _edgePeekTimer;

    public void Start()
    {
        if (_windows.Count > 0)
            return;

        // Platform servisleri başlatılır (paylaşılan tekil servisler)
        _mediaService = new MediaService();
        _ = _mediaService.StartAsync();

        _powerService = new PowerService();
        _weatherService = new WeatherService();
        _focusAssistService = new FocusAssistService();
        _shelfService = new ShelfService();

        _windowTracker = new WindowTracker();
        _fullscreenDetector = new FullscreenDetector();
        _monitorService = new MonitorService();
        _monitorService.Start();
        _monitorService.MonitorsChanged += OnMonitorsChanged;

        var monitors = _monitorService.GetMonitors();
        var targetMonitors = DisplayLayoutResolver.ResolveTargetMonitors(monitors, settings.MonitorTarget);

        foreach (var monitor in targetMonitors)
        {
            CreateNotchWindow(monitor);
        }

        // 100 ms kenar gözetleme (peek) zamanlayıcısı; yalnız gizliyken çalışır (AGENTS.md #5).
        // DispatcherTimer UI thread'de tikler: eskiden ThreadPool zamanlayıcısıydı ve CheckEdgePeek
        // pencere/durum nesnelerine UI dışından dokunup VerifyAccess çökmesine yol açıyordu.
        _edgePeekTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _edgePeekTimer.Tick += (_, _) => CheckEdgePeek();

        _windowTracker.ForegroundWindowChanged += OnForegroundWindowChanged;
        _windowTracker.Start();

        UpdateEdgeTimer();
    }

    private void CreateNotchWindow(DisplayMonitor monitor)
    {
        var controller = new NotchController();
        controller.SetEnabledPanels(ParseEnabledPanels(settings.EnabledPanels));

        var visibilityController = new VisibilityController(settings.NotchVisibility);

        var musicVm = new MusicViewModel(_mediaService!);
        var controlCenterVm = new ControlCenterViewModel(
            () => new AudioService(),
            () => new RadioService(),
            () => new BrightnessService(),
            _focusAssistService!,
            settings);
        var statusVm = new StatusViewModel(_powerService!, _weatherService!, settings);
        var calendarVm = new CalendarViewModel(settings);
        var shelfVm = new ShelfViewModel(_shelfService!);

        var viewModel = new NotchViewModel(
            settings,
            controller,
            musicVm,
            controlCenterVm,
            statusVm,
            calendarVm,
            shelfVm,
            visibilityController);

        var window = new NotchWindow { DataContext = viewModel };
        window.SetTargetMonitor(monitor);

        var state = new NotchWindowState
        {
            Monitor = monitor,
            Window = window,
            ViewModel = viewModel,
            VisibilityController = visibilityController,
            Controller = controller
        };

        window.SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            state.Hwnd = hwnd;
            visibilityController.NotchHandle = hwnd;
            _fullscreenDetector?.IgnoredHandles.Add(hwnd);
            UpdateNotchBounds(window, visibilityController, monitor);
        };

        window.DisplayChangeHandler = () =>
        {
            _monitorService?.NotifyDisplayChanged();
        };

        visibilityController.Changed += OnVisibilityChanged;

        _windowStates[monitor.Id] = state;
        _windows[monitor.Id] = window;

        window.Show();
        UpdateNotchBounds(window, visibilityController, monitor);
    }

    private void OnMonitorsChanged(object? sender, EventArgs e)
    {
        if (Application.Current is not null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(() => OnMonitorsChanged(sender, e));
            return;
        }

        if (_monitorService is null) return;

        var currentMonitors = _monitorService.GetMonitors();
        var targetMonitors = DisplayLayoutResolver.ResolveTargetMonitors(currentMonitors, settings.MonitorTarget);
        var targetKeys = new HashSet<string>(targetMonitors.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);

        // 1. Hedefte olmayan veya bağlantısı kesilen monitörlerin pencerelerini kapat
        var toRemove = _windows.Keys.Where(k => !targetKeys.Contains(k)).ToList();
        foreach (var key in toRemove)
        {
            if (_windowStates.Remove(key, out var state))
            {
                state.VisibilityController.Changed -= OnVisibilityChanged;
                if (state.Hwnd != 0)
                {
                    _fullscreenDetector?.IgnoredHandles.Remove(state.Hwnd);
                }
                _windows.Remove(key);
                state.Dispose();
            }
        }

        // 2. Yeni monitörler için pencere oluştur veya mevcutların konumunu güncelle
        foreach (var monitor in targetMonitors)
        {
            if (_windowStates.TryGetValue(monitor.Id, out var existingState))
            {
                existingState.Monitor = monitor;
                existingState.Window.SetTargetMonitor(monitor);
                UpdateNotchBounds(existingState.Window, existingState.VisibilityController, monitor);
            }
            else
            {
                CreateNotchWindow(monitor);
            }
        }

        UpdateEdgeTimer();
    }

    private static void UpdateNotchBounds(NotchWindow window, VisibilityController visibilityController, DisplayMonitor monitor)
    {
        var source = PresentationSource.FromVisual(window);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? monitor.DpiScaleX;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? monitor.DpiScaleY;

        var monitorBounds = monitor.Bounds;
        var notchWidth = 420 * dpiX;
        var notchHeight = 140 * dpiY;
        var monitorWidth = monitorBounds.Right - monitorBounds.Left;
        var left = (int)(monitorBounds.Left + (monitorWidth - notchWidth) / 2);
        var right = (int)(left + notchWidth);

        visibilityController.NotchBounds = new WindowRect(left, monitorBounds.Top, right, (int)(monitorBounds.Top + notchHeight));
    }

    private void OnForegroundWindowChanged(object? sender, WindowInfo info)
    {
        if (_fullscreenDetector is null)
            return;

        foreach (var (_, state) in _windowStates)
        {
            // Her monitörün kendi tam ekran durumu sorgulanır:
            // Yan ekranda tam ekran uygulama olması bu monitördeki Koy'u gizlemez!
            var isFullscreen = _fullscreenDetector.IsFullscreen(state.Monitor.Handle);
            state.VisibilityController.SetFullscreen(isFullscreen);
            state.VisibilityController.UpdateForegroundWindow(info.Handle, info.Bounds);

            if (!isFullscreen && state.Hwnd != 0 && info.Handle != state.Hwnd)
            {
                if (!FullscreenDetector.IsScreenClippingHost(info.Handle))
                {
                    WindowStyles.ReassertTopmost(state.Hwnd);
                }
            }
        }

        UpdateEdgeTimer();
    }

    private void OnVisibilityChanged(object? sender, VisibilityResult result)
    {
        UpdateEdgeTimer();
    }

    private void UpdateEdgeTimer()
    {
        if (_edgePeekTimer is null || _windowStates.Count == 0)
            return;

        var anyNeedsPeek = _windowStates.Values.Any(s =>
            (!s.VisibilityController.IsVisible || s.VisibilityController.IsMouseAtEdge) &&
            !s.VisibilityController.IsFullscreen &&
            s.VisibilityController.Mode != VisibilityMode.Fixed);

        if (anyNeedsPeek)
        {
            if (!_edgePeekTimer.IsEnabled)
                _edgePeekTimer.Start();
        }
        else if (_edgePeekTimer.IsEnabled)
        {
            _edgePeekTimer.Stop();
        }
    }

    private void CheckEdgePeek()
    {
        if (_windowTracker is null || _windowStates.Count == 0)
            return;

        if (_windowTracker.GetCursorPosition(out var x, out var y))
        {
            foreach (var (_, state) in _windowStates)
            {
                var vc = state.VisibilityController;
                if (vc.IsFullscreen)
                    continue;

                var monitor = state.Monitor;
                var top = monitor.Bounds.Top;
                var left = monitor.Bounds.Left;
                var right = monitor.Bounds.Right;

                // İmleç bu monitör sınırları içinde mi?
                if (x >= left && x <= right)
                {
                    var bounds = vc.NotchBounds;
                    var minX = bounds.Left - 30;
                    var maxX = bounds.Right + 30;
                    var isAtEdge = y >= top && y <= top + 2 && (bounds.IsEmpty || (x >= minX && x <= maxX));

                    if (isAtEdge)
                    {
                        vc.SetMouseAtEdge(true);
                    }
                    else if (vc.IsMouseAtEdge && !vc.IsMouseOver)
                    {
                        vc.SetMouseAtEdge(false);
                    }
                }
            }
        }
    }

    public void Stop()
    {
        _edgePeekTimer?.Stop();
        _edgePeekTimer = null;

        if (_monitorService is not null)
        {
            _monitorService.MonitorsChanged -= OnMonitorsChanged;
        }

        if (_windowTracker is not null)
        {
            _windowTracker.ForegroundWindowChanged -= OnForegroundWindowChanged;
            _windowTracker.Dispose();
            _windowTracker = null;
        }

        foreach (var (_, state) in _windowStates)
        {
            state.VisibilityController.Changed -= OnVisibilityChanged;
            state.Dispose();
        }
        _windowStates.Clear();
        _windows.Clear();

        _monitorService?.Dispose();
        _monitorService = null;

        _fullscreenDetector = null;

        _mediaService?.Dispose();
        _mediaService = null;

        _powerService?.Dispose();
        _powerService = null;

        _weatherService?.Dispose();
        _weatherService = null;

        _focusAssistService?.Dispose();
        _focusAssistService = null;

        _shelfService?.Dispose();
        _shelfService = null;
    }

    public static List<NotchPanel> ParseEnabledPanels(IEnumerable<string>? names)
    {
        var list = new List<NotchPanel>();
        if (names is not null)
        {
            foreach (var name in names)
            {
                if (string.Equals(name, "music", StringComparison.OrdinalIgnoreCase)) list.Add(NotchPanel.Music);
                else if (string.Equals(name, "controlcenter", StringComparison.OrdinalIgnoreCase)) list.Add(NotchPanel.ControlCenter);
                else if (string.Equals(name, "status", StringComparison.OrdinalIgnoreCase)) list.Add(NotchPanel.Status);
                else if (string.Equals(name, "calendar", StringComparison.OrdinalIgnoreCase)) list.Add(NotchPanel.Calendar);
                else if (string.Equals(name, "shelf", StringComparison.OrdinalIgnoreCase)) list.Add(NotchPanel.Shelf);
            }
        }
        if (list.Count == 0)
            list.Add(NotchPanel.Music);
        return list;
    }
}
