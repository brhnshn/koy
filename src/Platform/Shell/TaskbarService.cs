using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// Windows görev çubuğunu gizler ve geri getirir.
/// Güvenlik kuralı (AGENTS.md #7): Çökse bile görev çubuğu geri gelmeli.
/// Görev çubuğu gizlendiğinde durum %AppData%\Koy\taskbar-state.json dosyasına yazılır,
/// geri getirildiğinde ise bu dosya silinir. Açılışta bu dosya kontrol edilir.
/// </summary>
public static class TaskbarService
{
    private const uint ABM_GETSTATE = 0x00000004;
    private const uint ABM_SETSTATE = 0x0000000A;
    private const int ABS_AUTOHIDE = 0x00000001;
    private const int ABS_ALWAYSONTOP = 0x00000002;

    private static readonly string StateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Koy",
        "taskbar-state.json");

    public static unsafe void Hide()
    {
        var primary = ShellTrayLocator.FindExplorerTray();
        var prevState = ABS_ALWAYSONTOP;

        if (primary != HWND.Null)
        {
            var data = new APPBARDATA
            {
                cbSize = (uint)sizeof(APPBARDATA),
                hWnd = primary
            };
            prevState = (int)PInvoke.SHAppBarMessage(ABM_GETSTATE, ref data);

            data.lParam = new LPARAM((nint)ABS_AUTOHIDE);
            PInvoke.SHAppBarMessage(ABM_SETSTATE, ref data);
        }

        SaveState(hidden: true, prevState);
        SetVisible(false);
    }

    public static unsafe void Restore()
    {
        SetVisible(true);

        var prevState = ReadSavedState();
        var primary = ShellTrayLocator.FindExplorerTray();
        if (primary != HWND.Null)
        {
            var data = new APPBARDATA
            {
                cbSize = (uint)sizeof(APPBARDATA),
                hWnd = primary,
                lParam = new LPARAM((nint)(prevState > 0 ? prevState : ABS_ALWAYSONTOP))
            };
            PInvoke.SHAppBarMessage(ABM_SETSTATE, ref data);
        }

        DeleteState();
    }

    /// <summary>
    /// Gezgin, otomatik gizleme durumu değişince ya da kendi kendine görev çubuğunu yeniden gösterir;
    /// o zaman alt kenarda bizim dock'la birlikte ikinci bir çubuk çıkar. Birincil veya ikincil
    /// monitörlerdeki görev çubuklarından herhangi biri görünür olduysa tekrar gizler.
    /// </summary>
    public static void EnsureHidden()
    {
        var primary = ShellTrayLocator.FindExplorerTray();
        if (primary != HWND.Null && PInvoke.IsWindowVisible(primary))
        {
            SetVisible(false);
            return;
        }

        var secondary = HWND.Null;
        while ((secondary = PInvoke.FindWindowEx(HWND.Null, secondary, "Shell_SecondaryTrayWnd", null)) != HWND.Null)
        {
            if (PInvoke.IsWindowVisible(secondary))
            {
                SetVisible(false);
                return;
            }
        }
    }

    private static void SetVisible(bool visible)
    {
        var command = visible ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_HIDE;

        var primary = ShellTrayLocator.FindExplorerTray();
        if (primary != HWND.Null)
            PInvoke.ShowWindow(primary, command);

        // Diğer monitörlerdeki görev çubukları.
        var secondary = HWND.Null;
        while ((secondary = PInvoke.FindWindowEx(HWND.Null, secondary, "Shell_SecondaryTrayWnd", null)) != HWND.Null)
            PInvoke.ShowWindow(secondary, command);
    }

    private static void SaveState(bool hidden, int previousState)
    {
        try
        {
            var dir = Path.GetDirectoryName(StateFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = $"{{\"taskbarHidden\": {hidden.ToString().ToLowerInvariant()}, \"previousState\": {previousState}, \"timestamp\": \"{DateTime.UtcNow:O}\"}}";
            File.WriteAllText(StateFilePath, json);
        }
        catch
        {
            // Durum dosyası yazılamasa bile ana iş akışı kesilmez
        }
    }

    private static int ReadSavedState()
    {
        try
        {
            if (File.Exists(StateFilePath))
            {
                var content = File.ReadAllText(StateFilePath);
                var idx = content.IndexOf("\"previousState\":", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var start = idx + 16;
                    var end = content.IndexOfAny([',', '}'], start);
                    var numStr = (end > start ? content[start..end] : content[start..]).Trim();
                    if (int.TryParse(numStr, out var val))
                        return val;
                }
            }
        }
        catch
        {
        }
        return ABS_ALWAYSONTOP;
    }

    private static void DeleteState()
    {
        try
        {
            if (File.Exists(StateFilePath))
                File.Delete(StateFilePath);
        }
        catch
        {
            // Silme hatası yutulur
        }
    }
}
