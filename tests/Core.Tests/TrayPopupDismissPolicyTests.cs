using Koy.Core.Tray;

namespace Koy.Core.Tests;

public class TrayPopupDismissPolicyTests
{
    private static TimeSpan Ms(int ms) => TimeSpan.FromMilliseconds(ms);

    [Fact]
    public void PencereDisinaTiklaninca_Kapanir()
    {
        var policy = new TrayPopupDismissPolicy();

        var action = policy.Tick(Ms(0), pointerPressed: true, pointerInsidePopup: false, pointerInsideMenu: false, menuVisible: false);

        Assert.Equal(TrayPopupDismissAction.Close, action);
    }

    [Fact]
    public void PencereIcineTiklaninca_Kapanmaz()
    {
        var policy = new TrayPopupDismissPolicy();

        var action = policy.Tick(Ms(0), true, pointerInsidePopup: true, pointerInsideMenu: false, menuVisible: false);

        Assert.Equal(TrayPopupDismissAction.None, action);
    }

    [Fact]
    public void BasmadanImlecDisardaGezmekPencereyiKapatmaz_OdakKaybiSayilmaz()
    {
        var policy = new TrayPopupDismissPolicy();

        for (var t = 0; t < 2000; t += 50)
            Assert.Equal(TrayPopupDismissAction.None, policy.Tick(Ms(t), false, false, false, false));
    }

    [Fact]
    public void SagTiktanSonraMenuAcikkenPencereAcikKalir()
    {
        var policy = new TrayPopupDismissPolicy();
        policy.OnIconRightClicked(Ms(0));

        Assert.Equal(TrayPopupDismissAction.None, policy.Tick(Ms(50), false, true, false, menuVisible: false));
        Assert.Equal(TrayPopupDismissAction.None, policy.Tick(Ms(100), false, false, true, menuVisible: true));
        Assert.Equal(TrayPopupDismissAction.None, policy.Tick(Ms(2000), false, false, true, menuVisible: true));
    }

    [Fact]
    public void MenudenSecimYapilincaMenuKapanir_PencereDeKapanir()
    {
        var policy = new TrayPopupDismissPolicy();
        policy.OnIconRightClicked(Ms(0));
        policy.Tick(Ms(100), false, false, true, true);

        // Menü öğesine tıklandı (imleç menüde), sonraki adımda menü yok.
        Assert.Equal(TrayPopupDismissAction.None, policy.Tick(Ms(200), pointerPressed: true, pointerInsidePopup: false, pointerInsideMenu: true, menuVisible: true));
        Assert.Equal(TrayPopupDismissAction.Close, policy.Tick(Ms(250), false, false, false, menuVisible: false));
    }

    [Fact]
    public void MenuAcikkenMenuVePencereDisinaTiklaninca_Kapanir()
    {
        var policy = new TrayPopupDismissPolicy();
        policy.OnIconRightClicked(Ms(0));
        policy.Tick(Ms(100), false, false, true, true);

        var action = policy.Tick(Ms(300), pointerPressed: true, pointerInsidePopup: false, pointerInsideMenu: false, menuVisible: true);

        Assert.Equal(TrayPopupDismissAction.Close, action);
    }

    [Fact]
    public void MenuPencereyeTiklanarakKapandiysa_PencereAcikKalir()
    {
        var policy = new TrayPopupDismissPolicy();
        policy.OnIconRightClicked(Ms(0));
        policy.Tick(Ms(100), false, false, true, true);

        // Kullanıcı menü açıkken başka bir tepsi simgesine tıkladı: menü kapanır, pencere kalır.
        policy.Tick(Ms(300), pointerPressed: true, pointerInsidePopup: true, pointerInsideMenu: false, menuVisible: true);
        var action = policy.Tick(Ms(350), false, true, false, menuVisible: false);

        Assert.Equal(TrayPopupDismissAction.None, action);
    }

    [Fact]
    public void MenuHicGelmezse_BeklemeBiterVePencereAcikKalir()
    {
        var policy = new TrayPopupDismissPolicy();
        policy.OnIconRightClicked(Ms(0));

        Assert.Equal(TrayPopupDismissAction.None, policy.Tick(Ms(900), false, true, false, menuVisible: false));

        // Sonradan başka biri menü açıp kapatsa bile bu sağ tıkla ilişkilendirilmez.
        policy.Tick(Ms(1000), false, true, false, menuVisible: true);
        Assert.Equal(TrayPopupDismissAction.None, policy.Tick(Ms(1100), false, true, false, menuVisible: false));
    }

    [Fact]
    public void KlavyeyleMenuKapaninca_PencereKapanir()
    {
        var policy = new TrayPopupDismissPolicy();
        policy.OnIconRightClicked(Ms(0));
        policy.Tick(Ms(100), false, false, true, true);

        Assert.Equal(TrayPopupDismissAction.Close, policy.Tick(Ms(400), false, false, false, menuVisible: false));
    }

    [Fact]
    public void Reset_BeklemeyiSifirlar()
    {
        var policy = new TrayPopupDismissPolicy();
        policy.OnIconRightClicked(Ms(0));
        policy.Tick(Ms(100), false, false, true, true);
        policy.Reset();

        Assert.Equal(TrayPopupDismissAction.None, policy.Tick(Ms(200), false, false, false, menuVisible: false));
    }
}
