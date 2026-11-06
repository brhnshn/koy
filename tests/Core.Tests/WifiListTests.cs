using Koy.Core.Wireless;

namespace Koy.Core.Tests;

public class WifiListTests
{
    private static WifiNetwork Net(string ssid, int quality = 60, bool secured = true, bool connected = false, bool profile = false) =>
        new(ssid, quality, secured, connected, profile);

    private static WifiScanResult Scan(params WifiNetwork[] networks) =>
        new(WifiAccess.Allowed, true, false, networks);

    [Theory]
    [InlineData(-100, 0)]
    [InlineData(-120, 0)]
    [InlineData(-75, 50)]
    [InlineData(-50, 100)]
    [InlineData(-30, 100)]
    public void QualityFromRssi_DogrusalVeSinirli(double dbm, int expected) =>
        Assert.Equal(expected, WifiList.QualityFromRssi(dbm));

    [Fact]
    public void QualityFromRssi_NaN_Sifir() => Assert.Equal(0, WifiList.QualityFromRssi(double.NaN));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(50, 2)]
    [InlineData(51, 3)]
    [InlineData(75, 3)]
    [InlineData(76, 4)]
    [InlineData(100, 4)]
    public void SignalBars_Esikler(int quality, int bars) => Assert.Equal(bars, WifiList.SignalBars(quality));

    [Fact]
    public void Normalize_AyniSsid_TekSatirEnGucluSinyal()
    {
        var list = WifiList.Normalize([Net("Ev", 40), Net("Ev", 80, secured: false), Net("Ofis", 60)]);

        Assert.Equal(2, list.Count);
        var home = list.Single(n => n.Ssid == "Ev");
        Assert.Equal(80, home.SignalQuality);
        Assert.True(home.IsSecured);
    }

    [Fact]
    public void Normalize_BagliEnUstte_SonraSinyalSirasi_GizliEnSonda()
    {
        var list = WifiList.Normalize([Net("", 99), Net("A", 90), Net("B", 30, connected: true), Net("C", 70)]);

        Assert.Equal(["B", "A", "C", ""], list.Select(n => n.Ssid));
    }

    [Fact]
    public void Normalize_GizliAglar_TekSatirdaToplanir()
    {
        var list = WifiList.Normalize([Net("", 20), Net("  ", 50), Net("X", 10)]);

        Assert.Equal(2, list.Count);
        var hidden = list.Single(n => n.IsHidden);
        Assert.Equal(50, hidden.SignalQuality);
    }

    [Fact]
    public void Normalize_BagliVeKayitliBayraklariBirlesir()
    {
        var list = WifiList.Normalize([Net("Ev", 40, profile: true), Net("Ev", 70, connected: true)]);

        var home = Assert.Single(list);
        Assert.True(home.IsConnected);
        Assert.True(home.HasProfile);
    }

    [Fact]
    public void Resolve_SonucYok_Yukleniyor() => Assert.Equal(WifiListState.Loading, WifiList.Resolve(null, true));

    [Fact]
    public void Resolve_AdaptorYok() =>
        Assert.Equal(WifiListState.NoAdapter, WifiList.Resolve(new WifiScanResult(WifiAccess.Allowed, false, false, []), true));

    [Theory]
    [InlineData(WifiAccess.DeniedByUser)]
    [InlineData(WifiAccess.DeniedBySystem)]
    public void Resolve_ErisimReddi(WifiAccess access) =>
        Assert.Equal(WifiListState.NoAccess, WifiList.Resolve(new WifiScanResult(access, true, false, [Net("A")]), true));

    [Fact]
    public void Resolve_RadyoKapali() => Assert.Equal(WifiListState.RadioOff, WifiList.Resolve(Scan(Net("A")), false));

    [Fact]
    public void Resolve_TaramaHatasi() =>
        Assert.Equal(WifiListState.ScanFailed, WifiList.Resolve(new WifiScanResult(WifiAccess.Allowed, true, true, []), true));

    [Fact]
    public void Resolve_BosListe() => Assert.Equal(WifiListState.Empty, WifiList.Resolve(Scan(), true));

    [Fact]
    public void Resolve_HepsiAdsiz_KonumIzniYok() =>
        Assert.Equal(WifiListState.LocationBlocked, WifiList.Resolve(Scan(Net(""), Net(" ")), true));

    [Fact]
    public void Resolve_AdliAgVar_Hazir() =>
        Assert.Equal(WifiListState.Ready, WifiList.Resolve(Scan(Net(""), Net("Ev")), true));

    [Fact]
    public void ActionFor_Bagli_Gizli_Hicbir() =>
        Assert.All([Net("A", connected: true), Net("")], n => Assert.Equal(WifiRowAction.None, WifiList.ActionFor(n)));

    [Fact]
    public void ActionFor_KayitliYaDaAcik_Dogrudan()
    {
        Assert.Equal(WifiRowAction.ConnectDirect, WifiList.ActionFor(Net("A", secured: true, profile: true)));
        Assert.Equal(WifiRowAction.ConnectDirect, WifiList.ActionFor(Net("A", secured: false)));
    }

    [Fact]
    public void ActionFor_GuvenliKayitsiz_SifreSorar() =>
        Assert.Equal(WifiRowAction.AskPassword, WifiList.ActionFor(Net("A", secured: true, profile: false)));

    [Fact]
    public void StatusKey_BagliGuvenliAcikKayitli()
    {
        Assert.Equal("WifiConnectedSecure", WifiList.StatusKey(Net("A", connected: true, secured: true)));
        Assert.Equal("WifiConnectedOpen", WifiList.StatusKey(Net("A", connected: true, secured: false)));
        Assert.Equal("WifiSaved", WifiList.StatusKey(Net("A", profile: true)));
        Assert.Null(WifiList.StatusKey(Net("A")));
    }

    [Fact]
    public void ErrorKey_BasariNull_DigerleriAnahtar()
    {
        Assert.Null(WifiList.ErrorKey(WifiConnectStatus.Connected));
        Assert.Equal("WifiErrPassword", WifiList.ErrorKey(WifiConnectStatus.WrongPassword));
        Assert.Equal("WifiErrFailed", WifiList.ErrorKey(WifiConnectStatus.Failed));
        foreach (var status in Enum.GetValues<WifiConnectStatus>().Where(s => s != WifiConnectStatus.Connected))
            Assert.False(string.IsNullOrEmpty(WifiList.ErrorKey(status)));
    }

    [Fact]
    public void HasProfile_BosSsidKayitliSayilmaz()
    {
        var names = new HashSet<string> { "Ev", "" };
        Assert.True(WifiList.HasProfile(names, "Ev"));
        Assert.False(WifiList.HasProfile(names, ""));
        Assert.False(WifiList.HasProfile(names, "Ofis"));
    }
}
