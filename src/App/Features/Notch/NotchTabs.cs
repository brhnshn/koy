using System.Windows;
using System.Windows.Media;
using Koy.App.Resources;
using Koy.Core.Notch;

namespace Koy.App.Features.Notch;

/// <summary>
/// Sekme çubuğunun tanımı. Çubuk bu listeden üretilir: Pano ve Sistem gibi yeni bir sayfa eklemek için
/// (1) <see cref="NotchPanel"/> ve <see cref="NotchPanels.Order"/>'a değer, (2) buraya bir satır (yazı anahtarı,
/// simge ve vurgu token'ı; ikisi de Tokens.xaml / IconsNotch.xaml'de hazır), (3) <c>NotchViewModel.CurrentPanelContent</c>'e
/// bir satır ve NotchWindow.xaml'e sayfanın DataTemplate'i yeter.
/// </summary>
internal static class NotchTabs
{
    private sealed record Definition(NotchPanel Panel, string LabelKey, string IconKey, string AccentKey);

    private static readonly Definition[] All =
    [
        new(NotchPanel.Home, "TabHome", "IconTabHome", "KoyNotchAccentHome"),
        new(NotchPanel.Focus, "TabFocus", "IconTabFocus", "KoyNotchAccentFocus"),
        new(NotchPanel.Shelf, "TabShelf", "IconTabShelf", "KoyNotchAccentShelf"),
        // Pano ve Sistem servisleri gelince açılır (simgeler ve renkler hazır):
        // new(NotchPanel.Clipboard, "TabClipboard", "IconTabClipboard", "KoyNotchAccentClipboard"),
        // new(NotchPanel.System, "TabSystem", "IconTabSystem", "KoyNotchAccentSystem"),
        new(NotchPanel.ControlCenter, "TabControlCenter", "IconTabControl", "KoyNotchAccentControl"),
    ];

    public static NotchTabViewModel Create(NotchPanel panel, bool selected)
    {
        var def = All.FirstOrDefault(d => d.Panel == panel);
        var label = def is null ? panel.ToString() : Strings.Get(def.LabelKey);
        var icon = def is null ? null : Application.Current?.TryFindResource(def.IconKey) as Geometry;
        var accent = def is null ? null : Application.Current?.TryFindResource(def.AccentKey) as Brush;
        return new NotchTabViewModel(panel, label, icon, accent) { IsSelected = selected };
    }
}
