using Koy.Core.Timers;
using Xunit;

namespace Koy.Core.Tests;

public sealed class PomodoroTests
{
    [Fact]
    public void StartsInWorkMode_WithCorrectDuration()
    {
        var config = new PomodoroConfig(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15));
        using var pomodoro = new Pomodoro(config);

        Assert.Equal(PomodoroMode.Work, pomodoro.Mode);
        Assert.Equal(PomodoroState.Stopped, pomodoro.State);
        Assert.Equal(TimeSpan.FromMinutes(25), pomodoro.Remaining);
    }

    [Fact]
    public void Start_ChangesStateToRunning()
    {
        using var pomodoro = new Pomodoro();
        pomodoro.Start();
        Assert.Equal(PomodoroState.Running, pomodoro.State);

        pomodoro.Pause();
        Assert.Equal(PomodoroState.Paused, pomodoro.State);
    }

    [Fact]
    public void Tick_DecrementsRemainingTime()
    {
        var config = new PomodoroConfig(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
        using var pomodoro = new Pomodoro(config);
        pomodoro.Start();

        pomodoro.OnTimerTick();
        Assert.Equal(TimeSpan.FromSeconds(9), pomodoro.Remaining);
    }

    [Fact]
    public void SessionCompletion_TransitionsToShortBreak_ThenWork()
    {
        var config = new PomodoroConfig(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), 2);
        using var pomodoro = new Pomodoro(config);
        pomodoro.Start();

        PomodoroMode? completed = null;
        pomodoro.SessionCompleted += (_, m) => completed = m;

        // Tick 1
        pomodoro.OnTimerTick();
        Assert.Equal(TimeSpan.FromSeconds(1), pomodoro.Remaining);

        // Tick 2 (completes Work)
        pomodoro.OnTimerTick();
        Assert.Equal(PomodoroMode.Work, completed);
        Assert.Equal(1, pomodoro.CompletedWorkSessions);
        Assert.Equal(PomodoroMode.ShortBreak, pomodoro.Mode);
        Assert.Equal(TimeSpan.FromSeconds(1), pomodoro.Remaining);

        // Tick 3 (completes ShortBreak)
        pomodoro.OnTimerTick();
        Assert.Equal(PomodoroMode.ShortBreak, completed);
        Assert.Equal(PomodoroMode.Work, pomodoro.Mode);
        Assert.Equal(TimeSpan.FromSeconds(2), pomodoro.Remaining);
    }

    [Fact]
    public void SessionCompletion_AfterTargetSessions_TransitionsToLongBreak()
    {
        var config = new PomodoroConfig(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), 2);
        using var pomodoro = new Pomodoro(config);
        pomodoro.Start();

        // 1st Work
        pomodoro.OnTimerTick();
        Assert.Equal(PomodoroMode.ShortBreak, pomodoro.Mode);

        // 1st ShortBreak
        pomodoro.OnTimerTick();
        Assert.Equal(PomodoroMode.Work, pomodoro.Mode);

        // 2nd Work -> should transition to LongBreak!
        pomodoro.OnTimerTick();
        Assert.Equal(PomodoroMode.LongBreak, pomodoro.Mode);
        Assert.Equal(TimeSpan.FromSeconds(5), pomodoro.Remaining);
    }
}
