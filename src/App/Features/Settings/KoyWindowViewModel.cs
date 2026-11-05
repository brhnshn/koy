using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Resources;
using Koy.Core.Appearance;
using Koy.Core.Dock;
using Koy.Core.Settings;
using Koy.Core.Uninstall;
using Koy.Core.Visibility;
using Koy.Platform.Shell;

namespace Koy.App.Features.Settings;

public sealed partial class KoyWindowViewModel : ObservableObject
{
    private readonly SettingsStore _store;

    /// <summary>Kısayollar sayfası: tuş yakalama, kayıt ve hata durumu.</summary>
    public ShortcutsViewModel Shortcuts { get; }

    /// <summary>Ekranlar sayfası: monitör şeması ve ekran başına ayarlar.</summary>
    public DisplaysViewModel Displays { get; }

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
    [NotifyPropertyChangedFor(nameof(LastCheckText), nameof(AboutStatusText), nameof(CheckButtonText), nameof(CanCheckUpdates))]
    [NotifyCanExecuteChangedFor(nameof(CheckUpdatesCommand))]
    private bool _isCheckingUpdates;

    /// <summary>Son denetimin gerçek durumu; hiç denetim yapılmadıysa "Henüz denetlenmedi".</summary>
    public string LastCheckText
    {
        get
        {
            if (IsCheckingUpdates)
                return Strings.Get("UpdateChecking");
            return UpdateStatus.LastResult switch
            {
                UpdateCheckResult.Failed => Strings.Get("UpdateCheckFailed"),
                UpdateCheckResult.NotInstalled => Strings.Get("UpdateNotInstalled"),
                _ when UpdateStatus.LastCheck is { } t => string.Format(Strings.Get("UpdateLastCheck"), FormatCheckTime(t, DateTime.Now)),
                _ => Strings.Get("UpdateNeverChecked")
            };
        }
    }

    public string CheckButtonText => Strings.Get(IsCheckingUpdates ? "UpdateChecking" : "CheckNow");
    public bool CanCheckUpdates => !IsCheckingUpdates;

    private static string FormatCheckTime(DateTime t, DateTime now) =>
        t.Date == now.Date ? string.Format(Strings.Get("UpdateToday"), t.ToString("HH:mm")) : t.ToString("d MMM HH:mm");

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
    private bool _dockShowBadges = true;

    [ObservableProperty]
    private bool _dockShowStartButton = true;

    [ObservableProperty]
    private bool _dockShowFolders = true;

    [ObservableProperty]
    private bool _dockShowMinimized = true;

    [ObservableProperty]
    private bool _dockShowTray = true;

    [ObservableProperty]
    private bool _dockThumbnailPreview = true;

    [ObservableProperty]
    private int _dockThumbnailDelayMs = 400;

    // --- EKRANLAR ---
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
    private string _theme = "Dark";

    [ObservableProperty]
    private string _accentColor = "#3B82F6";

    [ObservableProperty]
    private bool _useWindowsAccent;

    [ObservableProperty]
    private string _material = "Katı";

    [ObservableProperty]
    private double _cornerRadius = 12.0;

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
    private double _memoryUsageMb;

    [ObservableProperty]
    private string _memoryUsageText = "–";

    [ObservableProperty]
    private double _memoryPct;

    /// <summary>0: bütçe içinde, 1: bütçenin %80'i aşıldı, 2: 60 MB bütçesi aşıldı.</summary>
    [ObservableProperty]
    private int _memoryLevel;

    [ObservableProperty]
    private bool _batterySaverReduceMotion = true;

    [ObservableProperty]
    private string _weatherRefreshInterval = "30m"; // 30m, 1h, startup

    [ObservableProperty]
    private int _pomodoroWorkMinutes = 25;

    [ObservableProperty]
    private int _pomodoroShortBreakMinutes = 5;

    [ObservableProperty]
    private int _pomodoroLongBreakMinutes = 15;

    /// <summary>Pomodoro süre seçenekleri (dakika). Ayar dosyasına elle yazılan başka değerler de çalışır.</summary>
    public int[] PomodoroWorkOptions { get; } = [15, 20, 25, 30, 45, 50, 60];
    public int[] PomodoroShortBreakOptions { get; } = [3, 5, 10, 15];
    public int[] PomodoroLongBreakOptions { get; } = [10, 15, 20, 30];

