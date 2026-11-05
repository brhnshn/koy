namespace Koy.Core.Network;

/// <summary>Arayüzde gösterilecek ağ durumunun kısa sınıfı (metni ve simgeyi bu belirler).</summary>
public enum NetworkDisplay
{
    Disconnected,
    NoInternet,
    WiFi,
    Ethernet,
    Cellular
}

/// <summary>
/// Ham <see cref="NetworkStatus"/> değerini çentikte gösterilecek duruma çevirir. Saf mantık: metin ve simge
/// seçimi App tarafında, karar burada (kural #11).
/// </summary>
public static class NetworkSummary
{
    public static NetworkDisplay Describe(NetworkStatus status) => status switch
    {
        { Kind: NetworkKind.None } => NetworkDisplay.Disconnected,
        { HasInternet: false } => NetworkDisplay.NoInternet,
        { Kind: NetworkKind.WiFi } => NetworkDisplay.WiFi,
        { Kind: NetworkKind.Cellular } => NetworkDisplay.Cellular,
        _ => NetworkDisplay.Ethernet
    };

    /// <summary>Kablosuz ağın adı; boşluklar kırpılır, ad yoksa ya da Wi-Fi değilse null.</summary>
    public static string? WiFiName(NetworkStatus status)
    {
        if (status.Kind != NetworkKind.WiFi)
            return null;

        var name = status.Name?.Trim();
        return string.IsNullOrEmpty(name) ? null : name;
    }

    /// <summary>İnternete çıkabilen bir bağlantı var mı (simge normal mi, üstü çizili mi çizilecek).</summary>
    public static bool IsConnected(NetworkStatus status) =>
        Describe(status) is not (NetworkDisplay.Disconnected or NetworkDisplay.NoInternet);
}
