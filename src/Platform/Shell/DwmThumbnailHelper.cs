using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// DWM pencere küçük resim (live window thumbnail) kayıt ve güncelleme yardımcısı.
/// </summary>
public static class DwmThumbnailHelper
{
    private const uint DWM_TNP_RECTDESTINATION = 0x00000001;
    private const uint DWM_TNP_OPACITY = 0x00000004;
    private const uint DWM_TNP_VISIBLE = 0x00000008;

    public static unsafe nint Register(nint destinationHwnd, nint sourceHwnd)
    {
        if (destinationHwnd == 0 || sourceHwnd == 0)
            return 0;

        nint thumbHandle = 0;
        var hr = PInvoke.DwmRegisterThumbnail(
            new HWND(destinationHwnd),
            new HWND(sourceHwnd),
            &thumbHandle);

        return hr.Succeeded ? thumbHandle : 0;
    }

    public static void Unregister(nint thumbHandle)
    {
        if (thumbHandle != 0)
        {
            PInvoke.DwmUnregisterThumbnail(thumbHandle);
        }
    }

    public static void Update(nint thumbHandle, int destWidth, int destHeight, byte opacity = 255)
    {
        if (thumbHandle == 0 || destWidth <= 0 || destHeight <= 0)
            return;

        var props = new DWM_THUMBNAIL_PROPERTIES
        {
            dwFlags = DWM_TNP_RECTDESTINATION | DWM_TNP_VISIBLE | DWM_TNP_OPACITY,
            rcDestination = new RECT
            {
                left = 0,
                top = 0,
                right = destWidth,
                bottom = destHeight
            },
            opacity = opacity,
            fVisible = true
        };

        PInvoke.DwmUpdateThumbnailProperties(thumbHandle, in props);
    }

    public static unsafe nint CreateChildHostWindow(nint parentHwnd, int width, int height)
    {
        if (parentHwnd == 0)
            return 0;

        fixed (char* pClassName = "STATIC")
        {
            var child = PInvoke.CreateWindowEx(
                0,
                pClassName,
                null,
                WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CLIPCHILDREN,
                0, 0,
                Math.Max(1, width),
                Math.Max(1, height),
                new HWND(parentHwnd),
                HMENU.Null,
                HINSTANCE.Null,
                null);

            return child.Value;
        }
    }

    public static void DestroyChildHostWindow(nint hwnd)
    {
        if (hwnd != 0)
        {
            PInvoke.DestroyWindow(new HWND(hwnd));
        }
    }

    public static void ResizeChildHostWindow(nint hwnd, int width, int height)
    {
        if (hwnd != 0 && width > 0 && height > 0)
        {
            PInvoke.SetWindowPos(
                new HWND(hwnd),
                HWND.Null,
                0, 0,
                width, height,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }
    }
}
