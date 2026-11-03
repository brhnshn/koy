using System.Collections.Concurrent;
using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// Kısayol (.lnk) veya çalıştırılabilir dosyalardan (.exe) 32x32 boyutunda simgeleri çıkaran
/// ve bellek önbelleğinde tutan hafif yardımcı sınıf.
/// </summary>
public static class IconHelper
{
    private static readonly ConcurrentDictionary<string, byte[]?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static byte[]? GetIconBytes(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return Cache.GetOrAdd(path, ExtractIconInternal);
    }

    private static unsafe byte[]? ExtractIconInternal(string path)
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
                return ConvertHIconToBmpBytes(shinfo.hIcon);
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

    internal static unsafe byte[]? ConvertHIconToBmpBytes(HICON hIcon)
    {
        if (hIcon.IsNull)
            return null;

        using var safeHandle = new IconSafeHandle(hIcon);
        if (!PInvoke.GetIconInfo(safeHandle, out var iconInfo))
            return null;

        try
        {
            var targetBmp = !iconInfo.hbmColor.IsNull ? iconInfo.hbmColor : iconInfo.hbmMask;
            if (targetBmp.IsNull)
                return null;

            var hdc = PInvoke.CreateCompatibleDC(default);
            try
            {
                var bih = new BITMAPINFOHEADER
                {
                    biSize = (uint)sizeof(BITMAPINFOHEADER),
                    biWidth = 32,
                    biHeight = 32,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0 // BI_RGB
                };

                var bi = new BITMAPINFO { bmiHeader = bih };
                var pixelData = new byte[32 * 32 * 4];

                using var bmpHandle = new BitmapSafeHandle(targetBmp);
                fixed (byte* pPixels = pixelData)
                {
                    var lines = PInvoke.GetDIBits(
                        hdc,
                        bmpHandle,
                        0,
                        32,
                        pPixels,
                        &bi,
                        DIB_USAGE.DIB_RGB_COLORS);

                    if (lines == 0)
                        return null;
                }

                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);

                // BMP Dosya Başlığı (14 bayt)
                writer.Write((byte)'B');
                writer.Write((byte)'M');
                uint offBits = 14 + 40;
                uint fileSize = offBits + (uint)pixelData.Length;
                writer.Write(fileSize);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write(offBits);

                // BMP Bilgi Başlığı (40 bayt)
                writer.Write((uint)40);
                writer.Write((int)32);
                writer.Write((int)32);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write((uint)0);
                writer.Write((uint)pixelData.Length);
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((uint)0);
                writer.Write((uint)0);

                // Piksel baytları
                writer.Write(pixelData);
                return ms.ToArray();
            }
            finally
            {
                if (hdc != default)
                    PInvoke.DeleteDC(hdc);
            }
        }
        finally
        {
            if (!iconInfo.hbmColor.IsNull)
                PInvoke.DeleteObject(iconInfo.hbmColor);
            if (!iconInfo.hbmMask.IsNull)
                PInvoke.DeleteObject(iconInfo.hbmMask);
        }
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
