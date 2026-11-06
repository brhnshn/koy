namespace Koy.Core.Wireless;

/// <summary>Satıra tıklayınca ne olacağı.</summary>
public enum WifiRowAction
{
    None,
    /// <summary>Kayıtlı profille ya da açık ağa doğrudan bağlan.</summary>
    ConnectDirect,
    /// <summary>Güvenli ve kayıtsız: satırın içinde şifre kutusu açılır.</summary>
    AskPassword
}

/// <summary>Wi-Fi listesinin saf mantığı: sinyal çubukları, birleştirme, sıralama, durum ve satır eylemleri.</summary>
public static class WifiList
{
    public const int MaxBars = 4;

    /// <summary>RSSI (dBm) değerinden 0-100 kalite: -100 dBm ve altı 0, -50 dBm ve üstü 100 (doğrusal dönüşüm).</summary>
    public static int QualityFromRssi(double dbm)
    {
        if (double.IsNaN(dbm))
            return 0;

        return (int)Math.Clamp(Math.Round(2 * (dbm + 100)), 0, 100);
    }

    /// <summary>0-100 kaliteden 0-4 çubuk. Ağ görünüyorsa (kalite &gt; 0) en az 1 çubuk çizilir.</summary>
    public static int SignalBars(int quality)
    {
        if (quality <= 0)
            return 0;
        if (quality <= 25)
            return 1;
        if (quality <= 50)
            return 2;
        if (quality <= 75)
            return 3;
        return MaxBars;
    }

    /// <summary>
    /// Ham tarama kayıtlarını (her erişim noktası ayrı) listeye çevirir: aynı SSID tek satır olur (en güçlü sinyal,
    /// güvenli/bağlı/kayıtlı bayrakları birleşir), gizli ağlar tek "Gizli ağ" satırında toplanır.
    /// Sıra: bağlı olan, sonra sinyal gücüne göre, gizli ağ en sonda.
    /// </summary>
    public static List<WifiNetwork> Normalize(IEnumerable<WifiNetwork> raw)
    {
        var bySsid = new Dictionary<string, WifiNetwork>(StringComparer.Ordinal);
        foreach (var network in raw)
        {
            var key = network.IsHidden ? string.Empty : network.Ssid;
            bySsid[key] = bySsid.TryGetValue(key, out var existing) ? Merge(existing, network) : network with { Ssid = key };
        }

        return bySsid.Values
            .OrderBy(n => n.IsHidden)
            .ThenByDescending(n => n.IsConnected)
            .ThenByDescending(n => n.SignalQuality)
            .ThenBy(n => n.Ssid, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static WifiNetwork Merge(WifiNetwork a, WifiNetwork b) => new(
        a.Ssid,
        Math.Max(a.SignalQuality, b.SignalQuality),
        a.IsSecured || b.IsSecured,
        a.IsConnected || b.IsConnected,
        a.HasProfile || b.HasProfile);

    /// <summary>Tarama sonucu ve radyo durumundan listenin hangi hâlde gösterileceği.</summary>
    public static WifiListState Resolve(WifiScanResult? result, bool radioOn)
    {
        if (result is null)
            return WifiListState.Loading;
        if (!result.AdapterPresent)
            return WifiListState.NoAdapter;
        if (result.Access is WifiAccess.DeniedByUser or WifiAccess.DeniedBySystem)
            return WifiListState.NoAccess;
        if (!radioOn)
            return WifiListState.RadioOff;
        if (result.ScanFailed)
            return WifiListState.ScanFailed;

        var networks = Normalize(result.Networks);
        if (networks.Count == 0)
            return WifiListState.Empty;

        // Konum izni yokken Windows ağları adsız döndürür; bağlı ağın adı bile boş görünür.
        return networks.All(n => n.IsHidden) ? WifiListState.LocationBlocked : WifiListState.Ready;
    }

    public static WifiRowAction ActionFor(WifiNetwork network)
    {
        if (network.IsHidden || network.IsConnected)
            return WifiRowAction.None;

        return network.HasProfile || !network.IsSecured ? WifiRowAction.ConnectDirect : WifiRowAction.AskPassword;
    }

    /// <summary>Satırın alt yazısı için kaynak anahtarı (Strings.resx); yazı gerekmiyorsa null.</summary>
    public static string? StatusKey(WifiNetwork network)
    {
        if (network.IsConnected)
            return network.IsSecured ? "WifiConnectedSecure" : "WifiConnectedOpen";

        return network.HasProfile ? "WifiSaved" : null;
    }

    /// <summary>Bağlanma sonucu için hata metni anahtarı; başarıda null.</summary>
    public static string? ErrorKey(WifiConnectStatus status) => status switch
    {
        WifiConnectStatus.Connected => null,
        WifiConnectStatus.WrongPassword => "WifiErrPassword",
        WifiConnectStatus.NotAvailable => "WifiErrNotAvailable",
        WifiConnectStatus.Timeout => "WifiErrTimeout",
        WifiConnectStatus.UnsupportedSecurity => "WifiErrUnsupported",
        WifiConnectStatus.AccessDenied => "WifiErrAccess",
        _ => "WifiErrFailed"
    };

    /// <summary>Kayıtlı profil adları arasında bu ağ var mı (profil adı çoğu zaman SSID'dir).</summary>
    public static bool HasProfile(ISet<string> profileNames, string ssid) =>
        !string.IsNullOrEmpty(ssid) && profileNames.Contains(ssid);
}
