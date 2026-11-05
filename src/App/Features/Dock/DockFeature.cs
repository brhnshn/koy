using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Koy.Core.Display;
using Koy.Core.Dock;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Koy.Core.Visibility;
using Koy.Platform.Display;
using Koy.Platform.Media;
using Koy.Platform.Shell;

namespace Koy.App.Features.Dock;

/// <summary>
/// Dock özelliğinin yaşam döngüsünü (IFeature) yönetir.
/// Dock kapalıyken hiçbir servisi veya hook'u belleğe yüklemez.
/// Açıldığında Windows görev çubuğunu gizler, kapandığında güvenle geri yükler (AGENTS.md #7 & #12).
/// Çoklu monitör ortamında hedef ekranların her biri için bağımsız bir DockWindow örneği yönetir (M6.2).
/// </summary>
public sealed class DockFeature(AppSettings settings) : IFeature
{
    private sealed class DockWindowState : IDisposable
    {
        public required DisplayMonitor Monitor { get; set; }
        public required DockWindow Window { get; init; }
        public required DockViewModel ViewModel { get; init; }
        public required VisibilityController VisibilityController { get; init; }
        public nint Hwnd { get; set; }

        public void Dispose()
        {
            Window.Close();
            ViewModel.Dispose();
            VisibilityController.Dispose();
        }
    }

    private readonly Dictionary<string, DockWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DockWindowState> _windowStates = new(StringComparer.OrdinalIgnoreCase);

    private PinnedAppsService? _pinnedAppsService;
    private WindowTracker? _windowTracker;
    private ShellActionService? _shellActionService;
    private FullscreenDetector? _fullscreenDetector;
    private AppBarService? _appBarService;
    private MonitorService? _monitorService;
    private MediaService? _mediaService;
    private TrayHostService? _trayService;
    private DockController? _controller;
    private DispatcherTimer? _edgePeekTimer;

    public void Start()
    {
        if (_windows.Count > 0)
            return;

        // Görev çubuğunu gizle (ve durumunu diske kaydet)
        TaskbarService.Hide();

        // Platform servislerini başlat
        _pinnedAppsService = new PinnedAppsService();
        _windowTracker = new WindowTracker();
        _shellActionService = new ShellActionService();
        _fullscreenDetector = new FullscreenDetector();
        _monitorService = new MonitorService();
        _monitorService.Start();
        _monitorService.MonitorsChanged += OnMonitorsChanged;

        _controller = new DockController(
            _pinnedAppsService,
            _windowTracker,
            _shellActionService,
            _fullscreenDetector);

        // Yalnızca ReserveSpace (Yer ayır) yakınlık modunda masaüstü çalışma alanından (AppBar) yer ayır
        if (settings.DockProximity == DockProximityMode.ReserveSpace)
        {
            _appBarService = new AppBarService();
        }

        // Görev çubuğu gizliyken tepsi simgelerine ulaşmanın tek yolu Koy'un kendi tepsisi (ayardan kapatılabilir).
        if (settings.DockShowTray)
        {
            _trayService = new TrayHostService();
            _trayService.Start();
        }

        // Medya servisi (Tam genişlik Now Playing hapı için)
        _mediaService = new MediaService();
        _ = _mediaService.StartAsync();

        var monitors = _monitorService.GetMonitors();
        var targetMonitors = DisplayLayoutResolver.ResolveTargetMonitors(monitors, settings.MonitorTarget);

        foreach (var monitor in targetMonitors)
        {
            CreateDockWindow(monitor);
        }

        // 50 ms kenar gözetleme (peek) zamanlayıcısı (yalnız gizliyken UI thread'de çalışır)
        _edgePeekTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _edgePeekTimer.Tick += (_, _) => CheckEdgePeek();

        _windowTracker.ForegroundWindowChanged += OnForegroundWindowChanged;
        _windowTracker.WindowsChanged += OnWindowsChanged;
        _windowTracker.Start();

        // Hide() sonrası Gezgin görev çubuğunu bir kez daha gösterebiliyor; açılışta hemen yakala.
        TaskbarService.EnsureHidden();
        _controller.Start();

        UpdateEdgeTimer();
    }

