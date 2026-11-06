using System.Diagnostics;

namespace Koy.Platform.Wireless;

/// <summary>Windows Ayarlar sayfalarını (ms-settings:) açar.</summary>
internal static class SettingsUri
{
    public static void Open(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // Ayarlar uygulaması kaldırılmış ya da politika ile engellenmiş olabilir.
        }
    }
}
