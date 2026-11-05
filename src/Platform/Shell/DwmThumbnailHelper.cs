using Koy.Core.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace Koy.Platform.Shell;

/// <summary>
/// DWM pencere küçük resim (live window thumbnail) kayıt ve güncelleme yardımcısı.
/// Hedef pencere üst düzey olmalıdır: DWM alt pencereye (WS_CHILD) kaydı E_INVALIDARG ile reddeder.
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

    /// <summary>Kaynak pencerenin küçük resim boyutu (fiziksel piksel); bilinmiyorsa (0, 0).</summary>
    public static (int Width, int Height) QuerySourceSize(nint thumbHandle)
    {
        if (thumbHandle == 0)
            return (0, 0);

        var hr = PInvoke.DwmQueryThumbnailSourceSize(thumbHandle, out var size);
        return hr.Succeeded ? (size.cx, size.cy) : (0, 0);
    }

    /// <summary>
    /// Küçük resmi hedef pencerenin istemci alanında verilen dikdörtgene (fiziksel piksel) yerleştirir.
    /// </summary>
    public static bool Update(nint thumbHandle, WindowRect destination, byte opacity = 255)
    {
        if (thumbHandle == 0 || destination.IsEmpty)
            return false;

        var props = new DWM_THUMBNAIL_PROPERTIES
        {
            dwFlags = DWM_TNP_RECTDESTINATION | DWM_TNP_VISIBLE | DWM_TNP_OPACITY,
            rcDestination = new RECT
            {
                left = destination.Left,
                top = destination.Top,
                right = destination.Right,
                bottom = destination.Bottom
            },
            opacity = opacity,
            fVisible = true
        };

        return PInvoke.DwmUpdateThumbnailProperties(thumbHandle, in props).Succeeded;
    }
}
