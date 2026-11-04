using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// Notch penceresinin Windows stil ayarları. Alt+Tab'dan gizler ve tıklamada odak çalmasını önler.
/// </summary>
public static class WindowStyles
{
    public static void ApplyToolWindowAndNoActivate(nint hwnd)
    {
        var handle = new HWND(hwnd);
        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        exStyle |= WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_NOACTIVATE;
        PInvoke.SetWindowLong(handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)exStyle);

        PInvoke.SetWindowPos(
            handle,
            HWND.Null,
            0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
    }

    /// <summary>
    /// Pencereyi tıklama geçirgen (WS_EX_TRANSPARENT) yapar ya da tık alabilir hale getirir.
    /// </summary>
    public static void SetClickThrough(nint hwnd, bool clickThrough)
    {
        var handle = new HWND(hwnd);
        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        if (clickThrough)
            exStyle |= WINDOW_EX_STYLE.WS_EX_TRANSPARENT;
        else
            exStyle &= ~WINDOW_EX_STYLE.WS_EX_TRANSPARENT;

        PInvoke.SetWindowLong(handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)exStyle);
        PInvoke.SetWindowPos(
            handle,
            HWND.Null,
            0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
    }

    /// <summary>
    /// Odak çalmadan pencereyi en üste (Topmost) taşır.
    /// </summary>
    public static void ReassertTopmost(nint hwnd)
    {
        var handle = new HWND(hwnd);
        PInvoke.SetWindowPos(
            handle,
            new HWND((nint)(-1)), // HWND_TOPMOST
            0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    /// <summary>
    /// Pencereyi ön plana (foreground) taşır.
    /// </summary>
    public static void SetForeground(nint hwnd)
    {
        if (hwnd == 0) return;
        PInvoke.SetForegroundWindow(new HWND(hwnd));
    }

    /// <summary>
    /// Pencerenin ekranı kaplayıp kaplamadığını (WS_MAXIMIZE) denetler.
    /// </summary>
    public static bool IsWindowMaximized(nint hwnd)
    {
        if (hwnd == 0) return false;
        var handle = new HWND(hwnd);
        var style = (uint)PInvoke.GetWindowLong(handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        const uint WS_MAXIMIZE = 0x01000000;
        return (style & WS_MAXIMIZE) != 0;
    }

    /// <summary>
    /// Verilen pencerenin ait olduğu süreç kimliğini (PID) döndürür.
    /// </summary>
    public static uint GetWindowProcessId(nint hwnd)
    {
        if (hwnd == 0) return 0;
        uint pid = 0;
        unsafe { PInvoke.GetWindowThreadProcessId(new HWND(hwnd), &pid); }
        return pid;
    }

    /// <summary>
    /// Verilen pencerenin bulunduğu monitörün tanıtıcısını (HMONITOR) döndürür.
    /// </summary>
    public static nint GetWindowMonitor(nint hwnd)
    {
        if (hwnd == 0) return 0;
        return PInvoke.MonitorFromWindow(new HWND(hwnd), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST).Value;
    }
}
