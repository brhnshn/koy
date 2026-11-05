using Koy.Core.Dock;
using Koy.Core.Visibility;

namespace Koy.Core.Settings;

/// <summary>
/// settings.json içeriği. Yeni alan eklerken varsayılan değer ver ki eski dosyalar bozulmasın.
/// Alan yeniden adlandırılır ya da silinirse <see cref="Version"/> artırılır.
/// Özellikler bilerek <c>set</c>: kaynak üreticili JSON, <c>init</c> özelliklerde dosyada olmayan alanı
/// varsayılan yerine sıfırlıyor (NotchEnabled false olup çentik hiç açılmıyordu).
/// </summary>
public sealed record AppSettings
{
    public int Version { get; set; } = 2;
    public string Language { get; set; } = "tr";
    public bool NotchEnabled { get; set; } = true;

    /// <summary>Kapalıysa Windows görev çubuğuna hiç dokunulmaz.</summary>
    public bool DockEnabled { get; set; } = true;

    public VisibilityMode NotchVisibility { get; set; } = VisibilityMode.Fixed;
    /// <summary>Fixed: Windows görev çubuğu gibi ekranın altında yer ayırır, büyütülmüş pencereler dock'un altında kalmaz.</summary>
    public VisibilityMode DockVisibility { get; set; } = VisibilityMode.Fixed;
    public bool DockHoverMagnify { get; set; } = true;

    /// <summary>Dock yerleşim stili: Yüzen kapsül (FloatingCapsule) veya Tam Genişlik (FullWidth).</summary>
    public DockStyleMode DockStyle { get; set; } = DockStyleMode.FloatingCapsule;

    /// <summary>Pencere yaklaşınca dock davranışı: DockDown (Aşağı yapış - varsayılan), Dodge (Çekil), ReserveSpace (Yer ayır), AlwaysHidden (Hep gizli).</summary>
    public DockProximityMode DockProximity { get; set; } = DockProximityMode.DockDown;

    /// <summary>Tam ekran bir uygulama veya oyun devredeyken dock'u otomatik gizle.</summary>
    public bool DockHideInFullscreen { get; set; } = true;

    public bool DockShowStartButton { get; set; } = true;
    public bool DockShowFolders { get; set; } = true;
    public bool DockShowMinimized { get; set; } = true;
    public bool DockShowBadges { get; set; } = true;
    public bool DockThumbnailPreview { get; set; } = true;
    public int DockThumbnailDelayMs { get; set; } = 400;
    public int DockDodgeDelayMs { get; set; } = 150;

    public string MonitorTarget { get; set; } = "Primary";
    public string? AccentColor { get; set; }

    /// <summary>Açıksa vurgu rengi Windows vurgu renginden gelir, AccentColor yok sayılır.</summary>
    public bool UseWindowsAccent { get; set; }

    /// <summary>İlk açılış sihirbazının tamamlanıp tamamlanmadığını belirtir.</summary>
    public bool FirstRunCompleted { get; set; }

    public string Theme { get; set; } = "Dark";
    /// <summary>Koy penceresinin köşe seçimi (bkz. CornerSetting): 6 Keskin, 12 Standart, 17 Yuvarlak. Eski varsayılan 18 Standart sayılır.</summary>
    public double CornerRadius { get; set; } = 12.0;
    public string AnimationSpeed { get; set; } = "Normal";
    public bool RespectReduceMotion { get; set; } = true;
    public bool HideWindowsTaskbar { get; set; } = true;
    public bool AutoCheckUpdates { get; set; } = true;
    public string UpdateChannel { get; set; } = "Kararlı";
    public bool BatterySaverReduceMotion { get; set; } = true;
    public string WeatherRefreshInterval { get; set; } = "30m";
    public double DockIconSize { get; set; } = 28.0;
    public int IdleDisplayMode { get; set; }
    public int NotchOpenBehavior { get; set; } = 1;
    public string Material { get; set; } = "Katı";
    public int WheelAction { get; set; }
    public int MiddleClickAction { get; set; }
    public int DockIndicatorStyle { get; set; }

