using Koy.Core.Tasks;

namespace Koy.Core.Tests;

public sealed class TaskServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "koy-tasks-" + Guid.NewGuid());
    private DateTimeOffset _now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);

    private string FilePath => Path.Combine(_dir, "tasks.json");

    private TaskService New(Action<string, Exception>? onError = null) => new(FilePath, () => _now, onError);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    private DateOnly Today => DateOnly.FromDateTime(_now.LocalDateTime);

    [Fact]
    public void Add_trims_title_and_rejects_blank()
    {
        var s = New();
        Assert.Null(s.Add("   "));
        var t = s.Add("  Rapor  ", new TimeOnly(14, 30))!;
        Assert.Equal("Rapor", t.Title);
        Assert.Equal(new TimeOnly(14, 30), t.Time);
        Assert.Equal(Today, t.Day);
        Assert.False(t.Done);
    }

    [Fact]
    public void Today_sorts_timed_by_time_then_untimed_then_done_last()
    {
        var s = New();
        s.Add("saatsiz");
        s.Add("öğleden sonra", new TimeOnly(15, 0));
        s.Add("sabah", new TimeOnly(9, 0));
        var done = s.Add("bitti", new TimeOnly(8, 0))!;
        s.SetDone(done.Id, true);

        Assert.Equal(["sabah", "öğleden sonra", "saatsiz", "bitti"], s.GetToday().Select(t => t.Title));
    }

    [Fact]
    public void New_day_hides_yesterdays_done_but_keeps_pending_and_does_not_delete()
    {
        var s = New();
        var finished = s.Add("dünkü bitti")!;
        s.Add("dünkü yarım");
        s.SetDone(finished.Id, true);

        _now = _now.AddDays(1);

        Assert.Equal(["dünkü yarım"], s.GetToday().Select(t => t.Title));
        Assert.Equal(2, s.GetRange(Today.AddDays(-1), Today.AddDays(-1)).Count);
    }

    [Fact]
    public void Update_Done_Remove()
    {
        var s = New();
        var t = s.Add("a")!;

        Assert.True(s.Update(t.Id, " b ", new TimeOnly(10, 0)));
        Assert.False(s.Update(t.Id, "", null));
        Assert.False(s.Update(Guid.NewGuid(), "x", null));
        Assert.True(s.SetDone(t.Id, true));
        Assert.False(s.SetDone(t.Id, true));
        var now = s.GetToday().Single();
        Assert.Equal("b", now.Title);
        Assert.True(now.Done);

        Assert.True(s.Remove(t.Id));
        Assert.False(s.Remove(t.Id));
        Assert.Empty(s.GetToday());
    }

    [Fact]
    public void Day_and_range_queries()
    {
        var s = New();
        s.Add("bugün", day: Today);
        s.Add("yarın", day: Today.AddDays(1));
        s.Add("gelecek hafta", day: Today.AddDays(8));

        Assert.Equal(["yarın"], s.GetDay(Today.AddDays(1)).Select(t => t.Title));
        Assert.Equal(["bugün", "yarın"], s.GetRange(Today, Today.AddDays(6)).Select(t => t.Title));
    }

    [Fact]
    public void Persists_across_instances()
    {
        var s = New();
        s.Add("kalıcı", new TimeOnly(7, 5));

        var loaded = New().GetToday().Single();
        Assert.Equal("kalıcı", loaded.Title);
        Assert.Equal(new TimeOnly(7, 5), loaded.Time);
    }

    [Fact]
    public void Keeps_at_most_200_pruning_old_done_first()
    {
        var s = New();
        var old = s.Add("eski bitmiş", day: Today.AddDays(-30))!;
        s.SetDone(old.Id, true);
        for (var i = 0; i < ITaskService.MaxTasks; i++)
            s.Add("g" + i);

        var all = s.GetRange(Today.AddDays(-60), Today.AddDays(1));
        Assert.Equal(ITaskService.MaxTasks, all.Count);
        Assert.DoesNotContain(all, t => t.Title == "eski bitmiş");
    }

    [Fact]
    public void Corrupt_file_starts_empty_and_reports_error()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "[ bozuk");
        var errors = new List<string>();

        var s = New((src, _) => errors.Add(src));

        Assert.Empty(s.GetToday());
        Assert.Single(errors);
        Assert.NotNull(s.Add("yeniden"));
    }

    [Fact]
    public void Changed_fires_on_mutations()
    {
        var s = New();
        var count = 0;
        s.Changed += (_, _) => count++;
        var t = s.Add("a")!;
        s.SetDone(t.Id, true);
        s.Remove(t.Id);
        Assert.Equal(3, count);
    }
}
