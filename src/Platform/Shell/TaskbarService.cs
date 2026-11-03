using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// Windows görev çubuğunu gizler ve geri getirir. Güvenlik kuralı: uygulama nasıl kapanırsa
/// kapansın görev çubuğu geri gelmeli (bkz. AGENTS.md). M4'e kadar yalnız <see cref="Restore"/> kullanılır.
/// </summary>
public static class TaskbarService
{
    public static void Hide() => SetVisible(false);

    public static void Restore() => SetVisible(true);

    private static void SetVisible(bool visible)
    {
        var command = visible ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_HIDE;

        var primary = PInvoke.FindWindow("Shell_TrayWnd", null);
        if (primary != HWND.Null)
            PInvoke.ShowWindow(primary, command);

        // Diğer monitörlerdeki görev çubukları.
        var secondary = HWND.Null;
        while ((secondary = PInvoke.FindWindowEx(HWND.Null, secondary, "Shell_SecondaryTrayWnd", null)) != HWND.Null)
            PInvoke.ShowWindow(secondary, command);
    }
}
