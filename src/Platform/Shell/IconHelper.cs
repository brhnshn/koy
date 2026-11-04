using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// Kısayol (.lnk), çalıştırılabilir dosya (.exe) ya da kabuk yolundan simgeyi saydam PNG olarak çıkaran
/// ve bellek önbelleğinde tutan hafif yardımcı sınıf.
/// </summary>
public static class IconHelper
{
    // 48 px: dock simgesi 24 DIP, %200 ölçekte bile büyütülmeden net kalır.
    private const int IconSize = 48;

    private static readonly ConcurrentDictionary<string, byte[]?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static byte[]? GetIconBytes(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return Cache.GetOrAdd(path, ExtractIconInternal);
    }

    private static byte[]? ExtractIconInternal(string path)
    {
        // Önce kabuğun resim fabrikası: .lnk için kısayol okunu eklemez ve "shell:AppsFolder\<AUMID>"
        // ile mağaza uygulamalarının gerçek simgesini verir. Olmazsa eski SHGetFileInfo yolu.
        return ExtractWithImageFactory(path) ?? ExtractWithFileInfo(path);
    }

    private static unsafe byte[]? ExtractWithImageFactory(string path)
    {
        try
        {
            PInvoke.SHCreateItemFromParsingName(path, null, typeof(IShellItemImageFactory).GUID, out var obj).ThrowOnFailure();
            if (obj is not IShellItemImageFactory factory)
                return null;

            try
            {
                factory.GetImage(new SIZE(IconSize, IconSize), SIIGBF.SIIGBF_ICONONLY, out var bitmap);
                using (bitmap)
                    return ConvertHBitmapToPng(new HBITMAP(bitmap.DangerousGetHandle()), IconSize);
            }
            finally
            {
                Marshal.ReleaseComObject(factory);
            }
        }
        catch
        {
            return null;
        }
    }

    private static unsafe byte[]? ExtractWithFileInfo(string path)
    {
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return null;

            SHFILEINFOW shinfo;
            var flags = SHGFI_FLAGS.SHGFI_ICON | SHGFI_FLAGS.SHGFI_LARGEICON;

            fixed (char* pPath = path)
            {
                var res = PInvoke.SHGetFileInfo(
                    pPath,
                    FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                    &shinfo,
                    (uint)sizeof(SHFILEINFOW),
                    flags);

                if (res == 0 || shinfo.hIcon.IsNull)
                    return null;
            }

            try
            {
                return ConvertHIconToPng(shinfo.hIcon);
            }
            finally
            {
                PInvoke.DestroyIcon(shinfo.hIcon);
            }
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? ConvertHIconToPng(HICON hIcon)
    {
        if (hIcon.IsNull)
            return null;

        using var safeHandle = new IconSafeHandle(hIcon);
        if (!PInvoke.GetIconInfo(safeHandle, out var iconInfo))
            return null;

        try
        {
            // SHGFI_LARGEICON 32 px döner.
            return iconInfo.hbmColor.IsNull ? null : ConvertHBitmapToPng(iconInfo.hbmColor, 32);
        }
        finally
        {
            if (!iconInfo.hbmColor.IsNull)
                PInvoke.DeleteObject(iconInfo.hbmColor);
            if (!iconInfo.hbmMask.IsNull)
                PInvoke.DeleteObject(iconInfo.hbmMask);
        }
    }

    private static unsafe byte[]? ConvertHBitmapToPng(HBITMAP hBitmap, int size)
    {
        if (hBitmap.IsNull)
            return null;

        var hdc = PInvoke.CreateCompatibleDC(default);
        try
        {
            var bi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize = (uint)sizeof(BITMAPINFOHEADER),
                    biWidth = size,
                    biHeight = -size, // yukarıdan aşağı satır sırası (PNG ile aynı)
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0 // BI_RGB
                }
            };

            var bgra = new byte[size * size * 4];
            using var bmpHandle = new BitmapSafeHandle(hBitmap);
            fixed (byte* pPixels = bgra)
            {
                if (PInvoke.GetDIBits(hdc, bmpHandle, 0, (uint)size, pPixels, &bi, DIB_USAGE.DIB_RGB_COLORS) == 0)
                    return null;
            }

            return PngEncoder.Encode(ToStraightRgba(bgra), size, size);
        }
        finally
        {
            if (hdc != default)
                PInvoke.DeleteDC(hdc);
        }
    }

    /// <summary>
    /// GDI'nin BGRA'sını PNG'nin düz (premultiplied olmayan) RGBA'sına çevirir.
    /// Kabuk resim fabrikası premultiplied alfa verir; eski simgelerde alfa hiç yoktur (hepsi 0).
    /// BMP ile WPF'e verildiğinde alfa kayboluyor ve simge siyah kare üstünde görünüyordu.
    /// </summary>
    private static byte[] ToStraightRgba(byte[] bgra)
    {
        var hasAlpha = false;
        var premultiplied = true;
        for (var i = 0; i < bgra.Length; i += 4)
        {
            var a = bgra[i + 3];
            if (a != 0)
                hasAlpha = true;
            if (bgra[i] > a || bgra[i + 1] > a || bgra[i + 2] > a)
                premultiplied = false;
        }

        var rgba = new byte[bgra.Length];
        for (var i = 0; i < bgra.Length; i += 4)
        {
            int b = bgra[i], g = bgra[i + 1], r = bgra[i + 2], a = bgra[i + 3];
            if (!hasAlpha)
            {
                a = 255;
            }
            else if (premultiplied && a is > 0 and < 255)
            {
                r = r * 255 / a;
                g = g * 255 / a;
                b = b * 255 / a;
            }

            rgba[i] = (byte)r;
            rgba[i + 1] = (byte)g;
            rgba[i + 2] = (byte)b;
            rgba[i + 3] = (byte)a;
        }

        return rgba;
    }

    private sealed class IconSafeHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public IconSafeHandle(HICON hIcon) : base(ownsHandle: false)
        {
            SetHandle(hIcon.Value);
        }

        protected override bool ReleaseHandle() => true;
    }

    private sealed class BitmapSafeHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public BitmapSafeHandle(HBITMAP hBmp) : base(ownsHandle: false)
        {
            SetHandle(hBmp.Value);
        }

        protected override bool ReleaseHandle() => true;
    }
}