    /// <summary>
    /// Genel kısayollar, "Ctrl+Alt+N" biçiminde metin; boş metin = atanmamış.
    /// Varsayılanlar Windows'un Win+... kısayollarıyla çakışmasın diye Ctrl+Alt+harf (bkz. ShortcutBindings).
    /// </summary>
    public string ShortcutToggleNotch { get; set; } = "Ctrl+Alt+N";
    public string ShortcutOpenShelf { get; set; } = "Ctrl+Alt+R";
    public string ShortcutToggleDock { get; set; } = "Ctrl+Alt+D";
    public string ShortcutOpenMenu { get; set; } = "Ctrl+Alt+K";
    public string ShortcutPlayPause { get; set; } = "Ctrl+Alt+Space";
    /// <summary>Pomodoro uzun mola süresi (dakika).</summary>
    public int PomodoroLongBreakMinutes { get; set; } = 15;
    /// <summary>Pomodoro kısa mola süresi (dakika).</summary>
    public int PomodoroShortBreakMinutes { get; set; } = 5;
    /// <summary>Pomodoro çalışma süresi (dakika).</summary>
    public int PomodoroWorkMinutes { get; set; } = 25;
    /// <summary>Parlaklık değişince çentikte 1,5 sn parlaklık göstergesi çıksın.</summary>
    public bool ShowBrightnessOsd { get; set; } = true;
    /// <summary>Ses değişince çentikte 1,5 sn ses göstergesi çıksın.</summary>
    public bool ShowVolumeOsd { get; set; } = true;
    /// <summary>Müzik çalarken çentikte dalga simgesi görünsün.</summary>
    public bool MusicShowWaveform { get; set; } = true;
    /// <summary>Tam ekran uygulama ya da oyun varken çentik gizlensin.</summary>
    public bool NotchHideInFullscreen { get; set; } = true;

    public List<string> ControlCenterLayout { get; set; } = ["wifi", "bluetooth", "donotdisturb"];
    public List<string> EnabledPanels { get; set; } = ["music", "controlcenter", "status", "calendar", "shelf"];
    public double WeatherLatitude { get; set; } = 41.01;
    public double WeatherLongitude { get; set; } = 28.97;
    public string WeatherCity { get; set; } = "İstanbul";

