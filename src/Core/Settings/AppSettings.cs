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
    public bool DockEnabled { get; set; }

    public VisibilityMode NotchVisibility { get; set; } = VisibilityMode.Fixed;
    public VisibilityMode DockVisibility { get; set; } = VisibilityMode.Smart;
    public bool DockHoverMagnify { get; set; } = true;
    public string MonitorTarget { get; set; } = "Primary";
    public string? AccentColor { get; set; }

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
               MonitorTarget == other.MonitorTarget &&
               AccentColor == other.AccentColor &&
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
        hash.Add(MonitorTarget);
        hash.Add(AccentColor);
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
