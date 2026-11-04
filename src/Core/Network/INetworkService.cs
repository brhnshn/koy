namespace Koy.Core.Network;

public enum NetworkKind
{
    None,
    Ethernet,
    WiFi,
    Cellular
}

/// <summary>İnternete çıkan bağlantının türü, erişimi ve (kablosuzda) 0-5 arası sinyal çubuğu.</summary>
public sealed record NetworkStatus(NetworkKind Kind, bool HasInternet, int SignalBars)
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