    // --- HAKKINDA ---
    /// <summary>Çalışan derlemenin sürümü (AssemblyInformationalVersion; "+commit" eki atılır).</summary>
    public string AppVersion { get; } = AppInfo.Version;

    /// <summary>Hakkında kimlik kartı: sürüm, lisans ve güncelleme durumu (denetim yapılmadıysa "güncel" denmez).</summary>
    public string AboutStatusText
    {
        get
        {
            var line = string.Format(Strings.Get("AboutVersionLine"), AppVersion);
            var state = IsCheckingUpdates ? Strings.Get("UpdateChecking") : UpdateStatus.LastResult switch
            {
                UpdateCheckResult.UpToDate => Strings.Get("UpdateStateUpToDate"),
                UpdateCheckResult.Failed => Strings.Get("UpdateStateFailed"),
                _ => Strings.Get("UpdateStateUnknown")
            };
            return $"{line} · {state}";
        }
    }

    public event Action? SettingsSaved;

    private bool _isLoading;

    public KoyWindowViewModel(SettingsStore? store = null)
    {
        _store = store ?? new SettingsStore(SettingsStore.DefaultPath);
        Shortcuts = new ShortcutsViewModel(_store);
        Displays = new DisplaysViewModel(_store);
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
            DockShowTray = s.DockShowTray;
            DockShowBadges = s.DockShowBadges;
            DockThumbnailPreview = s.DockThumbnailPreview;
            DockThumbnailDelayMs = s.DockThumbnailDelayMs;
            DockDodgeDelayMs = s.DockDodgeDelayMs;
            DockIconSize = s.DockIconSize > 0 ? s.DockIconSize : 28.0;
            DockIndicatorStyle = s.DockIndicatorStyle;

            NotchHideInFullscreen = s.NotchHideInFullscreen;
            ShowVolumeOsd = s.ShowVolumeOsd;
            ShowBrightnessOsd = s.ShowBrightnessOsd;
            MusicShowWaveform = s.MusicShowWaveform;
            IdleDisplayMode = s.IdleDisplayMode;
            NotchOpenBehavior = s.NotchOpenBehavior;

            // Eski sürümlerin karışık değerleri ("Koyu", 18 vb.) seçenek değerlerine indirilir.
            Theme = ThemeSetting.Normalize(s.Theme);
            AccentColor = s.AccentColor ?? AccentPalette.DefaultHex;
            UseWindowsAccent = s.UseWindowsAccent;
            Material = MaterialSetting.Normalize(s.Material);
            CornerRadius = CornerSetting.ToStored(CornerSetting.Resolve(s.CornerRadius));
            AnimationSpeed = MotionPolicy.NormalizeSpeed(s.AnimationSpeed);
            RespectReduceMotion = s.RespectReduceMotion;
            BatterySaverReduceMotion = s.BatterySaverReduceMotion;
            WeatherRefreshInterval = s.WeatherRefreshInterval;
            PomodoroWorkMinutes = s.PomodoroWorkMinutes;
            PomodoroShortBreakMinutes = s.PomodoroShortBreakMinutes;
            PomodoroLongBreakMinutes = s.PomodoroLongBreakMinutes;

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
            MemoryLevel = MemoryUsageMb > 60 ? 2 : MemoryUsageMb >= 48 ? 1 : 0;
        }
        catch
        {
            // Ölçülemediyse uydurma değer gösterilmez.
            MemoryUsageMb = 0;
            MemoryUsageText = "–";
            MemoryPct = 0;
            MemoryLevel = 0;
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
        if (e.PropertyName is nameof(SelectedPageIndex) or nameof(MemoryUsageMb) or nameof(MemoryUsageText) or nameof(MemoryPct) or nameof(MemoryLevel)
            or nameof(IsResetConfirming) or nameof(UninstallAvailability) or nameof(CanUninstall) or nameof(UninstallRowDescription)
            or nameof(IsUninstallConfirming) or nameof(KeepSettingsOnUninstall) or nameof(UninstallError) or nameof(IsCheckingUpdates) or nameof(LastCheckText) or nameof(AboutStatusText) or nameof(CheckButtonText) or nameof(CanCheckUpdates))
            return;

        SaveSettings();

        if (Application.Current is not App app)
            return;

        if (e.PropertyName is nameof(Theme) or nameof(AccentColor) or nameof(UseWindowsAccent) or nameof(Material)
            or nameof(CornerRadius) or nameof(AnimationSpeed) or nameof(RespectReduceMotion) or nameof(BatterySaverReduceMotion))
        {
            // Görünüm ayarları açık pencerelere anında yansır.
            ThemeManager.Apply(app, _store.Load());
        }

        if (e.PropertyName == nameof(NotchEnabled))
        {
            app.UpdateNotchState(NotchEnabled);
        }
        else if (e.PropertyName == nameof(DockEnabled))
        {
            app.UpdateDockState(DockEnabled);
        }
        else
        {
            // Özellikler ayarı kurulurken okur; seçenek değişince ilgili özellik yeni ayarla yeniden kurulur.
            if (AffectsDock(e.PropertyName))
                app.ReloadDock();
            if (AffectsNotch(e.PropertyName))
                app.ReloadNotch();
        }
    }

