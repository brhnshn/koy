namespace Koy.Core.Wireless;

/// <summary>
/// Wi-Fi ağ listesi, bağlanma ve bağlantıyı kesme sözleşmesi. Servis yalnız Wi-Fi sayfası açıkken yaşar;
/// tarama zamanlayıcısı servisin değil arayüzün işidir (sayfa kapanınca durur).
/// </summary>
public interface IWifiService : IDisposable
{
    /// <summary>Bir tarama yapar ve görünen ağları döndürür. Aynı anda gelen çağrılar sıraya girer.</summary>
    Task<WifiScanResult> ScanAsync();

    /// <summary>
    /// Ağa bağlanır. <paramref name="password"/> null ise kayıtlı profil (ya da açık ağ) kullanılır.
    /// Şifre yalnız bu çağrı süresince bellekte durur; günlüğe ve diske yazılmaz.
    /// </summary>
    Task<WifiConnectStatus> ConnectAsync(string ssid, string? password);

    /// <summary>Bağlı ağdan ayrılır.</summary>
    void Disconnect();

    /// <summary>Windows'ta Wi-Fi ayarları sayfasını açar.</summary>
    void OpenSettings();

    /// <summary>Windows'ta konum gizlilik sayfasını açar.</summary>
    void OpenLocationSettings();
}
