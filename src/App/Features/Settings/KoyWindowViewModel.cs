using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Dock;
using Koy.Core.Settings;
using Koy.Core.Visibility;
using Koy.Platform.Display;
using Koy.Platform.Shell;

namespace Koy.App.Features.Settings;

public sealed partial class KoyWindowViewModel : ObservableObject
{
    private readonly SettingsStore _store;

    /// <summary>Kısayollar sayfası: tuş yakalama, kayıt ve hata durumu.</summary>
    public ShortcutsViewModel Shortcuts { get; }

    [ObservableProperty]
    private int _selectedPageIndex; // 0: Genel, 1: Çentik, 2: Dock, 3: Ekranlar, 4: Görünüm, 5: Kısayollar, 6: Performans, 7: Hakkında

    // --- GENEL ---
    [ObservableProperty]
    private bool _runAtStartup;

    [ObservableProperty]
    private bool _hideWindowsTaskbar = true;

    [ObservableProperty]
    private string _language = "tr";

    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    [ObservableProperty]
    private string _updateChannel = "Kararlı";

    [ObservableProperty]
    private string _lastCheckText = "Son denetim: bugün 14:02";

    // --- ÇENTİK ---
    [ObservableProperty]
    private bool _notchEnabled = true;

    [ObservableProperty]
    private VisibilityMode _notchVisibility = VisibilityMode.Smart;

    [ObservableProperty]
    private bool _notchHideInFullscreen = true;

    [ObservableProperty]
    private int _idleDisplayMode; // 0: Saat ve pil, 1: Yalnız saat, 2: Hiçbiri

    [ObservableProperty]
    private bool _musicShowWaveform = true;

    [ObservableProperty]
    private bool _showVolumeOsd = true;

    [ObservableProperty]
    private bool _showBrightnessOsd = true;

    [ObservableProperty]
    private bool _hideWindowsOsd;

    [ObservableProperty]
    private bool _panelMusic = true;

    [ObservableProperty]
    private bool _panelCalendar = true;

    [ObservableProperty]
    private bool _panelShelf = true;

    [ObservableProperty]
    private bool _panelControlCenter = true;

    [ObservableProperty]
    private bool _panelStatus;

    [ObservableProperty]
    private int _notchOpenBehavior = 1; // 0: Üzerine gelince, 1: Tıklayınca

    [ObservableProperty]
    private string _notchSize = "Orta";

    // --- DOCK ---
    [ObservableProperty]
    private bool _dockEnabled = true;

    [ObservableProperty]
    private DockStyleMode _dockStyle = DockStyleMode.FloatingCapsule;

    [ObservableProperty]
    private DockProximityMode _dockProximity = DockProximityMode.DockDown;

    [ObservableProperty]
    private bool _dockHideInFullscreen = true;

    [ObservableProperty]
    private int _dockDodgeDelayMs = 150;

    [ObservableProperty]
    private double _dockIconSize = 40.0;

    [ObservableProperty]
    private bool _dockHoverMagnify = true;

    [ObservableProperty]
    private int _dockIndicatorStyle; // 0: Nokta, 1: Çizgi

    [ObservableProperty]
    private bool _dockGroupWindows = true;

    [ObservableProperty]
    private bool _dockShowBadges = true;

    [ObservableProperty]
    private bool _dockShowStartButton = true;

    [ObservableProperty]
    private bool _dockShowFolders = true;

    [ObservableProperty]
    private bool _dockShowMinimized = true;

    [ObservableProperty]
    private bool _dockShowRecent;

    [ObservableProperty]
    private bool _dockThumbnailPreview = true;

    [ObservableProperty]
    private int _dockThumbnailDelayMs = 400;

    // --- EKRANLAR ---
    [ObservableProperty]
    private int _monitorCount = 1;

    [ObservableProperty]
    private bool _screen1Notch = true;

    [ObservableProperty]
    private bool _screen1Dock = true;

    [ObservableProperty]
    private bool _screen1WindowsOnly;

    [ObservableProperty]
    private int _newScreenDefault; // 0: Çentik ve dock, 1: Yalnız çentik, 2: Hiçbiri

