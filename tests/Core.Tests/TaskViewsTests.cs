using Koy.Core.Tasks;

namespace Koy.Core.Tests;

public sealed class TaskViewsTests
{
    private static readonly DateOnly Thursday = new(2026, 11, 5); // Perşembe

    [Fact]
    public void Range_Day_IsToday()
    {
        Assert.Equal((Thursday, Thursday), TaskViews.Range(TaskScope.Day, Thursday));
    }

    [Fact]
    public void Range_Week_StartsOnMonday_AndEndsOnSunday()
    {
        var (from, to) = TaskViews.Range(TaskScope.Week, Thursday);
        Assert.Equal(new DateOnly(2026, 11, 2), from);
        Assert.Equal(DayOfWeek.Monday, from.DayOfWeek);
        Assert.Equal(new DateOnly(2026, 11, 8), to);
        Assert.Equal(DayOfWeek.Sunday, to.DayOfWeek);
    }

    [Fact]
    public void Range_Week_OnSunday_BelongsToTheWeekThatEndsToday()
    {
        var sunday = new DateOnly(2026, 11, 8);
        var (from, to) = TaskViews.Range(TaskScope.Week, sunday);
        Assert.Equal(new DateOnly(2026, 11, 2), from);
        Assert.Equal(sunday, to);
    }

    [Fact]
    public void Range_Week_OnMonday_StartsToday()
    {
        var monday = new DateOnly(2026, 11, 2);
        Assert.Equal(monday, TaskViews.Range(TaskScope.Week, monday).From);
    }

    [Theory]
    [InlineData(2026, 11, 6, 30)]
    [InlineData(2026, 2, 10, 28)]
    [InlineData(2028, 2, 29, 29)]
    [InlineData(2026, 12, 31, 31)]
    public void Range_Month_CoversFirstToLastDay(int y, int m, int d, int lastDay)
    {
        var (from, to) = TaskViews.Range(TaskScope.Month, new DateOnly(y, m, d));
        Assert.Equal(new DateOnly(y, m, 1), from);
        Assert.Equal(new DateOnly(y, m, lastDay), to);
    }

    [Fact]
    public void Load_UsesGetToday_ForDay_AndGetRange_ForOthers()
    {
        var dir = Path.Combine(Path.GetTempPath(), "koy-tv-" + Guid.NewGuid().ToString("N"));
        try
        {
            var now = new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);
            var tasks = new TaskService(Path.Combine(dir, "tasks.json"), () => now);
            tasks.Add("bugün");
            tasks.Add("yarın", day: new DateOnly(2026, 11, 6));
            tasks.Add("gelecek ay", day: new DateOnly(2026, 12, 2));

            Assert.Equal(["bugün"], TaskViews.Load(tasks, TaskScope.Day, Thursday).Select(t => t.Title));
            Assert.Equal(["bugün", "yarın"], TaskViews.Load(tasks, TaskScope.Week, Thursday).Select(t => t.Title));
            Assert.Equal(["bugün", "yarın"], TaskViews.Load(tasks, TaskScope.Month, Thursday).Select(t => t.Title));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Theory]
    [InlineData("14:30", 14, 30)]
    [InlineData("9:05", 9, 5)]
    [InlineData("09.05", 9, 5)]
    [InlineData("1430", 14, 30)]
    [InlineData("930", 9, 30)]
    [InlineData(" 7:00 ", 7, 0)]
    public void TryParseTime_AcceptsCommonForms(string text, int hour, int minute)
    {
        Assert.True(TaskViews.TryParseTime(text, out var time));
        Assert.Equal(new TimeOnly(hour, minute), time);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseTime_Empty_MeansNoTime(string? text)
    {
        Assert.True(TaskViews.TryParseTime(text, out var time));
        Assert.Null(time);
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("12:60")]
    [InlineData("abc")]
    [InlineData("12")]
    [InlineData("12:3x")]
    public void TryParseTime_Invalid_ReturnsFalse(string text)
    {
        Assert.False(TaskViews.TryParseTime(text, out var time));
        Assert.Null(time);
    }

    [Fact]
    public void NextId_IsFirstUndone()
    {
        var done = Item("a", done: true);
        var next = Item("b");
        var later = Item("c");
        Assert.Equal(next.Id, TaskViews.NextId([done, next, later]));
        Assert.Null(TaskViews.NextId([done]));
        Assert.Null(TaskViews.NextId([]));
    }

    [Fact]
    public void Label_NextTaskWithoutTime_IsNow()
    {
        var (isNow, text) = TaskViews.Label(Item("a"), isNext: true, new TimeOnly(10, 0), Thursday);
        Assert.True(isNow);
        Assert.Equal("", text);
    }

    [Fact]
    public void Label_NextTaskInTheFuture_ShowsItsTime()
    {
        var (isNow, text) = TaskViews.Label(Item("a", time: new TimeOnly(16, 0)), isNext: true, new TimeOnly(10, 0), Thursday);
        Assert.False(isNow);
        Assert.Equal("16:00", text);
    }

    [Fact]
    public void Label_NextTaskWhoseTimeHasCome_IsNow()
    {
        var (isNow, _) = TaskViews.Label(Item("a", time: new TimeOnly(9, 30)), isNext: true, new TimeOnly(10, 0), Thursday);
        Assert.True(isNow);
    }

    [Fact]
    public void Label_LeftoverFromEarlierDay_IsNowWhenNext()
    {
        var old = Item("a", time: new TimeOnly(23, 0), day: Thursday.AddDays(-1));
        Assert.True(TaskViews.Label(old, isNext: true, new TimeOnly(8, 0), Thursday).IsNow);
    }

    [Fact]
    public void Label_OtherTasks_ShowOnlyTheirTime()
    {
        Assert.Equal((false, "16:00"), TaskViews.Label(Item("a", time: new TimeOnly(16, 0)), isNext: false, new TimeOnly(10, 0), Thursday));
        Assert.Equal((false, ""), TaskViews.Label(Item("a"), isNext: false, new TimeOnly(10, 0), Thursday));
        Assert.Equal((false, "08:00"), TaskViews.Label(Item("a", time: new TimeOnly(8, 0), done: true), isNext: true, new TimeOnly(10, 0), Thursday));
    }

    private static TaskItem Item(string title, bool done = false, TimeOnly? time = null, DateOnly? day = null) =>
        new(Guid.NewGuid(), title, day ?? Thursday, time, done, DateTimeOffset.UnixEpoch);
}
