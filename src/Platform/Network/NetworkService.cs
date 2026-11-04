using Koy.Core.Network;
using Windows.Networking.Connectivity;

namespace Koy.Platform.Network;

/// <summary>
/// İnternet bağlantısı profilini (WinRT NetworkInformation) izler. Yoklama yok: Windows değişince bildirir.
/// </summary>
public sealed class NetworkService : INetworkService
{
    private bool _disposed;

    public NetworkStatus Current { get; private set; } = NetworkStatus.Disconnected;

    public event EventHandler<NetworkStatus>? Changed;

    public NetworkService()
    {
        Current = Read();
        NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
    }

    private void OnNetworkStatusChanged(object sender)
    {
        var next = Read();
        if (next == Current)
            return;

        Current = next;
        Changed?.Invoke(this, next);
    }

    private static NetworkStatus Read()
    {
        try
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile is null)
                return NetworkStatus.Disconnected;

            var hasInternet = profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            var bars = profile.GetSignalBars() ?? 0;

            if (profile.IsWlanConnectionProfile)
                return new NetworkStatus(NetworkKind.WiFi, hasInternet, bars);

            if (profile.IsWwanConnectionProfile)
                return new NetworkStatus(NetworkKind.Cellular, hasInternet, bars);

            return new NetworkStatus(NetworkKind.Ethernet, hasInternet, 0);
        }
        catch
        {
            return NetworkStatus.Disconnected;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
    }
}