    public bool Equals(AppSettings? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Version == other.Version &&
               Language == other.Language &&
               NotchEnabled == other.NotchEnabled &&
               DockEnabled == other.DockEnabled &&
               NotchVisibility == other.NotchVisibility &&
               DockVisibility == other.DockVisibility &&
               DockHoverMagnify == other.DockHoverMagnify &&
               DockStyle == other.DockStyle &&
               DockProximity == other.DockProximity &&
               DockHideInFullscreen == other.DockHideInFullscreen &&
               DockShowStartButton == other.DockShowStartButton &&
               DockShowFolders == other.DockShowFolders &&
               DockShowMinimized == other.DockShowMinimized &&
               DockShowBadges == other.DockShowBadges &&
               DockThumbnailPreview == other.DockThumbnailPreview &&
               DockThumbnailDelayMs == other.DockThumbnailDelayMs &&
               DockDodgeDelayMs == other.DockDodgeDelayMs &&
               MonitorTarget == other.MonitorTarget &&
               AccentColor == other.AccentColor &&
               UseWindowsAccent == other.UseWindowsAccent &&
               FirstRunCompleted == other.FirstRunCompleted &&
               Theme == other.Theme &&
               CornerRadius.Equals(other.CornerRadius) &&
               AnimationSpeed == other.AnimationSpeed &&
               RespectReduceMotion == other.RespectReduceMotion &&
               HideWindowsTaskbar == other.HideWindowsTaskbar &&
               AutoCheckUpdates == other.AutoCheckUpdates &&
               UpdateChannel == other.UpdateChannel &&
               BatterySaverReduceMotion == other.BatterySaverReduceMotion &&
               WeatherRefreshInterval == other.WeatherRefreshInterval &&
               DockIconSize.Equals(other.DockIconSize) &&
               IdleDisplayMode == other.IdleDisplayMode &&
               NotchOpenBehavior == other.NotchOpenBehavior &&
               Material == other.Material &&
               WheelAction == other.WheelAction &&
               MiddleClickAction == other.MiddleClickAction &&
               DockIndicatorStyle == other.DockIndicatorStyle &&
               ShortcutToggleNotch == other.ShortcutToggleNotch &&
               ShortcutOpenShelf == other.ShortcutOpenShelf &&
               ShortcutToggleDock == other.ShortcutToggleDock &&
               ShortcutOpenMenu == other.ShortcutOpenMenu &&
               ShortcutPlayPause == other.ShortcutPlayPause &&
               PomodoroLongBreakMinutes == other.PomodoroLongBreakMinutes &&
               PomodoroShortBreakMinutes == other.PomodoroShortBreakMinutes &&
               PomodoroWorkMinutes == other.PomodoroWorkMinutes &&
               ShowBrightnessOsd == other.ShowBrightnessOsd &&
               ShowVolumeOsd == other.ShowVolumeOsd &&
               MusicShowWaveform == other.MusicShowWaveform &&
               NotchHideInFullscreen == other.NotchHideInFullscreen &&
               WeatherLatitude.Equals(other.WeatherLatitude) &&
               WeatherLongitude.Equals(other.WeatherLongitude) &&
               WeatherCity == other.WeatherCity &&
               (ControlCenterLayout == other.ControlCenterLayout || (ControlCenterLayout is not null && other.ControlCenterLayout is not null && ControlCenterLayout.SequenceEqual(other.ControlCenterLayout))) &&
               (EnabledPanels == other.EnabledPanels || (EnabledPanels is not null && other.EnabledPanels is not null && EnabledPanels.SequenceEqual(other.EnabledPanels)));
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Version);
        hash.Add(Language);
        hash.Add(NotchEnabled);
        hash.Add(DockEnabled);
        hash.Add(NotchVisibility);
        hash.Add(DockVisibility);
        hash.Add(DockHoverMagnify);
        hash.Add(DockStyle);
        hash.Add(DockProximity);
        hash.Add(DockHideInFullscreen);
        hash.Add(DockShowStartButton);
        hash.Add(DockShowFolders);
        hash.Add(DockShowMinimized);
        hash.Add(DockShowBadges);
        hash.Add(DockThumbnailPreview);
        hash.Add(DockThumbnailDelayMs);
        hash.Add(DockDodgeDelayMs);
        hash.Add(MonitorTarget);
        hash.Add(AccentColor);
        hash.Add(UseWindowsAccent);
        hash.Add(FirstRunCompleted);
        hash.Add(Theme);
        hash.Add(CornerRadius);
        hash.Add(AnimationSpeed);
        hash.Add(RespectReduceMotion);
        hash.Add(HideWindowsTaskbar);
        hash.Add(AutoCheckUpdates);
        hash.Add(UpdateChannel);
        hash.Add(BatterySaverReduceMotion);
        hash.Add(WeatherRefreshInterval);
        hash.Add(DockIconSize);
        hash.Add(IdleDisplayMode);
        hash.Add(NotchOpenBehavior);
        hash.Add(Material);
        hash.Add(WheelAction);
        hash.Add(MiddleClickAction);
        hash.Add(DockIndicatorStyle);
        hash.Add(ShortcutToggleNotch);
        hash.Add(ShortcutOpenShelf);
        hash.Add(ShortcutToggleDock);
        hash.Add(ShortcutOpenMenu);
        hash.Add(ShortcutPlayPause);
        hash.Add(PomodoroLongBreakMinutes);
        hash.Add(PomodoroShortBreakMinutes);
        hash.Add(PomodoroWorkMinutes);
        hash.Add(ShowBrightnessOsd);
        hash.Add(ShowVolumeOsd);
        hash.Add(MusicShowWaveform);
        hash.Add(NotchHideInFullscreen);
        hash.Add(WeatherLatitude);
        hash.Add(WeatherLongitude);
        hash.Add(WeatherCity);
        if (ControlCenterLayout is not null)
        {
            foreach (var item in ControlCenterLayout)
                hash.Add(item);
        }
        if (EnabledPanels is not null)
        {
            foreach (var item in EnabledPanels)
                hash.Add(item);
        }
        return hash.ToHashCode();
    }
}
