using System.IO;
using Koy.Core.Shell;
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
    private const int ABS_AUTOHIDE = TaskbarStatePolicy.AutoHide;

    private static readonly string StateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Koy",
        "taskbar-state.json");

    /// <summary>
    /// Görev çubuğunu gizler. Birden çok kez çağrılabilir (idempotent): zaten gizliyken kayıtlı "önceki durum"
    /// korunur, böylece bizim koyduğumuz AUTOHIDE kullanıcının gerçek durumu sanılıp üzerine yazılmaz.
    /// Explorer yeniden başlayınca da aynı çağrı gizlemeyi yeniden uygular.
    /// </summary>
    public static unsafe void Hide()
    {
        var saved = ReadSavedState();
        var primary = PInvoke.FindWindow("Shell_TrayWnd", null);
        var prevState = saved is { Hidden: true } s ? s.PreviousState : TaskbarStatePolicy.AlwaysOnTop;

        if (primary != HWND.Null)
        {
            var data = new APPBARDATA
            {
                cbSize = (uint)sizeof(APPBARDATA),
                hWnd = primary
            };
            var current = (int)PInvoke.SHAppBarMessage(ABM_GETSTATE, ref data);
            prevState = TaskbarStatePolicy.ChoosePreviousState(saved, current);

            data.lParam = new LPARAM((nint)ABS_AUTOHIDE);
            PInvoke.SHAppBarMessage(ABM_SETSTATE, ref data);
        }

        SaveState(new TaskbarSavedState(true, prevState));
        SetVisible(false);
    }

    /// <summary>
    /// Görev çubuğunu (birincil ve bütün ikincil monitörlerdekileri) geri getirir ve durumu önceki haline döndürür.
    /// Explorer çalışmıyorsa (birincil çubuk yok) durum dosyası silinmez; sonraki açılış yeniden dener.
    /// </summary>
    public static unsafe void Restore()
    {
        SetVisible(true);

        var saved = ReadSavedState();
        var primary = PInvoke.FindWindow("Shell_TrayWnd", null);
        if (primary == HWND.Null)
            return;

        var data = new APPBARDATA
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = primary,
            lParam = new LPARAM((nint)TaskbarStatePolicy.ResolveRestoreState(saved))
        };
        PInvoke.SHAppBarMessage(ABM_SETSTATE, ref data);

        DeleteState();
    }

    /// <summary>
    /// Gezgin, otomatik gizleme durumu değişince ya da kendi kendine görev çubuğunu yeniden gösterir;
    /// o zaman alt kenarda bizim dock'la birlikte ikinci bir çubuk çıkar. Birincil veya ikincil
    /// monitörlerdeki görev çubuklarından herhangi biri görünür olduysa tekrar gizler.
    /// </summary>
    public static void EnsureHidden()
    {
        var primary = PInvoke.FindWindow("Shell_TrayWnd", null);
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

        var primary = PInvoke.FindWindow("Shell_TrayWnd", null);
        if (primary != HWND.Null)
            PInvoke.ShowWindow(primary, command);

        // Diğer monitörlerdeki görev çubukları.
        var secondary = HWND.Null;
        while ((secondary = PInvoke.FindWindowEx(HWND.Null, secondary, "Shell_SecondaryTrayWnd", null)) != HWND.Null)
            PInvoke.ShowWindow(secondary, command);
    }

    private static void SaveState(TaskbarSavedState state)
    {
        try
        {
            var dir = Path.GetDirectoryName(StateFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(StateFilePath, TaskbarStatePolicy.Serialize(state, DateTime.UtcNow));
        }
        catch
        {
            // Durum dosyası yazılamasa bile ana iş akışı kesilmez
        }
    }

    private static TaskbarSavedState? ReadSavedState()
    {
        try
        {
            if (File.Exists(StateFilePath))
                return TaskbarStatePolicy.Parse(File.ReadAllText(StateFilePath));
        }
        catch
        {
        }
        return null;
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
