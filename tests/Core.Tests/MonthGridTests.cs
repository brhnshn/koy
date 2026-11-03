using Koy.Core.Calendar;
using Xunit;

namespace Koy.Core.Tests;

public sealed class MonthGridTests
{
    [Fact]
    public void Build_AlwaysReturns42Days()
    {
        var grid = MonthGrid.Build(2026, 11, new DateOnly(2026, 11, 4));
        Assert.Equal(42, grid.Count);
    }

    [Fact]
    public void Build_FirstDayIsMonday()
    {
        var grid = MonthGrid.Build(2026, 11, new DateOnly(2026, 11, 4));
        Assert.Equal(DayOfWeek.Monday, grid[0].Date.DayOfWeek);
    }

    [Fact]
    public void Build_CorrectlyFlagsCurrentMonthAndToday()
    {
        var today = new DateOnly(2026, 11, 4);
        var grid = MonthGrid.Build(2026, 11, today);

        var todayItem = grid.FirstOrDefault(d => d.IsToday);
        Assert.NotNull(todayItem);
        Assert.Equal(today, todayItem.Date);
        Assert.True(todayItem.IsCurrentMonth);

        // November 2026 starts on Sunday (1st Nov 2026 is Sunday)
        // So offset is 6 days (26 Oct 2026 is Monday)
        Assert.False(grid[0].IsCurrentMonth);
        Assert.Equal(new DateOnly(2026, 10, 26), grid[0].Date);
    }
}
