using Koy.Core.QuickLaunch;
using Koy.Core.Shell;

namespace Koy.Core.Tests;

public class QuickLaunchTests
{
    private sealed class FakeShell : IShellActionService
    {
        public List<string> Launched { get; } = [];
        public bool Throw { get; set; }
        public void SendWinKey() { }
        public void ToggleSystemTray() { }
        public void OpenQuickSettings() { }
        public void OpenNotificationCenter() { }
        public void Launch(string path, string? arguments = null)
        {
            if (Throw) throw new InvalidOperationException("başlatılamadı");
            Launched.Add(path);
        }
    }

    private static (QuickLaunchService Service, List<IReadOnlyList<string>> Saves, FakeShell Shell) New(params string[] initial)
    {
        var saves = new List<IReadOnlyList<string>>();
        var shell = new FakeShell();
        return (new QuickLaunchService(initial, saves.Add, shell, _ => true), saves, shell);
    }

    [Fact]
    public void Add_keeps_order_and_saves()
    {
        var (s, saves, _) = New();
        Assert.True(s.Add(@"C:\a.exe"));
        Assert.True(s.Add(@"C:\b.exe"));

        Assert.Equal([@"C:\a.exe", @"C:\b.exe"], s.Items);
        Assert.Equal([@"C:\a.exe", @"C:\b.exe"], saves[^1]);
    }

    [Fact]
    public void Add_rejects_duplicates_ignoring_case_and_trailing_slash()
    {
        var (s, _, _) = New(@"C:\Klasor");
        Assert.False(s.Add(@"c:\klasor\"));
        Assert.Single(s.Items);
    }

    [Fact]
    public void Add_strips_quotes_and_rejects_blank()
    {
        var (s, _, _) = New();
        Assert.True(s.Add("  \"C:\\a.exe\" "));
        Assert.Equal(@"C:\a.exe", s.Items[0]);
        Assert.False(s.Add("  "));
    }

    [Fact]
    public void Add_rejects_missing_path()
    {
        var s = new QuickLaunchService([], _ => { }, new FakeShell(), _ => false);
        Assert.False(s.Add(@"C:\yok.exe"));
    }

    [Fact]
    public void Add_stops_at_eight()
    {
        var (s, _, _) = New();
        for (var i = 0; i < 8; i++)
            Assert.True(s.Add($@"C:\{i}.exe"));
        Assert.False(s.Add(@"C:\9.exe"));
        Assert.Equal(8, s.Items.Count);
    }

    [Fact]
    public void Initial_list_is_cleaned_deduped_and_capped()
    {
        var initial = new List<string> { @"C:\a", "", @"c:\A" };
        for (var i = 0; i < 12; i++)
            initial.Add($@"C:\{i}");
        var (s, _, _) = New([.. initial]);

        Assert.Equal(8, s.Items.Count);
        Assert.Equal(@"C:\a", s.Items[0]);
    }

    [Fact]
    public void Remove_and_move()
    {
        var (s, saves, _) = New(@"C:\a", @"C:\b", @"C:\c");

        Assert.True(s.Move(0, 2));
        Assert.Equal([@"C:\b", @"C:\c", @"C:\a"], s.Items);
        Assert.False(s.Move(5, 0));
        Assert.True(s.Remove(@"C:\C"));
        Assert.False(s.Remove(@"C:\yok"));
        Assert.Equal([@"C:\b", @"C:\a"], saves[^1]);
    }

    [Fact]
    public void Launch_goes_through_shell_and_swallows_failure()
    {
        var (s, _, shell) = New(@"C:\a");
        Assert.True(s.Launch(@"C:\a"));
        Assert.Equal([@"C:\a"], shell.Launched);

        shell.Throw = true;
        var logPath = Path.Combine(Path.GetTempPath(), "koy-ql-" + Guid.NewGuid() + ".log");
        Koy.Core.Diagnostics.CrashLog.LogPath = logPath;
        try
        {
            Assert.False(s.Launch(@"C:\a"));
        }
        finally
        {
            Koy.Core.Diagnostics.CrashLog.LogPath = Koy.Core.Diagnostics.CrashLog.DefaultPath;
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Theory]
    [InlineData(@"C:\Program Files\App\App.exe", "App")]
    [InlineData(@"C:\Users\x\Belgeler\", "Belgeler")]
    [InlineData(@"C:\", @"C:")]
    public void DisplayName(string path, string expected) => Assert.Equal(expected, QuickLaunchService.DisplayName(path));
}