    // --- GÖRÜNÜM ---
    [ObservableProperty]
    private string _theme = "Koyu";

    [ObservableProperty]
    private string _accentColor = "#3B82F6";

    [ObservableProperty]
    private bool _useWindowsAccent;

    [ObservableProperty]
    private string _material = "Katı";

    [ObservableProperty]
    private double _cornerRadius = 18.0;

    [ObservableProperty]
    private string _animationSpeed = "Normal";

    [ObservableProperty]
    private bool _respectReduceMotion = true;

    // --- KISAYOLLAR ---
    [ObservableProperty]
    private int _wheelAction; // 0: Panel değiştir, 1: Ses

    [ObservableProperty]
    private int _middleClickAction; // 0: Yeni pencere, 1: Kapat

    // --- PERFORMANS ---
    [ObservableProperty]
    private double _memoryUsageMb = 38.0;

    [ObservableProperty]
    private string _memoryUsageText = "38 MB / 60 MB";

    [ObservableProperty]
    private double _memoryPct = 63.0;

    [ObservableProperty]
    private bool _batterySaverReduceMotion = true;

    [ObservableProperty]
    private bool _batterySaverStopWaveform = true;

    [ObservableProperty]
    private string _weatherRefreshInterval = "30 dakikada bir";

    [ObservableProperty]
    private bool _audioVisualizerEnabled;

    // --- HAKKINDA ---
    [ObservableProperty]
    private string _appVersion = "0.1.0";

    [ObservableProperty]
    private string _aboutStatusText = "Sürüm 0.1.0 · GPLv3 · güncel";

    public event Action? SettingsSaved;

    private bool _isLoading;

    public KoyWindowViewModel(SettingsStore? store = null)
    {
        _store = store ?? new SettingsStore(SettingsStore.DefaultPath);
        Shortcuts = new ShortcutsViewModel(_store);
        LoadFromSettings();
    }