    private static bool AffectsDock(string? name) =>
        name is not null && name != nameof(DockEnabled) &&
        (name.StartsWith("Dock", StringComparison.Ordinal) ||
         name is nameof(HideWindowsTaskbar) or nameof(BatterySaverReduceMotion) or nameof(MiddleClickAction));

    private static bool AffectsNotch(string? name) =>
        name is nameof(NotchVisibility) or nameof(NotchHideInFullscreen) or nameof(MusicShowWaveform)
            or nameof(ShowVolumeOsd) or nameof(ShowBrightnessOsd) or nameof(IdleDisplayMode)
            or nameof(NotchOpenBehavior) or nameof(WheelAction) or nameof(BatterySaverReduceMotion)
            or nameof(WeatherRefreshInterval) or nameof(PomodoroWorkMinutes) or nameof(PomodoroShortBreakMinutes) or nameof(PomodoroLongBreakMinutes)
            or nameof(PanelMusic) or nameof(PanelCalendar) or nameof(PanelShelf)
            or nameof(PanelControlCenter) or nameof(PanelStatus);

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
        // Kaldırma başladıysa ayar dosyası yeniden oluşmasın (pencereler kapanırken kaydetmeye çalışır).
        if (AppUninstaller.InProgress)
            return;

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
            DockShowTray = DockShowTray,
            DockShowBadges = DockShowBadges,
            DockThumbnailPreview = DockThumbnailPreview,
            DockThumbnailDelayMs = DockThumbnailDelayMs,
            DockDodgeDelayMs = DockDodgeDelayMs,
            DockIconSize = DockIconSize,
            DockIndicatorStyle = DockIndicatorStyle,
            NotchHideInFullscreen = NotchHideInFullscreen,
            ShowVolumeOsd = ShowVolumeOsd,
            ShowBrightnessOsd = ShowBrightnessOsd,
            MusicShowWaveform = MusicShowWaveform,
            IdleDisplayMode = IdleDisplayMode,
            NotchOpenBehavior = NotchOpenBehavior,
            Theme = Theme,
            AccentColor = AccentColor,
            UseWindowsAccent = UseWindowsAccent,
            Material = Material,
            CornerRadius = CornerRadius,
            AnimationSpeed = AnimationSpeed,
            RespectReduceMotion = RespectReduceMotion,
            BatterySaverReduceMotion = BatterySaverReduceMotion,
            WeatherRefreshInterval = WeatherRefreshInterval,
            PomodoroWorkMinutes = PomodoroWorkMinutes,
            PomodoroShortBreakMinutes = PomodoroShortBreakMinutes,
            PomodoroLongBreakMinutes = PomodoroLongBreakMinutes,
            WheelAction = WheelAction,
            MiddleClickAction = MiddleClickAction,
            EnabledPanels = enabledPanels
        };

        _store.Save(updated);
        SettingsSaved?.Invoke();
    }

    [RelayCommand]
    public void RestoreTaskbar()
    {
        TaskbarService.Restore();
        // Dock çalışıyorsa Koy'un tepsi penceresi de bırakılır (görev çubuğu görünürken Shell_TrayWnd sahipliği alınmaz).
        (Application.Current as App)?.NotifyTaskbarRestoredManually();
    }

    /// <summary>Sıfırlama iki adımlı: ilk tıklama onay ister (yanlışlıkla silmeyi önler).</summary>
    [ObservableProperty]
    private bool _isResetConfirming;

    [RelayCommand]
    public void RequestReset() => IsResetConfirming = true;

    [RelayCommand]
    public void CancelReset() => IsResetConfirming = false;

    [RelayCommand]
    public void ResetSettings()
    {
        IsResetConfirming = false;
        // Kurulum sihirbazı tamamlandı bilgisi korunur; yoksa sıfırlama sihirbazı yeniden açardı.
        _store.Save(FirstRunPolicy.CreateReset(_store.Load()));
        LoadFromSettings();
        SettingsSaved?.Invoke();
        if (Application.Current is { } current)
            ThemeManager.Apply(current, _store.Load());

        // Çalışan çentik ve dock yeni varsayılanlarla yeniden kurulur.
        if (Application.Current is App app)
            app.ReloadAllFeatures();
    }

    // --- KALDIRMA ---
    /// <summary>Uygulama içinden kaldırma mümkün mü; kurulu olmayan sürümde düğme kapalı kalır.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUninstall), nameof(UninstallRowDescription))]
    private UninstallAvailability _uninstallAvailability = AppUninstaller.GetAvailability();

    public bool CanUninstall => UninstallAvailability == UninstallAvailability.Available;

    public string UninstallRowDescription =>
        Strings.Get(CanUninstall ? "UninstallRowDesc" : "UninstallOnlyInstalled");

    /// <summary>Onay görünümü açık mı.</summary>
    [ObservableProperty]
    private bool _isUninstallConfirming;

    /// <summary>"Ayarlarımı sakla": varsayılan işaretli; kaldırma geri alınamayan tek yönlü iş, güvenli taraf saklamak.</summary>
    [ObservableProperty]
    private bool _keepSettingsOnUninstall = true;

    [ObservableProperty]
    private string _uninstallError = "";

    [RelayCommand(CanExecute = nameof(CanUninstall))]
    public void RequestUninstall()
    {
        UninstallError = "";
        KeepSettingsOnUninstall = true;
        IsUninstallConfirming = true;
    }

    [RelayCommand]
    public void CancelUninstall() => IsUninstallConfirming = false;

    [RelayCommand]
    public void ConfirmUninstall()
    {
        if (!CanUninstall)
            return;

        if (AppUninstaller.Begin(KeepSettingsOnUninstall))
            return; // Koy kapanıyor

        UninstallError = Strings.Get("UninstallFailed");
    }

    partial void OnUninstallAvailabilityChanged(UninstallAvailability value) => RequestUninstallCommand.NotifyCanExecuteChanged();

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

    [RelayCommand(CanExecute = nameof(CanCheckUpdates))]
    public async Task CheckUpdatesAsync()
    {
        IsCheckingUpdates = true;
        try
        {
            var mgr = new Velopack.UpdateManager(new Velopack.Sources.GithubSource("https://github.com/brhnshn/koy", null, UpdateChannel == "Önizleme"));
            if (!mgr.IsInstalled)
            {
                // Geliştirme çalıştırmasında denetim yapılamaz; başarı gibi gösterilmez.
                UpdateStatus.Record(UpdateCheckResult.NotInstalled);
                return;
            }

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion != null)
            {
                await mgr.DownloadUpdatesAsync(newVersion);
                // ApplyUpdatesAndRestart süreci hemen sonlandırır (OnExit çalışmaz): görev çubuğunu önce geri getir.
                Koy.App.App.SafeRestoreTaskbar();
                mgr.ApplyUpdatesAndRestart(newVersion);
                return;
            }

            UpdateStatus.Record(UpdateCheckResult.UpToDate);
        }
        catch
        {
            UpdateStatus.Record(UpdateCheckResult.Failed);
        }
        finally
        {
            IsCheckingUpdates = false;
            OnPropertyChanged(nameof(LastCheckText));
            OnPropertyChanged(nameof(AboutStatusText));
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
