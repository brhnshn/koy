namespace Koy.Core.Wireless;

/// <summary>Wi-Fi taramasına erişim durumu (Windows 11 24H2'den beri SSID görmek konum iznine bağlı).</summary>
public enum WifiAccess
{
    Unknown,
    Allowed,
    DeniedByUser,
    DeniedBySystem
}

/// <summary>Tarama sonucunda listede ne gösterileceği.</summary>
public enum WifiListState
{
    Loading,
    Ready,
    /// <summary>Wi-Fi radyosu kapalı.</summary>
    RadioOff,
    NoAdapter,
    /// <summary>Wi-Fi erişimi reddedildi.</summary>
    NoAccess,
    /// <summary>Ağlar görünüyor ama adları gizli: masaüstü uygulamalarına konum izni verilmemiş.</summary>
    LocationBlocked,
    /// <summary>Tarama tamamlandı, çevrede ağ yok.</summary>
    Empty,
    /// <summary>Tarama hata verdi.</summary>
    ScanFailed
}

/// <summary>
/// Listedeki tek ağ. <see cref="Ssid"/> boşsa gizli ağdır. <see cref="SignalQuality"/> 0-100.
/// </summary>
public sealed record WifiNetwork(string Ssid, int SignalQuality, bool IsSecured, bool IsConnected, bool HasProfile)
{
    public bool IsHidden => string.IsNullOrWhiteSpace(Ssid);
}

/// <summary>Bir taramanın ham sonucu; durum <see cref="WifiList.Resolve"/> ile belirlenir.</summary>
public sealed record WifiScanResult(
    WifiAccess Access,
    bool AdapterPresent,
    bool ScanFailed,
    IReadOnlyList<WifiNetwork> Networks);

public enum WifiConnectStatus
{
    Connected,
    WrongPassword,
    NotAvailable,
    Timeout,
    UnsupportedSecurity,
    AccessDenied,
    Failed
}
