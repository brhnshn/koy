using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace Koy.Platform.Shell;

public enum WindowBackdropType
{
    None = 1,
    Mica = 2,
    Acrylic = 3,
    MicaAlt = 4
}

public static class BackdropService
{
    public static void ApplyBackdrop(nint hwnd, WindowBackdropType type)
    {
        if (hwnd == 0)
            return;

        try
        {
            unsafe
            {
                var backdrop = (DWM_SYSTEMBACKDROP_TYPE)(int)type;
                PInvoke.DwmSetWindowAttribute(
                    (HWND)hwnd,
                    DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    &backdrop,
                    (uint)sizeof(DWM_SYSTEMBACKDROP_TYPE));
            }
        }
        catch
        {
        }
    }
}
