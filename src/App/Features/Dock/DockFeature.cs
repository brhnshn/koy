using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Koy.Core.Display;
using Koy.Core.Power;
using Koy.Core.Dock;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Koy.Core.Tray;
using Koy.Core.Visibility;
using Koy.Platform.Display;
using Koy.Platform.Media;
using Koy.Platform.Power;
using Koy.Platform.Shell;

namespace Koy.App.Features.Dock;

/// <summary>
/// Dock özelliğinin yaşam döngüsünü (IFeature) yönetir.
/// Dock kapalıyken hiçbir servisi veya hook'u belleğe yüklemez.
/// Açıldığında Windows görev çubuğunu gizler, kapandığında güvenle geri yükler (AGENTS.md #7 & #12).
/// Çoklu monitör ortamında hedef ekranların her biri için bağımsız bir DockWindow örneği yönetir (M6.2).
/// </summary>
public sealed class DockFeature(AppSettings settings, SharedServices services) : IFeature
{
    private sealed class DockWindowState : IDisposable
    {
        public required DisplayMonitor Monitor { get; set; }
        public required DockWindow Window { get; init; }
        public required DockViewModel ViewModel { get; init; }
        public required VisibilityController VisibilityController { get; init; }
        public required EdgeRevealTracker EdgeTracker { get; init; }
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

    private OrderedPinnedAppsService? _pinnedAppsService;
    private readonly RecentFilesService _recentFiles = new();   // durumsuz; listeyi yalnız yığın açılınca okur
    private WindowTracker? _windowTracker;
    private ShellActionService? _shellActionService;
    private FullscreenDetector? _fullscreenDetector;
    private AppBarService? _appBarService;
    private MonitorService? _monitorService;
    private DisplayEvents? _displayEvents;
    private MediaService? _mediaService;
    private PowerService? _powerService;
    private TrayHostService? _trayService;   // yalnız görev çubuğu gizliyken dolu (ApplyTrayPolicy)
    private DockController? _controller;
    private DispatcherTimer? _edgePeekTimer;

    /// <summary>Açık dock penceresi sayısı. Dock'ta Koy düğmesi göründüğü için bildirim alanı simgesi buna bakar.</summary>
    public int WindowCount => _windowStates.Count;

    /// <summary>Dock pencere sayısı değişti (açıldı, ekran eklendi/çıkarıldı, durdu).</summary>
    public event Action? WindowCountChanged;

