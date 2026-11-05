using System.Text;
using Koy.Core.Dock;

namespace Koy.Core.Tests;

public class PinnedOrderTests
{
    private static PinnedApp App(string file) =>
        new(Path.GetFileNameWithoutExtension(file), @"C:\Pinned\" + file, null, null);

    private static string[] Ids(IEnumerable<PinnedApp> apps) => apps.Select(a => PinnedOrder.IdOf(a.ShortcutPath)).ToArray();

    [Fact]
    public void IdOf_YoldanBagimsizDosyaAdi()
    {
        Assert.Equal("Obsidian.lnk", PinnedOrder.IdOf(@"C:\Users\x\AppData\Obsidian.lnk"));
    }

    [Fact]
    public void Apply_KayitliSiraYenileriSonaEkler()
    {
        var apps = new[] { App("A.lnk"), App("B.lnk"), App("C.lnk"), App("D.lnk") };

        var result = PinnedOrder.Apply(apps, ["C.lnk", "A.lnk"]);

        Assert.Equal(["C.lnk", "A.lnk", "B.lnk", "D.lnk"], Ids(result));
    }

    [Fact]
    public void Apply_KayitBossaKlasorSirasiniKorur()
    {
        var apps = new[] { App("B.lnk"), App("A.lnk") };

        Assert.Equal(["B.lnk", "A.lnk"], Ids(PinnedOrder.Apply(apps, [])));
    }

    [Fact]
    public void Apply_BuyukKucukHarfeDuyarsiz()
    {
        var apps = new[] { App("A.lnk"), App("b.LNK") };

        Assert.Equal(["b.LNK", "A.lnk"], Ids(PinnedOrder.Apply(apps, ["B.lnk"])));
    }

    [Fact]
    public void Reconcile_KaldirilaniDusurupYeniyiSonaEkler()
    {
        var result = PinnedOrder.Reconcile(["C.lnk", "X.lnk", "A.lnk"], ["A.lnk", "B.lnk", "C.lnk"]);

        Assert.Equal(["C.lnk", "A.lnk", "B.lnk"], result);
    }

    [Fact]
    public void Reconcile_MevcutListeBossaKayitiKorur()
    {
        var result = PinnedOrder.Reconcile(["C.lnk", "A.lnk"], []);

        Assert.Equal(["C.lnk", "A.lnk"], result);
    }

    [Fact]
    public void Reconcile_TekrarlariTekeIndirir()
    {
        Assert.Equal(["A.lnk", "B.lnk"], PinnedOrder.Reconcile(["A.lnk", "a.lnk"], ["A.lnk", "B.lnk"]));
    }

    [Theory]
    [InlineData("A.lnk", 2, new[] { "B.lnk", "C.lnk", "A.lnk" })]
    [InlineData("C.lnk", 0, new[] { "C.lnk", "A.lnk", "B.lnk" })]
    [InlineData("B.lnk", 99, new[] { "A.lnk", "C.lnk", "B.lnk" })]
    [InlineData("B.lnk", -5, new[] { "B.lnk", "A.lnk", "C.lnk" })]
    [InlineData("YOK.lnk", 1, new[] { "A.lnk", "B.lnk", "C.lnk" })]
    public void Move_ogeyiYeniDizineTasir(string id, int index, string[] expected)
    {
        Assert.Equal(expected, PinnedOrder.Move(["A.lnk", "B.lnk", "C.lnk"], id, index));
    }

    // Gerçek Taskband değeri gibi: ad öncesinde uzunluk baytı gibi ikili artıklar, aralarda başka dizgiler.
    private static byte[] Favorites(int leadingBytes, params string[] entries)
    {
        var bytes = new List<byte>(new byte[leadingBytes]);
        foreach (var entry in entries)
        {
            bytes.AddRange(Encoding.Unicode.GetBytes("U" + entry));
            bytes.AddRange(Encoding.Unicode.GetBytes("\0\0Microsoft.Something\0UserPinned\0"));
            bytes.AddRange([0x00, 0x12, 0xBE, 0xEF]);
        }

        return [.. bytes];
    }

    [Fact]
    public void SeedFromTaskband_DegerdekiSiraninKullanir()
    {
        var data = Favorites(8, "Obsidian.lnk", "Visual Studio Code.lnk", "Antigravity.lnk");

        var seed = PinnedOrder.SeedFromTaskband(data, ["Antigravity.lnk", "Docker Desktop.lnk", "Obsidian.lnk", "Visual Studio Code.lnk"]);

        // Bulunanlar değerdeki sırayla, bulunamayan (Docker) klasör sırasıyla arkadan.
        Assert.Equal(["Obsidian.lnk", "Visual Studio Code.lnk", "Antigravity.lnk", "Docker Desktop.lnk"], seed);
    }

    [Fact]
    public void SeedFromTaskband_TekHizalamadaDaCalisir()
    {
        var data = Favorites(9, "B.lnk", "A.lnk");

        Assert.Equal(["B.lnk", "A.lnk"], PinnedOrder.SeedFromTaskband(data, ["A.lnk", "B.lnk"]));
    }

    [Fact]
    public void SeedFromTaskband_KisaAdUzunAdinIcindeYanlisEslesmez()
    {
        var data = Favorites(8, "Visual Studio Code.lnk", "Code.lnk");

        var seed = PinnedOrder.SeedFromTaskband(data, ["Code.lnk", "Visual Studio Code.lnk"]);

        Assert.Equal(["Visual Studio Code.lnk", "Code.lnk"], seed);
    }

    [Fact]
    public void SeedFromTaskband_HicEslesmezseKlasorSirasi()
    {
        var data = Favorites(8, "Baska.lnk");

        Assert.Equal(["B.lnk", "A.lnk"], PinnedOrder.SeedFromTaskband(data, ["B.lnk", "A.lnk"]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 1, 2, 3 })]
    public void SeedFromTaskband_BosVeBozukDegerdeKlasorSirasi(byte[]? data)
    {
        Assert.Equal(["B.lnk", "A.lnk"], PinnedOrder.SeedFromTaskband(data, ["B.lnk", "A.lnk"]));
    }

    [Fact]
    public void SeedFromTaskband_RastgeleBaytlardaCokmez()
    {
        var rng = new Random(42);
        for (var i = 0; i < 50; i++)
        {
            var data = new byte[rng.Next(0, 4000)];
            rng.NextBytes(data);

            var seed = PinnedOrder.SeedFromTaskband(data, ["A.lnk", "B.lnk"]);

            Assert.Equal(2, seed.Count);
        }
    }

    private sealed class FakeInner(params PinnedApp[] apps) : IPinnedAppsService
    {
        public List<PinnedApp> Apps { get; } = [.. apps];

        public IReadOnlyList<PinnedApp> GetPinnedApps() => Apps;
    }

    [Fact]
    public void Service_IlkAcilistaTohumuYazarSonraKayitEsastir()
    {
        var inner = new FakeInner(App("A.lnk"), App("B.lnk"), App("C.lnk"));
        List<string> stored = [];
        var saves = 0;
        var service = new OrderedPinnedAppsService(inner, () => stored, o => { stored = [.. o]; saves++; },
            () => Favorites(8, "C.lnk", "A.lnk", "B.lnk"));

        Assert.Equal(["C.lnk", "A.lnk", "B.lnk"], Ids(service.GetPinnedApps()));
        Assert.Equal(["C.lnk", "A.lnk", "B.lnk"], stored);

        // Windows tarafı bir daha okunmaz: kayıt esas. Aynı sırada tekrar kaydedilmez.
        Assert.Equal(["C.lnk", "A.lnk", "B.lnk"], Ids(service.GetPinnedApps()));
        Assert.Equal(1, saves);
    }

    [Fact]
    public void Service_KayitOkumaHatasindaKlasorSirasinaDuser()
    {
        var inner = new FakeInner(App("B.lnk"), App("A.lnk"));
        List<string> stored = [];
        var service = new OrderedPinnedAppsService(inner, () => stored, o => stored = [.. o],
            () => throw new InvalidOperationException("kayıt defteri okunamadı"));

        Assert.Equal(["B.lnk", "A.lnk"], Ids(service.GetPinnedApps()));
    }

    [Fact]
    public void Service_YeniSabitlenenSonaEklenirKaldirilanDuser()
    {
        var inner = new FakeInner(App("A.lnk"), App("B.lnk"));
        List<string> stored = ["B.lnk", "A.lnk"];
        var service = new OrderedPinnedAppsService(inner, () => stored, o => stored = [.. o]);

        inner.Apps.Add(App("N.lnk"));
        Assert.Equal(["B.lnk", "A.lnk", "N.lnk"], Ids(service.GetPinnedApps()));

        inner.Apps.RemoveAll(a => a.Title == "B");
        Assert.Equal(["A.lnk", "N.lnk"], Ids(service.GetPinnedApps()));
        Assert.Equal(["A.lnk", "N.lnk"], stored);
    }

    [Fact]
    public void Service_KlasorBosOkunursaKayitSilinmez()
    {
        var inner = new FakeInner(App("A.lnk"), App("B.lnk"));
        List<string> stored = ["B.lnk", "A.lnk"];
        var service = new OrderedPinnedAppsService(inner, () => stored, o => stored = [.. o]);

        inner.Apps.Clear();
        Assert.Empty(service.GetPinnedApps());
        Assert.Equal(["B.lnk", "A.lnk"], stored);
    }

    [Fact]
    public void Service_MoveToSirayiDegistirirVeYazar()
    {
        var inner = new FakeInner(App("A.lnk"), App("B.lnk"), App("C.lnk"));
        List<string> stored = ["A.lnk", "B.lnk", "C.lnk"];
        var service = new OrderedPinnedAppsService(inner, () => stored, o => stored = [.. o]);
        service.GetPinnedApps();

        service.MoveTo("A.lnk", 2);

        Assert.Equal(["B.lnk", "C.lnk", "A.lnk"], stored);
        Assert.Equal(["B.lnk", "C.lnk", "A.lnk"], Ids(service.GetPinnedApps()));
    }

    [Fact]
    public void Service_YazmaHatasiOturumdaSiraKorunur()
    {
        var inner = new FakeInner(App("A.lnk"), App("B.lnk"));
        var service = new OrderedPinnedAppsService(inner, () => ["A.lnk", "B.lnk"], _ => throw new IOException("disk dolu"));

        service.MoveTo("A.lnk", 1);

        Assert.Equal(["B.lnk", "A.lnk"], Ids(service.GetPinnedApps()));
    }
}

public class DockReorderMathTests
{
    [Theory]
    [InlineData(5, 2, 0.0, 40.0, 2)]
    [InlineData(5, 2, 19.0, 40.0, 2)]
    [InlineData(5, 2, 20.0, 40.0, 3)]
    [InlineData(5, 2, 85.0, 40.0, 4)]
    [InlineData(5, 2, 500.0, 40.0, 4)]
    [InlineData(5, 2, -45.0, 40.0, 1)]
    [InlineData(5, 2, -500.0, 40.0, 0)]
    [InlineData(0, 0, 10.0, 40.0, 0)]
    public void TargetIndex_YuvayaYuvarlarVeKirpar(int count, int original, double offset, double slot, int expected)
    {
        Assert.Equal(expected, DockReorderMath.TargetIndex(count, original, offset, slot));
    }