    private void CreateDockWindow(DisplayMonitor monitor)
    {
        var visibilityController = new VisibilityController(settings.DockVisibility);
        var viewModel = new DockViewModel(
            settings,
            _controller!,
            _shellActionService!,
            visibilityController,
            status: null,
            mediaService: _mediaService,
            trayService: _trayService);

        var window = new DockWindow { DataContext = viewModel };
        window.SetTargetMonitor(monitor);

        var state = new DockWindowState
        {
            Monitor = monitor,
            Window = window,
            ViewModel = viewModel,
            VisibilityController = visibilityController
        };

        window.SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            state.Hwnd = hwnd;
            visibilityController.NotchHandle = hwnd;

            if (_fullscreenDetector is not null)
            {
                _fullscreenDetector.IgnoredHandles.Add(hwnd);
            }

            if (_windowTracker is not null)
            {
                _windowTracker.RegisterShellHook(hwnd);
                window.WmShellHook = _windowTracker.ShellHookMessage;
                window.ShellHookHandler = (wParam, lParam) => _windowTracker.HandleShellHookMessage(wParam, lParam);
            }

            if (_appBarService is not null)
            {
                var source = PresentationSource.FromVisual(window);
                var dpiScale = source?.CompositionTarget?.TransformToDevice.M22 ?? monitor.DpiScaleY;
                var height = window.ActualHeight > 0 ? (int)Math.Ceiling(window.ReservedHeight) : 46;
                _appBarService.Register(hwnd, height, dpiScale);
            }

            UpdateDockBounds(window, visibilityController);
        };

        window.SizeChanged += (s, e) =>
        {
            UpdateDockBounds(window, visibilityController);

            if (_appBarService is not null && state.Hwnd != 0 && e.NewSize.Height > 0)
            {
                var source = PresentationSource.FromVisual(window);
                var dpiScale = source?.CompositionTarget?.TransformToDevice.M22 ?? monitor.DpiScaleY;
                _appBarService.UpdatePosition(state.Hwnd, (int)Math.Ceiling(window.ReservedHeight), dpiScale);
            }
        };

        window.LocationChanged += (s, e) =>
        {
            UpdateDockBounds(window, visibilityController);
        };

        window.DisplayChangeHandler = () =>
        {
            _monitorService?.NotifyDisplayChanged();
        };

        _windowStates[monitor.Id] = state;
        _windows[monitor.Id] = window;

