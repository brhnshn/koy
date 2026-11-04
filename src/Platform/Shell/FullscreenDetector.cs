using Koy.Core.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// Tam ekran uygulama algılayıcı.
/// SHQueryUserNotificationState ve ön plan penceresinin monitör sınırlarını kaplama durumunu denetler.
/// </summary>
public sealed class FullscreenDetector : IFullscreenDetector
{
    public nint NotchHandle { get; set; }

    public bool IsFullscreen()
    {
        // 1. Windows bildirim durumunu sorgula (DirectX özel tam ekran veya sunum modu)
        // Not: QUNS_BUSY bilerek dahil edilmez çünkü Windows 11 Odaklanma Yardımı (Do Not Disturb / Quiet Hours)
        // devredeyken tam ekran oyun olmasa da QUNS_BUSY döndürür ve çentiği yanlışlıkla gizler.
        var hr = PInvoke.SHQueryUserNotificationState(out var notificationState);
        if (hr.Succeeded)
        {
            if (notificationState is QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN or
                                     QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE)
            {
                return true;
            }
        }

        // 2. Ön plandaki pencerenin monitörü tamamen kaplayıp kaplamadığını kontrol et (tarayıcı tam ekran, video vb.)
        var fg = PInvoke.GetForegroundWindow();
        if (fg.IsNull || fg == PInvoke.GetDesktopWindow() || fg == PInvoke.GetShellWindow())
            return false;

        if (NotchHandle != 0 && fg.Value == NotchHandle)
            return false;

        // Masaüstü pencerelerini (Progman, WorkerW) veya görev çubuğunu yoksay
        unsafe
        {
            Span<char> buffer = stackalloc char[256];
            fixed (char* ptr = buffer)
            {
                var len = PInvoke.GetClassName(fg, ptr, 256);
                if (len > 0)
                {
                    var className = buffer[..len];
                    if (className.SequenceEqual("Progman") ||
                        className.SequenceEqual("WorkerW") ||
                        className.SequenceEqual("Shell_TrayWnd"))
                    {
                        return false;
                    }
                }
            }
        }

        var hMon = PInvoke.MonitorFromWindow(fg, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        if (hMon.Value == 0)
            return false;

        unsafe
        {
            var mi = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
            if (!PInvoke.GetMonitorInfo(hMon, ref mi))
                return false;

            if (!PInvoke.GetWindowRect(fg, out var wr))
                return false;

            // Standart başlıklı (WS_CAPTION) bir pencere maksimize olsa bile tam ekran oyun/video sayılmaz
            var style = (WINDOW_STYLE)PInvoke.GetWindowLong(fg, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            if (style.HasFlag(WINDOW_STYLE.WS_CAPTION))
                return false;

            return wr.left <= mi.rcMonitor.left &&
                   wr.top <= mi.rcMonitor.top &&
                   wr.right >= mi.rcMonitor.right &&
                   wr.bottom >= mi.rcMonitor.bottom;
        }
    }
}