    [Fact]
    public void ShiftFor_SagaSurukleyinceAradakilerSolaKayar()
    {
        // Sürüklenen 1 → hedef 3: 2 ve 3 sola.
        Assert.Equal(0, DockReorderMath.ShiftFor(0, 1, 3, 40));
        Assert.Equal(0, DockReorderMath.ShiftFor(1, 1, 3, 40));
        Assert.Equal(-40, DockReorderMath.ShiftFor(2, 1, 3, 40));
        Assert.Equal(-40, DockReorderMath.ShiftFor(3, 1, 3, 40));
        Assert.Equal(0, DockReorderMath.ShiftFor(4, 1, 3, 40));
    }

    [Fact]
    public void ShiftFor_SolaSurukleyinceAradakilerSagaKayar()
    {
        // Sürüklenen 3 → hedef 1: 1 ve 2 sağa.
        Assert.Equal(0, DockReorderMath.ShiftFor(0, 3, 1, 40));
        Assert.Equal(40, DockReorderMath.ShiftFor(1, 3, 1, 40));
        Assert.Equal(40, DockReorderMath.ShiftFor(2, 3, 1, 40));
        Assert.Equal(0, DockReorderMath.ShiftFor(3, 3, 1, 40));
        Assert.Equal(0, DockReorderMath.ShiftFor(4, 3, 1, 40));
    }

    [Fact]
    public void ShiftFor_HedefAyniysaKaymaYok()
    {
        for (var i = 0; i < 4; i++)
            Assert.Equal(0, DockReorderMath.ShiftFor(i, 2, 2, 40));
    }

    [Fact]
    public void ExceedsThreshold_TiklamaIleSurukleyiAyirir()
    {
        Assert.False(DockReorderMath.ExceedsThreshold(2, -3, 4));
        Assert.True(DockReorderMath.ExceedsThreshold(-4, 0, 4));
        Assert.True(DockReorderMath.ExceedsThreshold(0, 5, 4));
    }
}
