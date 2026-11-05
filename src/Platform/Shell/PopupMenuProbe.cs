using Koy.Core.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Koy.Platform.Shell;

/// <summary>
/// Ekranda görünen bağlam menülerini (Win32 menü penceresi sınıfı "#32768", alt menüler de dahil) ve pencere
/// dikdörtgenlerini sorgular. Dock'taki tepsi penceresi, uygulamanın bağlam menüsü açıkken kapanmasın ve menü
/// kapanınca kapansın diye yoklar; kanca kurmaz.
/// </summary>
public static class PopupMenuProbe
{
    private const string MenuClassName = "#32768";

    /// <summary>Görünen menü pencerelerinin ekran dikdörtgenleri; menü yoksa boş.</summary>
    public static IReadOnlyList<WindowRect> GetVisibleMenuRects()
    {
        List<WindowRect>? result = null;
        var hwnd = HWND.Null;
        while (!(hwnd = PInvoke.FindWindowEx(HWND.Null, hwnd, MenuClassName, null)).IsNull)
        {
            if (!PInvoke.IsWindowVisible(hwnd) || !PInvoke.GetWindowRect(hwnd, out var rect))
                continue;

            (result ??= []).Add(new WindowRect(rect.left, rect.top, rect.right, rect.bottom));
        }

        return result ?? [];
    }

    /// <summary>Pencere tanıtıcısının ekran dikdörtgeni; geçersizse false.</summary>
    public static bool TryGetWindowRect(nint handle, out WindowRect rect)
    {
        rect = WindowRect.Empty;
        if (handle == 0 || !PInvoke.GetWindowRect(new HWND(handle), out var r))
            return false;

        rect = new WindowRect(r.left, r.top, r.right, r.bottom);
        return true;
    }
}
