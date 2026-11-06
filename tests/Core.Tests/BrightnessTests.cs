using Koy.Core.Display;
using Xunit;

namespace Koy.Core.Tests;

public class BrightnessTests
{
    [Theory]
    [InlineData(BrightnessTarget.Internal, true, true, BrightnessSourceKind.Wmi)]
    [InlineData(BrightnessTarget.Internal, true, false, BrightnessSourceKind.Wmi)]
    [InlineData(BrightnessTarget.Internal, false, true, BrightnessSourceKind.Ddc)]
    [InlineData(BrightnessTarget.Internal, false, false, BrightnessSourceKind.None)]
    // Harici ekranda WMI kullanılmaz: dizüstü panelini değiştirirdi.
    [InlineData(BrightnessTarget.External, true, true, BrightnessSourceKind.Ddc)]
    [InlineData(BrightnessTarget.External, true, false, BrightnessSourceKind.None)]
    [InlineData(BrightnessTarget.External, false, false, BrightnessSourceKind.None)]
    [InlineData(BrightnessTarget.Unknown, true, true, BrightnessSourceKind.Wmi)]
    [InlineData(BrightnessTarget.Unknown, false, true, BrightnessSourceKind.Ddc)]
    [InlineData(BrightnessTarget.Unknown, false, false, BrightnessSourceKind.None)]
    public void Pick_chooses_source_for_target(BrightnessTarget target, bool wmi, bool ddc, BrightnessSourceKind expected) =>
        Assert.Equal(expected, BrightnessSourcePicker.Pick(target, wmi, ddc));

    [Fact]
    public void Pick_is_reevaluated_when_layout_changes()
    {
        // İkinci ekran kapanınca: önce harici (DDC), sonra dahili panel (WMI).
        Assert.Equal(BrightnessSourceKind.Ddc, BrightnessSourcePicker.Pick(BrightnessTarget.External, true, true));
        Assert.Equal(BrightnessSourceKind.Wmi, BrightnessSourcePicker.Pick(BrightnessTarget.Internal, true, false));
    }

    [Theory]
    [InlineData(0, 0, 100, 0)]
    [InlineData(50, 0, 100, 50)]
    [InlineData(35, 10, 60, 50)]
    [InlineData(200, 0, 100, 100)]
    [InlineData(5, 0, 0, 5)]
    public void ToPercent_scales_ddc_range(int value, int min, int max, int expected) =>
        Assert.Equal(expected, BrightnessSourcePicker.ToPercent(value, min, max));

    [Theory]
    [InlineData(0, 10, 60, 10)]
    [InlineData(100, 10, 60, 60)]
    [InlineData(40, 10, 60, 30)]
    [InlineData(150, 0, 100, 100)]
    public void FromPercent_scales_to_ddc_range(int percent, int min, int max, int expected) =>
        Assert.Equal(expected, BrightnessSourcePicker.FromPercent(percent, min, max));

    [Fact]
    public void Queue_first_post_starts_worker_later_posts_coalesce()
    {
        var q = new BrightnessWriteQueue();

        Assert.True(q.Post(10));
        Assert.False(q.Post(20));
        Assert.False(q.Post(30));

        Assert.True(q.TryTake(out var v));
        Assert.Equal(30, v);
        Assert.False(q.TryTake(out _));
    }

    [Fact]
    public void Queue_value_posted_while_applying_is_applied_next_without_second_worker()
    {
        var q = new BrightnessWriteQueue();
        Assert.True(q.Post(10));
        Assert.True(q.TryTake(out var first));
        Assert.Equal(10, first);

        // Uygulama sürerken gelen değerler birikmez, yeni işçi de başlamaz.
        Assert.False(q.Post(40));
        Assert.False(q.Post(55));

        Assert.True(q.TryTake(out var next));
        Assert.Equal(55, next);
        Assert.False(q.TryTake(out _));
    }

    [Fact]
    public void Queue_restarts_worker_after_it_drained()
    {
        var q = new BrightnessWriteQueue();
        Assert.True(q.Post(1));
        Assert.True(q.TryTake(out _));
        Assert.False(q.TryTake(out _));

        Assert.True(q.Post(2));
    }

    [Fact]
    public void Queue_clamps_values()
    {
        var q = new BrightnessWriteQueue();
        q.Post(500);
        Assert.True(q.TryTake(out var v));
        Assert.Equal(100, v);
    }
}
