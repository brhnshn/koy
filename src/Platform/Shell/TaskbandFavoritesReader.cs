using Microsoft.Win32;

namespace Koy.Platform.Shell;

/// <summary>
/// Windows'un görev çubuğu sabitleme sırasını tutan belgesiz ikili değeri okur
/// (HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband\Favorites). Yalnız okur, yazmaz;
/// biçim sürümlere göre değişebileceği için çözümleme Core'daki <c>PinnedOrder.SeedFromTaskband</c>'dadır ve
/// tek seferlik tohum olarak kullanılır. Okunamazsa null döner.
/// </summary>
public static class TaskbandFavoritesReader
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband";

    public static byte[]? Read()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
            return key?.GetValue("Favorites") as byte[];
        }
        catch
        {
            return null;
        }
    }
}