        window.Show();
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
                if (state.Hwnd != 0)
                {
                    _appBarService?.Unregister(state.Hwnd);
                    _windowTracker?.DeregisterShellHook(state.Hwnd);
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
                UpdateDockBounds(existingState.Window, existingState.VisibilityController);
            }
            else
            {
                CreateDockWindow(monitor);
            }
        }

        // Windows Explorer yeni monitör için ikincil görev çubuğu oluşturmuş olabilir (Shell_SecondaryTrayWnd)
        TaskbarService.EnsureHidden();
        UpdateEdgeTimer();
    }

    private void OnWindowsChanged(object? sender, EventArgs e)
    {
        // Bir pencere gösterildiğinde (görev çubuğu dahil) tetiklenir; görev çubuğu yeniden göründüyse gizle.
        TaskbarService.EnsureHidden();

        if (_windowTracker?.CurrentForeground is { } fg && fg.Handle != 0)
        {
            RecheckForegroundState(fg);
        }
    }

    private static void UpdateDockBounds(DockWindow window, VisibilityController visibilityController)
    {
        if (window.ActualWidth <= 0)
            return;

        var source = PresentationSource.FromVisual(window);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? (window.TargetMonitor?.DpiScaleX ?? 1.0);
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? (window.TargetMonitor?.DpiScaleY ?? 1.0);
        var left = (int)(window.Left * dpiX);
        var top = (int)(window.Top * dpiY);
        visibilityController.NotchBounds = new WindowRect(
            left,
            top,
            left + (int)(window.ActualWidth * dpiX),
            top + (int)(window.ActualHeight * dpiY));
    }

    private void OnForegroundWindowChanged(object? sender, WindowInfo info)
    {
        var pid = WindowStyles.GetWindowProcessId(info.Handle);

        // Harici bir pencereye geçildiğinde açık context menüyü kapat
        if (pid != (uint)Environment.ProcessId)
        {
            DockContextMenuManager.CloseActive();
        }

        // Gezgin kendi görev çubuğunu öne alırsa yeni tepsi kayıtları Koy'a gelmez; olay anında yoklanır, zamanlayıcı yok.
        _trayService?.EnsureFirst();

        // Koy'un kendi pencereleri (ayarlar, dock vb.) ön plana geldiğinde
        // mevcut tam ekran / maksimize durumunu sıfırlayıp dock'u zıplatma
        if (pid == (uint)Environment.ProcessId)
            return;

        RecheckForegroundState(info);
    }

    private void RecheckForegroundState(WindowInfo info)
    {
        if (_fullscreenDetector is null || info.Handle == 0)
            return;

        var isMaximized = WindowStyles.IsWindowMaximized(info.Handle);
        var fgMon = WindowStyles.GetWindowMonitor(info.Handle);

        foreach (var (_, state) in _windowStates)
        {
            // Pencere bu monitörde değilse bu monitör için büyütülmüş sayılmaz (M6.2: Yan ekrandaki durum burayı etkilemez)
            var monitorMaximized = isMaximized && (fgMon == 0 || fgMon == state.Monitor.Handle);

            // Her monitörün kendi tam ekran durumu sorgulanır:
            // Yan ekranda tam ekran uygulama olması bu monitördeki Koy'u gizlemez!
            var isFullscreen = _fullscreenDetector.IsFullscreen(state.Monitor.Handle);
            state.VisibilityController.SetFullscreen(isFullscreen);
            state.VisibilityController.UpdateForegroundWindow(info.Handle, info.Bounds);

            var leftDip = info.Bounds.Left / state.Monitor.DpiScaleX;
            var topDip = info.Bounds.Top / state.Monitor.DpiScaleY;
            var rightDip = info.Bounds.Right / state.Monitor.DpiScaleX;
            var bottomDip = info.Bounds.Bottom / state.Monitor.DpiScaleY;

            state.ViewModel.UpdateForegroundWindow(
                new WindowRect((int)leftDip, (int)topDip, (int)rightDip, (int)bottomDip),
                monitorMaximized,
                isFullscreen);

            if (!isFullscreen && !monitorMaximized && state.Hwnd != 0 && info.Handle != state.Hwnd)
            {
                if (!FullscreenDetector.IsScreenClippingHost(info.Handle))
                {
                    WindowStyles.ReassertTopmost(state.Hwnd);
                }
            }
        }

        UpdateEdgeTimer();
    }

    private void UpdateEdgeTimer()
    {
        if (_edgePeekTimer is null || _windowStates.Count == 0)
            return;

        var shouldRun = _windowStates.Values.Any(s =>
            (settings.DockProximity is DockProximityMode.Dodge or DockProximityMode.AlwaysHidden ||
             settings.DockVisibility != VisibilityMode.Fixed) &&
            settings.DockProximity != DockProximityMode.ReserveSpace);

        if (shouldRun)
        {
            if (!_edgePeekTimer.IsEnabled)
                _edgePeekTimer.Start();
        }
        else
        {
            if (_edgePeekTimer.IsEnabled)
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
                if (state.VisibilityController.IsFullscreen)
                    continue;

                var monitor = state.Monitor;
                var bottom = monitor.Bounds.Bottom;
                var left = monitor.Bounds.Left;
                var right = monitor.Bounds.Right;

                var isAtBottomEdge = y >= bottom - 4 && x >= left && x <= right;
                state.ViewModel.SetMouseAtBottom(isAtBottomEdge);
            }
        }
    }

    public void Stop()
    {
        _edgePeekTimer?.Stop();
        _edgePeekTimer = null;

        _mediaService?.Dispose();
        _mediaService = null;

        // Koy'un Shell_TrayWnd penceresi kapanır; uygulamalar yeniden doğrudan Windows tepsisine yazar.
        // Bütün kayıtlar zaten Windows'a da iletildiği için tepside eksik simge kalmaz.
        _trayService?.Dispose();
        _trayService = null;

        if (_monitorService is not null)
        {
            _monitorService.MonitorsChanged -= OnMonitorsChanged;
        }

        if (_appBarService is not null)
        {
            foreach (var (_, state) in _windowStates)
            {
                if (state.Hwnd != 0)
                    _appBarService.Unregister(state.Hwnd);
            }
            _appBarService.Dispose();
            _appBarService = null;
        }

        if (_windowTracker is not null)
        {
            foreach (var (_, state) in _windowStates)
            {
                if (state.Hwnd != 0)
                    _windowTracker.DeregisterShellHook(state.Hwnd);
            }
            _windowTracker.ForegroundWindowChanged -= OnForegroundWindowChanged;
            _windowTracker.WindowsChanged -= OnWindowsChanged;
            _windowTracker.Dispose();
            _windowTracker = null;
        }

        foreach (var (_, state) in _windowStates)
        {
            state.Dispose();
        }
        _windowStates.Clear();
        _windows.Clear();

        _monitorService?.Dispose();
        _monitorService = null;

        _controller?.Dispose();
        _controller = null;

        _fullscreenDetector = null;
        _pinnedAppsService = null;
        _shellActionService = null;

        // Güvenlik kuralı: Kapanışta Windows görev çubuğu her durumda geri getirilir
        TaskbarService.Restore();
    }
}
