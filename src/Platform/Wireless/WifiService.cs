using Koy.Core.Diagnostics;
using Koy.Core.Wireless;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;

namespace Koy.Platform.Wireless;

/// <summary>
/// <see cref="IWifiService"/> uygulaması: <c>Windows.Devices.WiFi.WiFiAdapter</c> (tarama, bağlanma, kesme) ve yalnız
/// kayıtlı profil adları için wlanapi. Servis Wi-Fi sayfası açıkken yaşar; kapanınca adaptör nesneleri bırakılır.
/// </summary>
public sealed class WifiService : IWifiService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private WiFiAdapter? _adapter;
    private WifiAccess _access = WifiAccess.Unknown;
    private bool _accessChecked;

    // Son taramadaki ağ nesneleri (ConnectAsync nesne ister); SSID başına en güçlü erişim noktası.
    private readonly Dictionary<string, WiFiAvailableNetwork> _lastNetworks = new(StringComparer.Ordinal);
    private bool _disposed;

    public async Task<WifiScanResult> ScanAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_disposed)
                return new WifiScanResult(WifiAccess.Unknown, true, true, []);

            if (!await EnsureAdapterAsync())
                return new WifiScanResult(_access, _access is WifiAccess.DeniedByUser or WifiAccess.DeniedBySystem, false, []);

            var adapter = _adapter!;
            await adapter.ScanAsync();

            var connectedSsid = await GetConnectedSsidAsync(adapter);
            var profiles = WlanProfiles.GetNames(adapter.NetworkAdapter.NetworkAdapterId);

            _lastNetworks.Clear();
            var networks = new List<WifiNetwork>();
            foreach (var available in adapter.NetworkReport.AvailableNetworks)
            {
                if (available.IsWiFiDirect)
                    continue;

                var ssid = available.Ssid ?? string.Empty;
                var quality = WifiList.QualityFromRssi(available.NetworkRssiInDecibelMilliwatts);
                var secured = available.SecuritySettings.NetworkAuthenticationType
                    is not (NetworkAuthenticationType.None or NetworkAuthenticationType.Open80211);
                var connected = connectedSsid is not null && ssid.Length > 0 && string.Equals(ssid, connectedSsid, StringComparison.Ordinal);

                networks.Add(new WifiNetwork(ssid, quality, secured, connected, WifiList.HasProfile(profiles, ssid)));

                if (ssid.Length > 0
                    && (!_lastNetworks.TryGetValue(ssid, out var existing) || available.NetworkRssiInDecibelMilliwatts > existing.NetworkRssiInDecibelMilliwatts))
                {
                    _lastNetworks[ssid] = available;
                }
            }

            return new WifiScanResult(WifiAccess.Allowed, true, false, networks);
        }
        catch (Exception ex)
        {
            // Adaptör çıkarıldı ya da yığın yanıt vermiyor: bir sonraki taramada yeniden aranır.
            CrashLog.Write("WifiService.ScanAsync", ex);
            _adapter = null;
            _accessChecked = false;
            return new WifiScanResult(_access, true, true, []);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> EnsureAdapterAsync()
    {
        if (!_accessChecked)
        {
            _access = MapAccess(await WiFiAdapter.RequestAccessAsync());
            _accessChecked = true;
        }

        if (_access != WifiAccess.Allowed)
            return false;

        if (_adapter is not null)
            return true;

        var adapters = await WiFiAdapter.FindAllAdaptersAsync();
        _adapter = adapters.Count > 0 ? adapters[0] : null;
        return _adapter is not null;
    }

    private static async Task<string?> GetConnectedSsidAsync(WiFiAdapter adapter)
    {
        try
        {
            var profile = await adapter.NetworkAdapter.GetConnectedProfileAsync();
            if (profile is { IsWlanConnectionProfile: true })
            {
                var ssid = profile.WlanConnectionProfileDetails.GetConnectedSsid();
                return string.IsNullOrEmpty(ssid) ? null : ssid;
            }
        }
        catch
        {
            // Bağlı ağ okunamazsa hiçbir satır bağlı gösterilmez; uydurma durum yok.
        }

        return null;
    }

    public async Task<WifiConnectStatus> ConnectAsync(string ssid, string? password)
    {
        await _gate.WaitAsync();
        try
        {
            if (_disposed || _adapter is null || !_lastNetworks.TryGetValue(ssid, out var network))
                return WifiConnectStatus.NotAvailable;

            // Şifre yalnız bu çağrıda kimlik bilgisi nesnesine konur; saklanmaz ve günlüğe yazılmaz.
            var result = string.IsNullOrEmpty(password)
                ? await _adapter.ConnectAsync(network, WiFiReconnectionKind.Automatic)
                : await _adapter.ConnectAsync(network, WiFiReconnectionKind.Automatic, new PasswordCredential { Password = password });

            return result.ConnectionStatus switch
            {
                WiFiConnectionStatus.Success => WifiConnectStatus.Connected,
                WiFiConnectionStatus.InvalidCredential => WifiConnectStatus.WrongPassword,
                WiFiConnectionStatus.NetworkNotAvailable => WifiConnectStatus.NotAvailable,
                WiFiConnectionStatus.Timeout => WifiConnectStatus.Timeout,
                WiFiConnectionStatus.UnsupportedAuthenticationProtocol => WifiConnectStatus.UnsupportedSecurity,
                WiFiConnectionStatus.AccessRevoked => WifiConnectStatus.AccessDenied,
                _ => WifiConnectStatus.Failed
            };
        }
        catch (Exception ex)
        {
            // İstisna iletisi şifre içermez; yine de parametreler günlüğe eklenmez.
            CrashLog.Write("WifiService.ConnectAsync", ex);
            return WifiConnectStatus.Failed;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Disconnect()
    {
        try
        {
            _adapter?.Disconnect();
        }
        catch (Exception ex)
        {
            CrashLog.Write("WifiService.Disconnect", ex);
        }
    }

    public void OpenSettings() => SettingsUri.Open("ms-settings:network-wifi");

    public void OpenLocationSettings() => SettingsUri.Open("ms-settings:privacy-location");

    private static WifiAccess MapAccess(WiFiAccessStatus status) => status switch
    {
        WiFiAccessStatus.Allowed => WifiAccess.Allowed,
        WiFiAccessStatus.DeniedByUser => WifiAccess.DeniedByUser,
        WiFiAccessStatus.DeniedBySystem => WifiAccess.DeniedBySystem,
        _ => WifiAccess.Unknown
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _adapter = null;
        _lastNetworks.Clear();
    }
}