    public void Start()
    {
        if (_windows.Count > 0)
            return;

        // Platform servislerini başlat
        _pinnedAppsService = new OrderedPinnedAppsService(
            new PinnedAppsService(),
            () => settings.DockPinnedOrder,
            SavePinnedOrder,
            TaskbandFavoritesReader.Read);
        _windowTracker = services.AcquireWindowTracker();
        _shellActionService = new ShellActionService();
        _fullscreenDetector = new FullscreenDetector();
        _monitorService = new MonitorService();
        _monitorService.Start();
        _monitorService.MonitorsChanged += OnMonitorsChanged;
        _displayEvents = new DisplayEvents(_monitorService);

        _controller = new DockController(
            _pinnedAppsService,
            _windowTracker,
            _shellActionService,
            _fullscreenDetector,
            refreshDebounce: TimeSpan.FromMilliseconds(120),
            post: action => Application.Current?.Dispatcher.InvokeAsync(action));

        // Yalnızca ReserveSpace (Yer ayır) yakınlık modunda masaüstü çalışma alanından (AppBar) yer ayır
        if (settings.DockProximity == DockProximityMode.ReserveSpace)
        {
            _appBarService = new AppBarService();
        }

        // Medya servisi (Tam genişlik Now Playing hapı için); Notch ile paylaşılan tek örnek
        _mediaService = services.AcquireMediaService();

        // Pil tasarrufu için güç durumu; ayar kapalıysa servis kurulmaz.
        if (settings.BatterySaverReduceMotion)
        {
            _powerService = new PowerService();
            _powerService.PowerChanged += OnPowerChanged;
            ApplyReduceMotion();
        }

        // Dock, ayarlarda "dock açık" olan her monitörde ayrı bir pencere olarak kurulur.
        var targetMonitors = ResolveTargetMonitors(_monitorService.GetMonitors());

        foreach (var monitor in targetMonitors)
        {
            CreateDockWindow(monitor);
        }

        // Önce tepsi, sonra görev çubuğu: Windows'un tepsisi gizlenmeden Koy'un tepsisi hazır olsun.
        ApplyTrayPolicy();

        // Görev çubuğu yalnız en az bir dock penceresi varken gizlenir (aksi halde hiçbir ekranda görev çubuğu kalmazdı).
        ApplyTaskbarPolicy();

        // 100 ms kenar gözetleme (peek) zamanlayıcısı (yalnız dock gizliyken UI thread'de çalışır)
        _edgePeekTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _edgePeekTimer.Tick += (_, _) => CheckEdgePeek();

        _windowTracker.ForegroundWindowChanged += OnForegroundWindowChanged;
        _windowTracker.WindowsChanged += OnWindowsChanged;

        // İzleyici paylaşımlı: Notch onu önceden başlattıysa ilk ön plan olayı bir daha gelmez, mevcut durumu elle uygula.
        var trackerWasRunning = _windowTracker.IsStarted;
        _windowTracker.Start();
        if (trackerWasRunning && _windowTracker.CurrentForeground is { } currentForeground)
            OnForegroundWindowChanged(this, currentForeground);

        // Hide() sonrası Gezgin görev çubuğunu bir kez daha gösterebiliyor; açılışta hemen yakala.
        EnsureTaskbarHidden();
        _controller.Start();

        UpdateEdgeTimer();
        WindowCountChanged?.Invoke();
    }

    /// <summary>
    /// Sabitli uygulama sırasını ayar dosyasına yazar. Dosya baştan okunup yalnız bu alan değiştirilir; Koy penceresinin
    /// kaydettiği öteki ayarlar ezilmez. SettingsSaved tetiklenmez (özellikler yeniden başlamasın).
    /// </summary>
    private void SavePinnedOrder(IReadOnlyList<string> order)
    {
        var store = new SettingsStore(SettingsStore.DefaultPath);
        store.Save(store.Load() with { DockPinnedOrder = [.. order] });
        settings.DockPinnedOrder = [.. order];
    }

    /// <summary>
    /// Explorer yeniden başlayınca yeni görev çubuğu görünür ve varsayılan durumda gelir; gizlemeyi yeniden uygular.
    /// TaskbarService.Hide idempotent olduğundan kullanıcının asıl görev çubuğu durumu korunur.
    /// Yer ayır modunda AppBar kayıtları da Explorer'la birlikte yok olur; çalışma alanı yeniden ayrılır.
    /// </summary>
    public void ReapplyTaskbarHiding()
    {
        if (_windowStates.Count == 0)
            return;

        if (settings.HideWindowsTaskbar)
        {
            TaskbarService.Hide();
            // Yeni Gezgin kendi tepsisini oluşturdu: Koy'unki yeniden öne alınır, uygulamalar simgelerini yeniden kaydeder.
            _trayService?.EnsureFirst();
        }

        if (_appBarService is null)
            return;

        foreach (var (_, state) in _windowStates)
        {
            if (state.Hwnd == 0)
                continue;

            var dpiScale = state.Monitor.DpiScaleY;
            _appBarService.Reregister(state.Hwnd, (int)Math.Ceiling(state.Window.ReservedHeight), dpiScale);
        }
    }

    private bool _taskbarHidden;

