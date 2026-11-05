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
    public int Version { get; set; } = 1;
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

    /// <summary>Pencere yaklaşınca dock davranışı: Dodge (Akıllı Gizlenme - varsayılan), DockDown (Aşağı yapış), ReserveSpace (Yer ayır), AlwaysHidden (Hep gizli).</summary>
    public DockProximityMode DockProximity { get; set; } = DockProximityMode.Dodge;

    /// <summary>Tam ekran bir uygulama veya oyun devredeyken dock'u otomatik gizle.</summary>
    public bool DockHideInFullscreen { get; set; } = true;

    public bool DockShowStartButton { get; set; } = true;
    public bool DockShowFolders { get; set; } = true;
    public bool DockShowMinimized { get; set; } = true;

    /// <summary>Dock'ta sistem tepsisi düğmesi. Açıkken Koy uygulamaların tepsi simgelerini kendisi toplar.</summary>
    public bool DockShowTray { get; set; } = true;
    public bool DockShowBadges { get; set; } = true;
    public bool DockThumbnailPreview { get; set; } = true;
    public int DockThumbnailDelayMs { get; set; } = 400;
    public int DockDodgeDelayMs { get; set; } = 150;

    public string MonitorTarget { get; set; } = "Primary";
    public string? AccentColor { get; set; }

    /// <summary>İlk açılış sihirbazının tamamlanıp tamamlanmadığını belirtir.</summary>
    public bool FirstRunCompleted { get; set; }

    public string Theme { get; set; } = "Dark";
    public double CornerRadius { get; set; } = 18.0;
    public string AnimationSpeed { get; set; } = "Normal";
    public bool RespectReduceMotion { get; set; } = true;
    public bool HideWindowsTaskbar { get; set; } = true;
    public bool AutoCheckUpdates { get; set; } = true;
    public string UpdateChannel { get; set; } = "Kararlı";
    public bool BatterySaverReduceMotion { get; set; } = true;
    public bool BatterySaverStopWaveform { get; set; } = true;
    public string WeatherRefreshInterval { get; set; } = "30m";
    public bool AudioVisualizerEnabled { get; set; }
    public double DockIconSize { get; set; } = 28.0;
    public int IdleDisplayMode { get; set; }
    public int NotchOpenBehavior { get; set; } = 1;
    public string Material { get; set; } = "Katı";
    public int WheelAction { get; set; }
    public int MiddleClickAction { get; set; }
    public int DockIndicatorStyle { get; set; }

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
               DockShowTray == other.DockShowTray &&
               DockShowBadges == other.DockShowBadges &&
               DockThumbnailPreview == other.DockThumbnailPreview &&
               DockThumbnailDelayMs == other.DockThumbnailDelayMs &&
               DockDodgeDelayMs == other.DockDodgeDelayMs &&
               MonitorTarget == other.MonitorTarget &&
               AccentColor == other.AccentColor &&
               FirstRunCompleted == other.FirstRunCompleted &&
               Theme == other.Theme &&
               CornerRadius.Equals(other.CornerRadius) &&
               AnimationSpeed == other.AnimationSpeed &&
               RespectReduceMotion == other.RespectReduceMotion &&
               HideWindowsTaskbar == other.HideWindowsTaskbar &&
               AutoCheckUpdates == other.AutoCheckUpdates &&
               UpdateChannel == other.UpdateChannel &&
               BatterySaverReduceMotion == other.BatterySaverReduceMotion &&
               BatterySaverStopWaveform == other.BatterySaverStopWaveform &&
               WeatherRefreshInterval == other.WeatherRefreshInterval &&
               AudioVisualizerEnabled == other.AudioVisualizerEnabled &&
               DockIconSize.Equals(other.DockIconSize) &&
               IdleDisplayMode == other.IdleDisplayMode &&
               NotchOpenBehavior == other.NotchOpenBehavior &&
               Material == other.Material &&
               WheelAction == other.WheelAction &&
               MiddleClickAction == other.MiddleClickAction &&
               DockIndicatorStyle == other.DockIndicatorStyle &&
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
        hash.Add(DockShowTray);
        hash.Add(DockShowBadges);
        hash.Add(DockThumbnailPreview);
        hash.Add(DockThumbnailDelayMs);
        hash.Add(DockDodgeDelayMs);
        hash.Add(MonitorTarget);
        hash.Add(AccentColor);
        hash.Add(FirstRunCompleted);
        hash.Add(Theme);
        hash.Add(CornerRadius);
        hash.Add(AnimationSpeed);
        hash.Add(RespectReduceMotion);
        hash.Add(HideWindowsTaskbar);
        hash.Add(AutoCheckUpdates);
        hash.Add(UpdateChannel);
        hash.Add(BatterySaverReduceMotion);
        hash.Add(BatterySaverStopWaveform);
        hash.Add(WeatherRefreshInterval);
        hash.Add(AudioVisualizerEnabled);
        hash.Add(DockIconSize);
        hash.Add(IdleDisplayMode);
        hash.Add(NotchOpenBehavior);
        hash.Add(Material);
        hash.Add(WheelAction);
        hash.Add(MiddleClickAction);
        hash.Add(DockIndicatorStyle);
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
