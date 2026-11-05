using Koy.Core.Display;
using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class DisplaySchemaTests
{
    private static DisplayMonitor Mon(string id, int l, int t, int r, int b, bool primary = false) =>
        new(id, id, new WindowRect(l, t, r, b), new WindowRect(l, t, r, b), primary);

    [Fact]
    public void Empty_or_zero_area_gives_empty_layout()
    {
        Assert.Same(SchemaLayout.Empty, DisplaySchema.Compute([], 500, 120));
        Assert.Same(SchemaLayout.Empty, DisplaySchema.Compute([Mon("a", 0, 0, 100, 100)], 0, 120));
    }

    [Fact]
    public void Two_side_by_side_monitors_keep_proportions_and_fit()
    {
        var monitors = new[] { Mon("a", 0, 0, 1920, 1080, true), Mon("b", 1920, 0, 3840, 1080) };

        var layout = DisplaySchema.Compute(monitors, 400, 200, gap: 0);

        Assert.Equal(400, layout.Width, 6);
        Assert.Equal(112.5, layout.Height, 6);
        Assert.Equal(2, layout.Tiles.Count);
        Assert.Equal(0, layout.Tiles[0].Left, 6);
        Assert.Equal(200, layout.Tiles[1].Left, 6);
        Assert.Equal(200, layout.Tiles[0].Width, 6);
        Assert.Equal(1920.0 / 1080.0, layout.Tiles[0].Width / layout.Tiles[0].Height, 6);
    }

    [Fact]
    public void Layout_is_never_scaled_up()
    {
        var layout = DisplaySchema.Compute([Mon("a", 0, 0, 100, 50, true)], 5000, 5000, gap: 0);

        Assert.Equal(100, layout.Width, 6);
        Assert.Equal(50, layout.Height, 6);
    }

    [Fact]
    public void Negative_coordinates_are_normalized_to_the_top_left()
    {
        var monitors = new[] { Mon("main", 0, 0, 1920, 1080, true), Mon("left", -1920, -300, 0, 780) };

        var layout = DisplaySchema.Compute(monitors, 384, 216, gap: 0);

        var left = layout.Tiles.Single(t => t.Id == "left");
        var main = layout.Tiles.Single(t => t.Id == "main");
        Assert.Equal(0, left.Left, 6);
        Assert.Equal(0, left.Top, 6);
        Assert.Equal(192, main.Left, 6);
        Assert.Equal(30, main.Top, 6);
        Assert.All(layout.Tiles, t => Assert.InRange(t.Left + t.Width, 0, layout.Width + 1e-6));
    }

    [Fact]
    public void Stacked_monitors_are_scaled_by_height()
    {
        var monitors = new[] { Mon("a", 0, 0, 1000, 1000, true), Mon("b", 0, 1000, 1000, 2000) };

        var layout = DisplaySchema.Compute(monitors, 500, 100, gap: 0);

        Assert.Equal(100, layout.Height, 6);
        Assert.Equal(50, layout.Width, 6);
    }

    [Fact]
    public void Gap_shrinks_tiles_so_adjacent_monitors_do_not_touch()
    {
        var monitors = new[] { Mon("a", 0, 0, 1000, 500, true), Mon("b", 1000, 0, 2000, 500) };

        var layout = DisplaySchema.Compute(monitors, 200, 200, gap: 8);

        Assert.True(layout.Tiles[1].Left - (layout.Tiles[0].Left + layout.Tiles[0].Width) >= 7.99);
    }

    [Fact]
    public void Numbering_puts_primary_first_then_left_to_right_then_top_to_bottom()
    {
        var monitors = new[]
        {
            Mon("sag", 1920, 0, 3840, 1080),
            Mon("ana", 0, 0, 1920, 1080, true),
            Mon("sol-alt", -1920, 500, 0, 1580),
            Mon("sol-ust", -1920, 0, 0, 500)
        };

        var ids = DisplaySchema.Order(monitors).Select(m => m.Id).ToArray();

        Assert.Equal(["ana", "sol-ust", "sol-alt", "sag"], ids);
        Assert.Equal(1, DisplaySchema.NumberOf(monitors, "ana"));
        Assert.Equal(4, DisplaySchema.NumberOf(monitors, "SAG"));
        Assert.Equal(0, DisplaySchema.NumberOf(monitors, "yok"));
    }
}