    public void LoadFromSettings()
    {
        _isLoading = true;
        try
        {
            var s = _store.Load();

            RunAtStartup = StartupService.IsEnabled;
            HideWindowsTaskbar = s.HideWindowsTaskbar;
            Language = s.Language;
            AutoCheckUpdates = s.AutoCheckUpdates;
            UpdateChannel = s.UpdateChannel;

            NotchEnabled = s.NotchEnabled;
            NotchVisibility = s.NotchVisibility;
            DockEnabled = s.DockEnabled;
            DockStyle = s.DockStyle;
            DockProximity = s.DockProximity;
            DockHideInFullscreen = s.DockHideInFullscreen;
            DockHoverMagnify = s.DockHoverMagnify;
            DockShowStartButton = s.DockShowStartButton;
            DockShowFolders = s.DockShowFolders;
            DockShowMinimized = s.DockShowMinimized;
            DockShowBadges = s.DockShowBadges;
            DockThumbnailPreview = s.DockThumbnailPreview;
            DockThumbnailDelayMs = s.DockThumbnailDelayMs;
            DockDodgeDelayMs = s.DockDodgeDelayMs;
            DockIconSize = s.DockIconSize > 0 ? s.DockIconSize : 28.0;
            DockIndicatorStyle = s.DockIndicatorStyle;

            NotchHideInFullscreen = s.NotchHideInFullscreen;
            MusicShowWaveform = s.MusicShowWaveform;
            IdleDisplayMode = s.IdleDisplayMode;
            NotchOpenBehavior = s.NotchOpenBehavior;

            Theme = s.Theme;
            AccentColor = s.AccentColor ?? "#3B82F6";
            Material = s.Material;
            CornerRadius = s.CornerRadius;
            AnimationSpeed = s.AnimationSpeed;
            RespectReduceMotion = s.RespectReduceMotion;
            BatterySaverReduceMotion = s.BatterySaverReduceMotion;
            BatterySaverStopWaveform = s.BatterySaverStopWaveform;
            AudioVisualizerEnabled = s.AudioVisualizerEnabled;

            WheelAction = s.WheelAction;
            MiddleClickAction = s.MiddleClickAction;
            Shortcuts.Load(s);

            var panels = s.EnabledPanels ?? ["music", "calendar", "shelf", "controlcenter"];
            PanelMusic = panels.Contains("music", StringComparer.OrdinalIgnoreCase);
            PanelCalendar = panels.Contains("calendar", StringComparer.OrdinalIgnoreCase);
            PanelShelf = panels.Contains("shelf", StringComparer.OrdinalIgnoreCase);
            PanelControlCenter = panels.Contains("controlcenter", StringComparer.OrdinalIgnoreCase);
            PanelStatus = panels.Contains("status", StringComparer.OrdinalIgnoreCase);

            RefreshMemory();

            try
            {
                using var monitorService = new MonitorService();
                MonitorCount = monitorService.GetMonitors().Count;
            }
            catch
            {
                MonitorCount = 1;
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void RefreshMemory()
    {
        try
        {
            var proc = Process.GetCurrentProcess();
            var memMb = proc.PrivateMemorySize64 / (1024.0 * 1024.0);
            MemoryUsageMb = Math.Round(memMb, 1);
            MemoryUsageText = $"{MemoryUsageMb:N1} MB / 60 MB";
            MemoryPct = Math.Clamp((MemoryUsageMb / 60.0) * 100.0, 0, 100);
        }
        catch
        {
            MemoryUsageMb = 38.0;
            MemoryUsageText = "38 MB / 60 MB";
            MemoryPct = 63.0;
        }
    }

    [RelayCommand]
    public void SelectPage(int index)
    {
        SelectedPageIndex = index;
        if (index == 6) // Performans
        {
            RefreshMemory();
        }
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_isLoading) return;
        if (e.PropertyName is nameof(SelectedPageIndex) or nameof(MemoryUsageMb) or nameof(MemoryUsageText) or nameof(MemoryPct) or nameof(MonitorCount))
            return;

        SaveSettings();

        if (e.PropertyName == nameof(NotchEnabled) && Application.Current is App appNotch)
        {
            appNotch.UpdateNotchState(NotchEnabled);
        }
        else if (e.PropertyName == nameof(DockEnabled) && Application.Current is App appDock)
        {
            appDock.UpdateDockState(DockEnabled);
        }
        else if (e.PropertyName is (nameof(DockStyle) or nameof(DockProximity) or nameof(DockHideInFullscreen) or nameof(DockIconSize) or nameof(HideWindowsTaskbar)) && Application.Current is App appReload)
        {
            appReload.ReloadDock();
        }
        else if (e.PropertyName is (nameof(NotchVisibility) or nameof(NotchHideInFullscreen) or nameof(MusicShowWaveform) or nameof(IdleDisplayMode) or nameof(NotchOpenBehavior) or nameof(WheelAction) or nameof(PanelMusic) or nameof(PanelCalendar) or nameof(PanelShelf) or nameof(PanelControlCenter) or nameof(PanelStatus)) && Application.Current is App appReloadNotch)
        {
            appReloadNotch.ReloadNotch();
        }
    }

    partial void OnLanguageChanged(string value)
    {
        if (_isLoading) return;
        try
        {
            var culture = new System.Globalization.CultureInfo(value == "en" ? "en-US" : "tr-TR");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
            System.Globalization.CultureInfo.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Koy.App.Resources.Loc.Instance.Refresh();
        }
        catch { }
    }

    [RelayCommand]
    public void SaveSettings()
    {
        StartupService.SetEnabled(RunAtStartup);

        var enabledPanels = new List<string>();
        if (PanelMusic) enabledPanels.Add("music");
        if (PanelCalendar) enabledPanels.Add("calendar");
        if (PanelShelf) enabledPanels.Add("shelf");
        if (PanelControlCenter) enabledPanels.Add("controlcenter");
        if (PanelStatus) enabledPanels.Add("status");
        if (enabledPanels.Count == 0) enabledPanels.Add("music");

        var current = _store.Load();
        var updated = current with
        {
            Language = Language,
            HideWindowsTaskbar = HideWindowsTaskbar,
            AutoCheckUpdates = AutoCheckUpdates,
            UpdateChannel = UpdateChannel,
            NotchEnabled = NotchEnabled,
            NotchVisibility = NotchVisibility,
            DockEnabled = DockEnabled,
            DockStyle = DockStyle,
            DockProximity = DockProximity,
            DockHideInFullscreen = DockHideInFullscreen,
            DockHoverMagnify = DockHoverMagnify,
            DockShowStartButton = DockShowStartButton,
            DockShowFolders = DockShowFolders,
            DockShowMinimized = DockShowMinimized,
            DockShowBadges = DockShowBadges,
            DockThumbnailPreview = DockThumbnailPreview,
            DockThumbnailDelayMs = DockThumbnailDelayMs,
            DockDodgeDelayMs = DockDodgeDelayMs,
            DockIconSize = DockIconSize,
            DockIndicatorStyle = DockIndicatorStyle,
            NotchHideInFullscreen = NotchHideInFullscreen,
            MusicShowWaveform = MusicShowWaveform,
            IdleDisplayMode = IdleDisplayMode,
            NotchOpenBehavior = NotchOpenBehavior,
            Theme = Theme,
            AccentColor = AccentColor,
            Material = Material,
            CornerRadius = CornerRadius,
            AnimationSpeed = AnimationSpeed,
            RespectReduceMotion = RespectReduceMotion,
            BatterySaverReduceMotion = BatterySaverReduceMotion,
            BatterySaverStopWaveform = BatterySaverStopWaveform,
            AudioVisualizerEnabled = AudioVisualizerEnabled,
            WheelAction = WheelAction,
            MiddleClickAction = MiddleClickAction,
            EnabledPanels = enabledPanels
        };

        _store.Save(updated);
        SettingsSaved?.Invoke();
    }

    [RelayCommand]
    public void SetAccentColor(string hex)
    {
        AccentColor = hex;
    }

    [RelayCommand]
    public void RestoreTaskbar()
    {
        TaskbarService.Restore();
    }

    [RelayCommand]
    public void ResetSettings()
    {
        // Kurulum sihirbazı tamamlandı bilgisi korunur; yoksa sıfırlama sihirbazı yeniden açardı.
        _store.Save(FirstRunPolicy.CreateReset(_store.Load()));
        LoadFromSettings();
        SettingsSaved?.Invoke();

        // Çalışan çentik ve dock yeni varsayılanlarla yeniden kurulur.
        if (Application.Current is App app)
            app.ReloadAllFeatures();
    }

    [RelayCommand]
    public void CopyDiagnostics()
    {
        RefreshMemory();
        var os = Environment.OSVersion.VersionString;
        var dotNet = Environment.Version.ToString();
        var proc = Process.GetCurrentProcess();

        var text = $"Koy v{AppVersion}\nOS: {os}\n.NET: {dotNet}\nPrivate Memory: {MemoryUsageMb:N1} MB\nProcess ID: {proc.Id}";
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
        }
    }

    [RelayCommand]
    public async Task CheckUpdatesAsync()
    {
        try
        {
            var mgr = new Velopack.UpdateManager(new Velopack.Sources.GithubSource("https://github.com/brhnshn/koy", null, UpdateChannel == "Önizleme"));
            if (mgr.IsInstalled)
            {
                var newVersion = await mgr.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    await mgr.DownloadUpdatesAsync(newVersion);
                    // ApplyUpdatesAndRestart süreci hemen sonlandırır (OnExit çalışmaz): görev çubuğunu önce geri getir.
                    Koy.App.App.SafeRestoreTaskbar();
                    mgr.ApplyUpdatesAndRestart(newVersion);
                    return;
                }
            }
            var now = DateTime.Now.ToString("HH:mm");
            LastCheckText = Language == "en" ? $"Last check: today {now}" : $"Son denetim: bugün {now}";
            AboutStatusText = Language == "en" ? $"Version {AppVersion} · GPLv3 · up to date" : $"Sürüm {AppVersion} · GPLv3 · güncel";
        }
        catch
        {
            var now = DateTime.Now.ToString("HH:mm");
            LastCheckText = Language == "en" ? $"Last check: today {now}" : $"Son denetim: bugün {now}";
        }
    }

    [RelayCommand]
    public void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/brhnshn/koy",
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    [RelayCommand]
    public void ExitApp()
    {
        TaskbarService.Restore();
        Application.Current?.Shutdown();
    }
}
