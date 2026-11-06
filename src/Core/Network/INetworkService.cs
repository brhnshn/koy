namespace Koy.Core.Network;

public enum NetworkKind
{
    None,
    Ethernet,
    WiFi,
    Cellular
}

/// <summary>
/// İnternete çıkan bağlantının türü, erişimi, (kablosuzda) 0-5 arası sinyal çubuğu ve ağ adı (Wi-Fi SSID'si /
/// bağlantı profili adı; bilinmiyorsa null).
/// </summary>
public sealed record NetworkStatus(NetworkKind Kind, bool HasInternet, int SignalBars, string? Name = null)
{
    public static NetworkStatus Disconnected { get; } = new(NetworkKind.None, false, 0);
}

/// <summary>
/// Ağ bağlantısı durumu sözleşmesi. Dock'un durum alanı Windows görev çubuğundaki ağ simgesinin yerini tutar.
/// </summary>
public interface INetworkService : IDisposable
{
    NetworkStatus Current { get; }

    event EventHandler<NetworkStatus>? Changed;
}
