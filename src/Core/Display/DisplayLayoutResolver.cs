namespace Koy.Core.Display;

/// <summary>
/// Monitör listesi ve hedefleme ayarlarına göre (Primary, All, Specific)
/// bileşenin (Notch/Dock) konumlandırılacağı hedef monitörleri çözümleyen saf etki alanı bileşeni.
/// </summary>
public static class DisplayLayoutResolver
{
    /// <summary>
    /// Verilen metinsel ayar değerini (örn. "Primary", "All", veya özel monitör kimliği)
    /// <see cref="MonitorTargetMode"/> numaralandırmasına çevirir.
    /// </summary>
    public static MonitorTargetMode ParseMode(string? targetSetting)
    {
        if (string.Equals(targetSetting, "All", StringComparison.OrdinalIgnoreCase))
            return MonitorTargetMode.All;

        if (string.IsNullOrWhiteSpace(targetSetting) ||
            string.Equals(targetSetting, "Primary", StringComparison.OrdinalIgnoreCase))
            return MonitorTargetMode.Primary;

        return MonitorTargetMode.Specific;
    }

    /// <summary>
    /// Belirtilen hedef modu ve ayar değerine göre hedef monitör listesini çözümler.
    /// </summary>
    public static IReadOnlyList<DisplayMonitor> ResolveTargetMonitors(
        IReadOnlyList<DisplayMonitor> monitors,
        string targetSetting,
        MonitorTargetMode mode)
    {
        if (monitors == null || monitors.Count == 0)
            return [];

        return mode switch
        {
            MonitorTargetMode.All => monitors,
            MonitorTargetMode.Specific =>
            [
                monitors.FirstOrDefault(m =>
                    string.Equals(m.Id, targetSetting, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(m.Name, targetSetting, StringComparison.OrdinalIgnoreCase))
                ?? monitors.FirstOrDefault(m => m.IsPrimary)
                ?? monitors[0]
            ],
            _ =>
            [
                monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0]
            ]
        };
    }

    /// <summary>
    /// Metinsel ayar değerine göre otomatik mod tespiti yaparak hedef monitör listesini çözümler.
    /// </summary>
    public static IReadOnlyList<DisplayMonitor> ResolveTargetMonitors(
        IReadOnlyList<DisplayMonitor> monitors,
        string targetSetting)
    {
        var mode = ParseMode(targetSetting);
        return ResolveTargetMonitors(monitors, targetSetting, mode);
    }
}
