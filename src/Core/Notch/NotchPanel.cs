namespace Koy.Core.Notch;

/// <summary>
/// Açık çentikte alttaki sekmelerden seçilebilen paneller. Sekme çubuğu bu sıraya göre dizilir
/// (<see cref="NotchPanels.Order"/>). Pano yalnız pano geçmişi ayarı açıkken görünür.
/// </summary>
public enum NotchPanel
{
    /// <summary>Ana: müzik ya da selamlama, hava durumu, sistem kartları, hızlı başlat, çalışan zamanlayıcı.</summary>
    Home,
    /// <summary>Odak: Pomodoro, mola, sayaç ve kronometre; görev listesi.</summary>
    Focus,
    Shelf,
    /// <summary>Pano geçmişi (yalnız <c>ClipboardHistoryEnabled</c> açıkken).</summary>
    Clipboard,
    /// <summary>Sistem: CPU, bellek, ağ hızı, Koy'un kendi belleği.</summary>
    System,
    ControlCenter
}

/// <summary>Panellerin sekme sırası ve ayarlardaki anahtar adları.</summary>
public static class NotchPanels
{
    /// <summary>Sekme çubuğundaki sıra. Ayarlar yalnız hangilerinin açık olduğunu söyler, sırayı değiştirmez.</summary>
    public static IReadOnlyList<NotchPanel> Order { get; } =
        [NotchPanel.Home, NotchPanel.Focus, NotchPanel.Shelf, NotchPanel.Clipboard, NotchPanel.System, NotchPanel.ControlCenter];

    /// <summary>
    /// <see cref="Settings.AppSettings.EnabledPanels"/> anahtarlarından açık paneller, sekme sırasıyla.
    /// Ana her zaman açıktır. Eski anahtarlar taşınır: "music" Ana, "calendar" Odak olur; "status" artık ayrı
    /// bir panel değil (saat, hava ve pil üstteki durum satırında), yok sayılır. Eski ayar dosyalarında "clipboard"
    /// ve "system" yoktur: bu sekmeler Koy penceresinden açılana kadar kapalı kalır (dosya bozulmaz).
    /// <paramref name="clipboardHistoryEnabled"/> kapalıysa Pano, anahtar listede olsa bile elenir.
    /// </summary>
    public static List<NotchPanel> ParseEnabled(IEnumerable<string>? keys, bool clipboardHistoryEnabled = true)
    {
        var enabled = new HashSet<NotchPanel> { NotchPanel.Home };
        if (keys is not null)
        {
            foreach (var key in keys)
            {
                if (TryParse(key, out var panel))
                    enabled.Add(panel);
            }
        }

        if (!clipboardHistoryEnabled)
            enabled.Remove(NotchPanel.Clipboard);

        return Order.Where(enabled.Contains).ToList();
    }

    /// <summary>Sekme sırasındaki yeri (listede olmayan sona gider).</summary>
    public static int SortIndex(NotchPanel panel)
    {
        for (var i = 0; i < Order.Count; i++)
        {
            if (Order[i] == panel)
                return i;
        }

        return int.MaxValue;
    }

    /// <summary>Ayarlara yazılan anahtar.</summary>
    public static string ToKey(NotchPanel panel) => panel switch
    {
        NotchPanel.Home => "home",
        NotchPanel.Focus => "focus",
        NotchPanel.Shelf => "shelf",
        NotchPanel.Clipboard => "clipboard",
        NotchPanel.System => "system",
        NotchPanel.ControlCenter => "controlcenter",
        _ => panel.ToString().ToLowerInvariant()
    };

    public static bool TryParse(string? key, out NotchPanel panel)
    {
        switch (key?.Trim().ToLowerInvariant())
        {
            case "home" or "music":
                panel = NotchPanel.Home;
                return true;
            case "focus" or "calendar":
                panel = NotchPanel.Focus;
                return true;
            case "shelf":
                panel = NotchPanel.Shelf;
                return true;
            case "clipboard":
                panel = NotchPanel.Clipboard;
                return true;
            case "system":
                panel = NotchPanel.System;
                return true;
            case "controlcenter":
                panel = NotchPanel.ControlCenter;
                return true;
            default:
                panel = default;
                return false;
        }
    }
}
