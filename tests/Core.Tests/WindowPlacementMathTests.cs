using Koy.Core.Display;
using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class WindowPlacementMathTests
{
    // Ana ekran %100, sağdaki ikinci ekran %150 ve farklı çözünürlükte; ikincisi sanal masaüstünde x=1920'den başlar.
    private static readonly DisplayMonitor Main = new("a", "Ana", new WindowRect(0, 0, 1920, 1080), new WindowRect(0, 0, 1920, 1040), true, 1.0, 1.0);
    private static readonly DisplayMonitor Side = new("b", "Yan", new WindowRect(1920, -200, 5760, 1960), new WindowRect(1920, -200, 5760, 1920), false, 1.5, 1.5);

    [Fact]
    public void Notch_is_centered_on_its_own_monitor_with_its_own_scale()
    {
        var rect = WindowPlacementMath.TopCenter(Side, 500, 260);

        Assert.Equal(750, rect.Width);   // 500 DIP * 1.5
        Assert.Equal(390, rect.Height);  // 260 DIP * 1.5
        Assert.Equal(1920 + (3840 - 750) / 2, rect.Left);
        Assert.Equal(-200, rect.Top);
    }

    [Fact]
    public void Notch_on_main_monitor_uses_scale_one()
    {
        var rect = WindowPlacementMath.TopCenter(Main, 500, 260);

        Assert.Equal(new WindowRect(710, 0, 1210, 260), rect);
    }

    [Fact]
    public void Capsule_dock_sits_at_bottom_center_with_scaled_margin()
    {
        var rect = WindowPlacementMath.BottomCenter(Side, 600, 60, 10);

        Assert.Equal(1960 - 15, rect.Bottom);
        Assert.Equal(90, rect.Height);
        Assert.Equal(900, rect.Width);
        Assert.Equal(1920 + (3840 - 900) / 2, rect.Left);
    }

    [Fact]
    public void Capsule_dock_never_exceeds_the_monitor_width()
    {
        var rect = WindowPlacementMath.BottomCenter(Main, 5000, 60, 0);

        Assert.Equal(1920, rect.Width);
        Assert.Equal(0, rect.Left);
    }

    [Fact]
    public void Full_width_dock_spans_the_monitor_and_hugs_the_bottom()
    {
        var rect = WindowPlacementMath.BottomFullWidth(Side, 44);

        Assert.Equal(1920, rect.Left);
        Assert.Equal(5760, rect.Right);
        Assert.Equal(1960, rect.Bottom);
        Assert.Equal(66, rect.Height);
    }
}
