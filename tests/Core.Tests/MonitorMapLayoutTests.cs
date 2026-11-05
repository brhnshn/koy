using Koy.Core.Display;
using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class MonitorMapLayoutTests
{
    private const double MaxWidth = 520;
    private const double MaxHeight = 132;

    private static DisplayMonitor Monitor(string id, int left, int top, int width, int height, bool primary = false, double dpi = 1.0) =>
        new(id, id, new WindowRect(left, top, left + width, top + height), new WindowRect(left, top, left + width, top + height), primary, dpi, dpi);

    [Fact]
    public void Compute_NoMonitors_ReturnsEmpty()
    {
        var result = MonitorMapLayout.Compute([], MaxWidth, MaxHeight, "Primary", true, true);

        Assert.Empty(result.Tiles);
        Assert.Equal(0, result.Width);
        Assert.Equal(0, result.Height);
    }

    [Fact]
    public void Compute_SingleMonitor_ReturnsOneTileWithRealResolution()
    {
        var result = MonitorMapLayout.Compute(
            [Monitor(@"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true)],
            MaxWidth, MaxHeight, "Primary", true, true);

        var tile = Assert.Single(result.Tiles);
        Assert.Equal(1, tile.Number);
        Assert.True(tile.IsPrimary);
        Assert.Equal(1920, tile.PixelWidth);
        Assert.Equal(1080, tile.PixelHeight);
        Assert.True(tile.HasNotch);
        Assert.True(tile.HasDock);

        // Yükseklik sınırı belirleyici: 1080 px → 132 DIP; oran korunur.
        Assert.Equal(MaxHeight, result.Height, 3);
        Assert.Equal(1920.0 * MaxHeight / 1080.0, result.Width, 3);
    }

    [Fact]
    public void Compute_SideBySide_KeepsArrangementAndLeavesGap()
    {
        var primary = Monitor(@"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true);
        var side = Monitor(@"\\.\DISPLAY2", 1920, 0, 2560, 1440, dpi: 1.25);

        var result = MonitorMapLayout.Compute([side, primary], MaxWidth, MaxHeight, "Primary", true, true, gap: 12);

        Assert.Equal(2, result.Tiles.Count);
        var p = result.Tiles[0];
        var s = result.Tiles[1];
        Assert.Equal(@"\\.\DISPLAY1", p.Id);
        Assert.Equal(1, p.Number);
        Assert.Equal(2, s.Number);

        Assert.True(s.Left > p.Left + p.Width, "yan ekran ana ekranın sağında olmalı");
        Assert.Equal(12, s.Left - (p.Left + p.Width), 3);
        Assert.True(s.Height > p.Height, "büyük ekran haritada da büyük görünmeli");
    }

    [Fact]
    public void Compute_MonitorLeftOfPrimary_PrimaryStaysNumberOne()
    {
        var primary = Monitor(@"\\.\DISPLAY2", 0, 0, 1920, 1080, primary: true);
        var left = Monitor(@"\\.\DISPLAY1", -1920, 0, 1920, 1080);

        var result = MonitorMapLayout.Compute([left, primary], MaxWidth, MaxHeight, "Primary", true, true);

        var p = result.Tiles.Single(t => t.IsPrimary);
        var l = result.Tiles.Single(t => !t.IsPrimary);
        Assert.Equal(1, p.Number);
        Assert.Equal(2, l.Number);
        Assert.True(l.Left < p.Left);
        Assert.True(l.Left >= 0, "negatif koordinatlı ekran haritanın dışına taşmamalı");
    }

    [Fact]
    public void Compute_StackedMonitors_PlacesUpperAbove()
    {
        var primary = Monitor(@"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true);
        var top = Monitor(@"\\.\DISPLAY3", 0, -1080, 1920, 1080);

        var result = MonitorMapLayout.Compute([primary, top], MaxWidth, MaxHeight, "Primary", true, true);

        var p = result.Tiles.Single(t => t.IsPrimary);
        var t = result.Tiles.Single(x => !x.IsPrimary);
        Assert.True(t.Top + t.Height < p.Top);
    }

    [Fact]
    public void Compute_ManyMonitors_FitInsideBounds()
    {
        var monitors = new[]
        {
            Monitor("A", 0, 0, 1920, 1080, primary: true),
            Monitor("B", 1920, 0, 1920, 1080),
            Monitor("C", 3840, 0, 1920, 1080),
            Monitor("D", -1080, -400, 1080, 1920)
        };

        var result = MonitorMapLayout.Compute(monitors, MaxWidth, MaxHeight, "All", true, true);

        Assert.Equal(4, result.Tiles.Count);
        Assert.True(result.Width <= MaxWidth + 0.001);
        Assert.True(result.Height <= MaxHeight + 0.001);
        Assert.All(result.Tiles, tile =>
        {
            Assert.True(tile.Left >= 0 && tile.Top >= 0);
            Assert.True(tile.Left + tile.Width <= result.Width + 0.001);
            Assert.True(tile.Top + tile.Height <= result.Height + 0.001);
            Assert.True(tile.Width > 0 && tile.Height > 0);
        });

        // Ana ekrandan sonra soldan sağa: D (en solda), B, C.
        Assert.Equal(["A", "D", "B", "C"], result.Tiles.Select(t => t.Id));
    }

    [Theory]
    [InlineData("Primary", true, false)]
    [InlineData("All", true, true)]
    [InlineData(@"\\.\DISPLAY2", false, true)]
    public void Compute_MarksNotchAndDockByMonitorTarget(string target, bool primaryHas, bool sideHas)
    {
        var primary = Monitor(@"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true);
        var side = Monitor(@"\\.\DISPLAY2", 1920, 0, 1920, 1080);

        var result = MonitorMapLayout.Compute([primary, side], MaxWidth, MaxHeight, target, true, true);

        Assert.Equal(primaryHas, result.Tiles[0].HasNotch);
        Assert.Equal(primaryHas, result.Tiles[0].HasDock);
        Assert.Equal(sideHas, result.Tiles[1].HasNotch);
        Assert.Equal(sideHas, result.Tiles[1].HasDock);
    }

    [Fact]
    public void Compute_DisabledFeatures_AreNotMarked()
    {
        var result = MonitorMapLayout.Compute(
            [Monitor("A", 0, 0, 1920, 1080, primary: true)],
            MaxWidth, MaxHeight, "Primary", notchEnabled: false, dockEnabled: true);

        var tile = Assert.Single(result.Tiles);
        Assert.False(tile.HasNotch);
        Assert.True(tile.HasDock);
    }
}
