using System.Windows;
using System.Windows.Interop;
using Koy.Core.Dock;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Koy.Core.Visibility;
using Koy.Platform.Display;
using Koy.Platform.Shell;

namespace Koy.App.Features.Dock;

/// <summary>
/// Dock özelliğinin yaşam döngüsünü (IFeature) yönetir.
/// Dock kapalıyken hiçbir servisi veya hook'u belleğe yüklemez.
/// Açıldığında Windows görev çubuğunu gizler, kapandığında güvenle geri yükler (AGENTS.md #7 & #12).
/// </summary>
public sealed class DockFeature(AppSettings settings) : IFeature
{
    private DockWindow? _window;
    private PinnedAppsService? _pinnedAppsService;
    private WindowTracker? _windowTracker;
    private ShellActionService? _shellActionService;
    private FullscreenDetector? _fullscreenDetector;
    private AppBarService? _appBarService;
    private VisibilityController? _visibilityController;
    private MonitorService? _monitorService;
    private DockController? _controller;
    private DockViewModel? _viewModel;
    private ITimer? _edgePeekTimer;
    private nint _windowHwnd;

    public void Start()
    {
        if (_window is not null)
            return;

        // Görev çubuğunu gizle (ve durumunu diske kaydet)
        TaskbarService.Hide();

        // Platform servislerini başlat
        _pinnedAppsService = new PinnedAppsService();
        _windowTracker = new WindowTracker();
        _shellActionService = new ShellActionService();
        _fullscreenDetector = new FullscreenDetector();
        _visibilityController = new VisibilityController(settings.DockVisibility);
        _monitorService = new MonitorService();
        _monitorService.Start();

        _controller = new DockController(
            _pinnedAppsService,
            _windowTracker,
            _shellActionService,
            _fullscreenDetector);

        _viewModel = new DockViewModel(
            settings,
            _controller,
            _shellActionService,
            _visibilityController);

        var targetMonitor = _monitorService.GetMonitor(settings.MonitorTarget) ?? _monitorService.GetPrimaryMonitor();

        _window = new DockWindow { DataContext = _viewModel };
        _window.SetTargetMonitor(targetMonitor);
        _window.SourceInitialized += OnWindowSourceInitialized;
        _window.SizeChanged += OnWindowSizeChanged;
        _window.DisplayChangeHandler = () =>
        {
            _monitorService.NotifyDisplayChanged();
            var m = _monitorService.GetMonitor(settings.MonitorTarget) ?? _monitorService.GetPrimaryMonitor();
            _window?.SetTargetMonitor(m);
        };
        _window.Show();

        // Fixed modda AppBar kaydı
        if (settings.DockVisibility == VisibilityMode.Fixed)
        {
            _appBarService = new AppBarService();
        }

        // 100 ms kenar gözetleme (peek) zamanlayıcısı (yalnız gizliyken çalışır)
        _edgePeekTimer = TimeProvider.System.CreateTimer(
            _ => CheckEdgePeek(),
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);

        _windowTracker.ForegroundWindowChanged += OnForegroundWindowChanged;
        _windowTracker.Start();
        _controller.Start();

        UpdateEdgeTimer();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_appBarService is not null && _windowHwnd != 0 && _window is not null && e.NewSize.Height > 0)
        {
            var source = PresentationSource.FromVisual(_window);
            var dpiScale = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            _appBarService.UpdatePosition(_windowHwnd, (int)e.NewSize.Height, dpiScale);
        }
    }

    private void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        if (_window is null)
            return;

        _windowHwnd = new WindowInteropHelper(_window).Handle;

        if (_windowTracker is not null)
        {
            _windowTracker.RegisterShellHook(_windowHwnd);
            _window.WmShellHook = _windowTracker.ShellHookMessage;
            _window.ShellHookHandler = (wParam, lParam) => _windowTracker.HandleShellHookMessage(wParam, lParam);
        }

        if (_appBarService is not null)
        {
            var source = PresentationSource.FromVisual(_window);
            var dpiScale = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            var height = _window.ActualHeight > 0 ? (int)_window.ActualHeight : 64;
            _appBarService.Register(_windowHwnd, height, dpiScale);
        }

        if (_fullscreenDetector is not null)
        {
            _fullscreenDetector.NotchHandle = _windowHwnd;
        }
    }

    private void OnForegroundWindowChanged(object? sender, WindowInfo info)
    {
        if (_fullscreenDetector is null || _visibilityController is null)
            return;

        var isFullscreen = _fullscreenDetector.IsFullscreen();
        _visibilityController.SetFullscreen(isFullscreen);
        _visibilityController.UpdateForegroundWindow(info.Handle, info.Bounds);

        if (!isFullscreen && _windowHwnd != 0 && info.Handle != _windowHwnd)
        {
            WindowStyles.ReassertTopmost(_windowHwnd);
        }

        UpdateEdgeTimer();
    }

    private void UpdateEdgeTimer()
    {
        if (_visibilityController is null || _edgePeekTimer is null)
            return;

        var shouldRun = (!_visibilityController.IsVisible || _visibilityController.IsMouseAtEdge) &&
                        !_visibilityController.IsFullscreen &&
                        settings.DockVisibility != VisibilityMode.Fixed;

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
        if (_visibilityController is null || _windowTracker is null || _viewModel is null)
            return;

        if (_visibilityController.IsFullscreen)
            return;

        if (_windowTracker.GetCursorPosition(out var x, out var y))
        {
            var targetMonitor = _monitorService?.GetMonitor(settings.MonitorTarget) ?? _monitorService?.GetPrimaryMonitor();
            var bottom = targetMonitor is not null ? targetMonitor.Bounds.Bottom : (int)SystemParameters.PrimaryScreenHeight;
            var left = targetMonitor is not null ? targetMonitor.Bounds.Left : 0;
            var right = targetMonitor is not null ? targetMonitor.Bounds.Right : (int)SystemParameters.PrimaryScreenWidth;

            var isAtBottomEdge = y >= bottom - 4 && x >= left && x <= right;
            _viewModel.SetMouseAtBottom(isAtBottomEdge);
        }
    }

    public void Stop()
    {
        _edgePeekTimer?.Dispose();
        _edgePeekTimer = null;

        if (_appBarService is not null && _windowHwnd != 0)
        {
            _appBarService.Unregister(_windowHwnd);
            _appBarService.Dispose();
            _appBarService = null;
        }

        if (_window is not null)
        {
            _window.SizeChanged -= OnWindowSizeChanged;
            _window.Close();
            _window = null;
        }

        if (_windowTracker is not null)
        {
            if (_windowHwnd != 0)
                _windowTracker.DeregisterShellHook(_windowHwnd);

            _windowTracker.ForegroundWindowChanged -= OnForegroundWindowChanged;
            _windowTracker.Dispose();
            _windowTracker = null;
        }

        _windowHwnd = 0;

        _monitorService?.Dispose();
        _monitorService = null;

        _viewModel?.Dispose();
        _viewModel = null;

        _controller?.Dispose();
        _controller = null;

        _visibilityController?.Dispose();
        _visibilityController = null;

        _fullscreenDetector = null;
        _pinnedAppsService = null;
        _shellActionService = null;

        // Güvenlik kuralı: Kapanışta Windows görev çubuğu her durumda geri getirilir
        TaskbarService.Restore();
    }
}
