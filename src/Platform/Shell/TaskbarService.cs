using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
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
    private static readonly string StateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Koy",
        "taskbar-state.json");

    public static void Hide()
    {
        SetVisible(false);
        SaveState(hidden: true);
    }

    public static void Restore()
    {
        SetVisible(true);
        DeleteState();
    }

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

    private static void SaveState(bool hidden)
    {
        try
        {
            var dir = Path.GetDirectoryName(StateFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = $"{{\"taskbarHidden\": {hidden.ToString().ToLowerInvariant()}, \"timestamp\": \"{DateTime.UtcNow:O}\"}}";
            File.WriteAllText(StateFilePath, json);
        }
        catch
        {
            // Durum dosyası yazılamasa bile ana iş akışı kesilmez
        }
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
