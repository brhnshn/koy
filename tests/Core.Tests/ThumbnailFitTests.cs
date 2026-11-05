using Koy.Core.Dock;
using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class ThumbnailFitTests
{
    [Fact]
    public void Fit_LandscapeSourceInPortraitBox_FillsWidthAndCentersVertically()
    {
        var box = new WindowRect(0, 0, 100, 200);

        var result = ThumbnailFit.Fit(1600, 900, box);

        Assert.Equal(new WindowRect(0, 72, 100, 128), result);
    }

    [Fact]
    public void Fit_PortraitSourceInLandscapeBox_FillsHeightAndCentersHorizontally()
    {
        var box = new WindowRect(10, 20, 474, 284); // 464 × 264

        var result = ThumbnailFit.Fit(600, 1200, box);

        Assert.Equal(264, result.Height);
        Assert.Equal(132, result.Width);
        Assert.Equal(10 + (464 - 132) / 2, result.Left);
        Assert.Equal(20, result.Top);
    }

    [Fact]
    public void Fit_SameAspectRatio_FillsBoxExactly()
    {
        var box = new WindowRect(16, 40, 480, 301); // 464 × 261

        var result = ThumbnailFit.Fit(1920, 1080, box);

        Assert.Equal(box.Width, result.Width);
        Assert.InRange(result.Height, 260, 261);
        Assert.Equal(16, result.Left);
    }

    [Fact]
    public void Fit_SmallSource_IsNotUpscaled()
    {
        var box = new WindowRect(0, 0, 400, 300);

        var result = ThumbnailFit.Fit(200, 100, box);

        Assert.Equal(new WindowRect(100, 100, 300, 200), result);
    }

    [Theory]
    [InlineData(0, 900)]
    [InlineData(1600, 0)]
    [InlineData(-5, -5)]
    public void Fit_UnknownSourceSize_ReturnsBox(int width, int height)
    {
        var box = new WindowRect(5, 5, 105, 65);

        Assert.Equal(box, ThumbnailFit.Fit(width, height, box));
    }

    [Fact]
    public void Fit_EmptyBox_ReturnsEmpty()
    {
        Assert.Equal(WindowRect.Empty, ThumbnailFit.Fit(1920, 1080, new WindowRect(10, 10, 10, 50)));
    }

    [Fact]
    public void Fit_ResultAlwaysInsideBox()
    {
        var box = new WindowRect(3, 7, 143, 77);

        foreach (var (w, h) in new[] { (1, 1), (3840, 160), (160, 3840), (1366, 768), (141, 71) })
        {
            var r = ThumbnailFit.Fit(w, h, box);
            Assert.True(r.Left >= box.Left && r.Top >= box.Top && r.Right <= box.Right && r.Bottom <= box.Bottom,
                $"{w}x{h} -> {r}");
            Assert.False(r.IsEmpty);
        }
    }
}