    private void EnsureTaskbarHidden()
    {
        if (settings.HideWindowsTaskbar && _windowStates.Count > 0)
        {
            // Görev çubuğu yeniden göründüyse gizlendi: tepsi sahipliği (elle geri getirmede bırakılmıştı) geri alınır.
            if (TaskbarService.EnsureHidden() && _trayReleasedByUser)
                _trayReleasedByUser = false;

            ApplyTrayPolicy();

            // Gezgin görev çubuğunu yeniden gösterip gizleyince kendi penceresini öne alabilir; tepsi geri alınır.
            _trayService?.EnsureFirst();
        }
    }

    // "Windows görev çubuğunu geri getir" düğmesi dock çalışırken basıldı: görev çubuğu görünürken tepsi sahipliği alınmaz.
    private bool _trayReleasedByUser;

    /// <summary>
    /// Kullanıcı görev çubuğunu elle geri getirdi (Dock / Hakkında sayfasındaki kurtarma düğmesi). Görev çubuğu
    /// görünürken Windows'un tepsisine dokunulmaz: Koy'un tepsi penceresi bırakılır. Görev çubuğu dock tarafından
    /// yeniden gizlenirse tepsi geri alınır.
    /// </summary>
    public void OnTaskbarRestoredManually()
    {
        _trayReleasedByUser = true;
        if (_trayService is not null)
            ReleaseTray();
    }

    /// <summary>
    /// Dock penceresi varken Windows görev çubuğunu (birincil ve bütün ikincil monitörlerdeki) gizler; hiç dock
    /// penceresi kalmadıysa (ör. dock her ekranda kapatıldı) geri getirir. Kullanıcı "görev çubuğunu gizle"yi
    /// kapattıysa hiç dokunulmaz.
    /// </summary>
    private void ApplyTaskbarPolicy()
    {
        if (!settings.HideWindowsTaskbar)
            return;

        if (_windowStates.Count > 0)
        {
            TaskbarService.Hide();
            _taskbarHidden = true;
            _trayService?.EnsureFirst();
        }
        else if (_taskbarHidden)
        {
            TaskbarService.Restore();
            _taskbarHidden = false;
        }
    }

    /// <summary>
    /// Koy'un tepsi penceresini yalnız Windows görev çubuğu gerçekten gizliyken (ayar açık ve en az bir dock penceresi
    /// var) kurar; koşul bozulunca (son dock penceresi kapandı, ayar değişti) bırakır. Bütün monitörlerdeki dock'lar
    /// SharedServices'teki aynı örneği paylaşır.
    /// </summary>
    private void ApplyTrayPolicy()
    {
        var shouldHost = !_trayReleasedByUser
            && TrayHostPolicy.ShouldHost(settings.HideWindowsTaskbar, settings.DockShowTray, _windowStates.Count);

        // Tepsi iş parçacığı beklenmedik biçimde öldüyse (pencere kurulumu sonrası kapandı) tutamaç ölüdür: bırakılıp yeniden alınır.
        if (shouldHost && _trayService is { IsRunning: false })
            ReleaseTray();

        // Kurulum başarısızsa pencere olaylarında her seferinde yeni iş parçacığı denenmez; ekran değişince yeniden denenir.
        if (shouldHost && _trayService is null && !_trayAcquireFailed)
        {
            _trayService = services.AcquireTrayHost();
            _trayAcquireFailed = _trayService is null;
            if (_trayService is not null)
            {
                foreach (var state in _windowStates.Values)
                    state.ViewModel.SetTrayService(_trayService);
            }
        }
        else if (!shouldHost && _trayService is not null)
        {
            ReleaseTray();
        }
    }

    private bool _trayAcquireFailed;

    private void ReleaseTray()
    {
        // Düğme ve açık pencere önce kapanır; sonra Koy'un Shell_TrayWnd penceresi yok edilir ve Windows tepsisi tek başına kalır.
        foreach (var state in _windowStates.Values)
            state.ViewModel.SetTrayService(null);

        _trayService = null;
        services.ReleaseTrayHost();
    }

    private IReadOnlyList<DisplayMonitor> ResolveTargetMonitors(IReadOnlyList<DisplayMonitor> monitors) =>
        MonitorPolicy.DockMonitors(MonitorPolicy.Resolve(settings.Monitors, settings.NewScreenDefault, monitors));

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
            trayService: _trayService,
            recentFiles: _recentFiles);

