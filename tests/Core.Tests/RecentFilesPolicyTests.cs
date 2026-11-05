using Koy.Core.Dock;

namespace Koy.Core.Tests;

public class RecentFilesPolicyTests
{
    private static RecentEntry E(string file, int minutesAgo) =>
        new(@"C:\Recent\" + file, new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc).AddMinutes(-minutesAgo));

    [Fact]
    public void Select_YenidenEskiyeSiralarVeUzantiyiAtar()
    {
        var result = RecentFilesPolicy.Select([E("eski.docx.lnk", 60), E("yeni.pdf.lnk", 1), E("orta.txt.lnk", 10)]);

        Assert.Equal(["yeni.pdf", "orta.txt", "eski.docx"], result.Select(r => r.Name));
        Assert.Equal(@"C:\Recent\yeni.pdf.lnk", result[0].OpenPath);
    }

    [Fact]
    public void Select_EnCokOnOgeVerir()
    {
        var entries = Enumerable.Range(0, 25).Select(i => E($"dosya{i}.txt.lnk", i)).ToList();

        Assert.Equal(10, RecentFilesPolicy.Select(entries).Count);
        Assert.Equal(10, RecentFilesPolicy.Select(entries, 99).Count);
        Assert.Equal(3, RecentFilesPolicy.Select(entries, 3).Count);
        Assert.Empty(RecentFilesPolicy.Select(entries, 0));
        Assert.Empty(RecentFilesPolicy.Select(entries, -4));
    }

    [Fact]
    public void Select_KisayolOlmayanlariVeBosAdlariAtar()
    {
        var result = RecentFilesPolicy.Select([E("desktop.ini", 1), E("not.txt", 2), E(".lnk", 3), E("gercek.txt.lnk", 4), new RecentEntry("", DateTime.UtcNow)]);

        Assert.Equal(["gercek.txt"], result.Select(r => r.Name));
    }

    [Fact]
    public void Select_AyniAdlilardanEnYeniyiTutar()
    {
        var older = new RecentEntry(@"C:\Recent\a\rapor.pdf.lnk", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = new RecentEntry(@"C:\Recent\b\RAPOR.pdf.lnk", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = RecentFilesPolicy.Select([older, newer]);

        Assert.Single(result);
        Assert.Equal(newer.ShortcutPath, result[0].OpenPath);
    }

    [Fact]
    public void Select_BosGirdideBosListe()
    {
        Assert.Empty(RecentFilesPolicy.Select([]));
    }
}