        viewModel.ReduceMotion = _reduceMotion;

        var window = new DockWindow { DataContext = viewModel };
        window.SetTargetMonitor(monitor);

        var state = new DockWindowState
        {
            Monitor = monitor,
            Window = window,
            ViewModel = viewModel,
            VisibilityController = visibilityController,
            // Çekil modunun kendi gecikmesi (DockDodgeDelayMs) ViewModel'de işler; çifte beklemesin.
            EdgeTracker = new EdgeRevealTracker(
                settings.DockProximity == DockProximityMode.Dodge ? TimeSpan.Zero : EdgeRevealTracker.DefaultDelay)
        };

        // Dock gizlenince / görününce kenar zamanlayıcısı açılıp kapanır (gizli değilken fare izlenmez).
        viewModel.EdgeWatchChanged += (_, _) => UpdateEdgeTimer();

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
                var dpiScale = state.Monitor.DpiScaleY;
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
                var dpiScale = state.Monitor.DpiScaleY;
                _appBarService.UpdatePosition(state.Hwnd, (int)Math.Ceiling(window.ReservedHeight), dpiScale);
            }
        };

        window.LocationChanged += (s, e) =>
        {
            UpdateDockBounds(window, visibilityController);
        };

        // DPI değişince DIP yükseklik aynı kalır (SizeChanged gelmez) ama piksel karşılığı değişir: AppBar payı yenilenir.
        window.DpiChanged += (s, e) =>
        {
            UpdateDockBounds(window, visibilityController);
            if (_appBarService is not null && state.Hwnd != 0 && window.ActualHeight > 0)
                _appBarService.UpdatePosition(state.Hwnd, (int)Math.Ceiling(window.ReservedHeight), state.Monitor.DpiScaleY);
        };

        window.DisplayChangeHandler = () =>
        {
            _monitorService?.NotifyDisplayChanged();
        };

        _windowStates[monitor.Id] = state;
        _windows[monitor.Id] = window;

        window.Show();
    }

    private bool _reduceMotion;

    private void OnPowerChanged(object? sender, PowerChangedEventArgs e)
    {
        // WinRT güç olayları UI dışı thread'den gelir; durum ve pencere nesnelerine UI thread'inde dokunulur.
        Application.Current?.Dispatcher.InvokeAsync(ApplyReduceMotion);
    }

    private void ApplyReduceMotion()
    {
        if (_powerService is null)
            return;

        _reduceMotion = BatterySaverPolicy.ShouldReduceMotion(
            settings.BatterySaverReduceMotion, _powerService.HasBattery, _powerService.Status);
        foreach (var state in _windowStates.Values)
            state.ViewModel.ReduceMotion = _reduceMotion;
    }

    private void OnMonitorsChanged(object? sender, EventArgs e)
    {
        if (Application.Current is not null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(() => OnMonitorsChanged(sender, e));
            return;
        }

        if (_monitorService is null) return;

        var targetMonitors = ResolveTargetMonitors(_monitorService.GetMonitors());
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

        // Windows Explorer yeni monitör için ikincil görev çubuğu oluşturmuş olabilir (Shell_SecondaryTrayWnd);
        // Hide() ikincil çubukları da kapsar. Son dock penceresi kapandıysa görev çubuğu geri gelir
        // ve (tepsi önce bırakılır) Koy'un tepsi penceresi de kapanır.
        _trayAcquireFailed = false;
        ApplyTrayPolicy();
        ApplyTaskbarPolicy();
        UpdateEdgeTimer();
        WindowCountChanged?.Invoke();
    }

    private void OnWindowsChanged(object? sender, EventArgs e)
    {
        // Bir pencere gösterildiğinde (görev çubuğu dahil) tetiklenir; görev çubuğu yeniden göründüyse gizle.
        EnsureTaskbarHidden();

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

        // Dodge/AlwaysHidden/akıllı görünürlük kenarı hep izler; büyütülmüş pencere dock'u gizlediyse
        // (NeedsEdgeWatch) ve kenardan açma açıksa yalnız o sürede izlenir.
        var shouldRun = _windowStates.Values.Any(s =>
            ((settings.DockProximity is DockProximityMode.Dodge or DockProximityMode.AlwaysHidden ||
              settings.DockVisibility != VisibilityMode.Fixed) &&
             settings.DockProximity != DockProximityMode.ReserveSpace) ||
            (settings.DockEdgeReveal && s.ViewModel.NeedsEdgeWatch));

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
        // Savunma: UI thread dışından çağrılırsa (VerifyAccess çökmesi, 4.11 23:18) UI'a taşı.
        if (Application.Current is { } app && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.InvokeAsync(CheckEdgePeek, DispatcherPriority.Background);
            return;
        }

        if (_windowTracker is null || _windowStates.Count == 0)
            return;

        if (_windowTracker.GetCursorPosition(out var x, out var y))
        {
            var buttonDown = CursorHelper.IsAnyMouseButtonDown();
            var now = TimeSpan.FromMilliseconds(Environment.TickCount64);

            foreach (var (_, state) in _windowStates)
            {
                // Tam ekranda (oyun, F11, tam ekran video) kenar tetiği yoktur; bu monitörün bayrağı da temizlenir.
                if (state.VisibilityController.IsFullscreen)
                {
                    state.EdgeTracker.Reset();
                    state.ViewModel.SetMouseAtBottom(false);
                    continue;
                }

                // Monitör başına: yalnız imlecin bulunduğu ekran. Fare kenarda ~300 ms durunca ve tuş basılı değilken açılır.
                var monitor = state.Monitor;
                var inZone = EdgeRevealTracker.IsInZone(
                    x, y, monitor.Bounds.Left, monitor.Bounds.Right, monitor.Bounds.Bottom,
                    revealed: state.ViewModel.IsVisible, dpiScaleY: monitor.DpiScaleY);

                state.ViewModel.SetMouseAtBottom(state.EdgeTracker.Update(inZone, buttonDown, now));
            }
        }
    }

    public void Stop()
    {
        try
        {
            StopCore();
        }
        finally
        {
            // Güvenlik kuralı: Kapanışta Windows görev çubuğu her durumda geri getirilir (yukarıdaki temizlik hata verse bile).
            TaskbarService.Restore();
        }
    }

    private void StopCore()
    {
        _edgePeekTimer?.Stop();
        _edgePeekTimer = null;

        // Paylaşılan servisleri yalnız bırak; son kullanıcı bırakınca SharedServices Dispose eder.
        if (_mediaService is not null)
        {
            services.ReleaseMediaService();
            _mediaService = null;
        }

        // Koy'un Shell_TrayWnd penceresi kapanır; uygulamalar yeniden doğrudan Windows tepsisine yazar.
        // Bütün kayıtlar zaten Windows'a da iletildiği için tepside eksik simge kalmaz (TrayHostService açıklaması).
        // Görev çubuğu geri getirilmeden (aşağıdaki Restore) önce yapılır.
        if (_trayService is not null)
            ReleaseTray();

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
            _windowTracker = null;
            services.ReleaseWindowTracker();
        }

        foreach (var (_, state) in _windowStates)
        {
            state.Dispose();
        }
        _windowStates.Clear();
        _windows.Clear();
        WindowCountChanged?.Invoke();

        _displayEvents?.Dispose();
        _displayEvents = null;
        _monitorService?.Dispose();
        _monitorService = null;

        if (_powerService is not null)
        {
            _powerService.PowerChanged -= OnPowerChanged;
            _powerService.Dispose();
            _powerService = null;
        }

        _controller?.Dispose();
        _controller = null;

        _fullscreenDetector = null;
        _pinnedAppsService = null;
        _shellActionService = null;
    }
}
