using Koy.Core.Timers;

namespace Koy.Core.Tests;

public sealed class FocusTimersTests
{
    private static readonly PomodoroConfig ShortConfig = new(
        TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), 2);

    private static FocusTimers Create(StepTimeProvider time) =>
        new(ShortConfig, TimeSpan.FromMinutes(1), time);

    [Fact]
    public void Pomodoro_PauseThenStart_KeepsCounting()
    {
        // Eski hata: Duraklat zamanlayıcıyı sonsuza çekiyordu, Başlat yeniden kurmuyordu; süre bir daha akmıyordu.
        var time = new StepTimeProvider();
        using var pomodoro = new Pomodoro(ShortConfig, time);

        pomodoro.Start();
        time.Advance(TimeSpan.FromSeconds(3));
        pomodoro.Pause();
        time.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(TimeSpan.FromSeconds(7), pomodoro.Remaining);

        pomodoro.Start();
        time.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(TimeSpan.FromSeconds(5), pomodoro.Remaining);
    }

    [Fact]
    public void Pomodoro_ResetThenStart_CountsAgain()
    {
        var time = new StepTimeProvider();
        using var pomodoro = new Pomodoro(ShortConfig, time);

        pomodoro.Start();
        time.Advance(TimeSpan.FromSeconds(4));
        pomodoro.Reset();
        pomodoro.Start();
        time.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(TimeSpan.FromSeconds(8), pomodoro.Remaining);
    }

    [Fact]
    public void Pomodoro_SetMode_SwitchesAndPauses()
    {
        var time = new StepTimeProvider();
        using var pomodoro = new Pomodoro(ShortConfig, time);
        pomodoro.Start();

        pomodoro.SetMode(PomodoroMode.ShortBreak);

        Assert.Equal(PomodoroMode.ShortBreak, pomodoro.Mode);
        Assert.Equal(PomodoroState.Paused, pomodoro.State);
        Assert.Equal(TimeSpan.FromSeconds(4), pomodoro.Remaining);
    }

    [Fact]
    public void Pomodoro_CurrentRound_FollowsCycle()
    {
        var time = new StepTimeProvider();
        using var pomodoro = new Pomodoro(ShortConfig, time); // 2 turluk döngü
        pomodoro.Start();

        Assert.Equal((PomodoroMode.Work, 1), (pomodoro.Mode, pomodoro.CurrentRound));

        time.Advance(pomodoro.Remaining); // 1. çalışma bitti -> kısa mola
        Assert.Equal((PomodoroMode.ShortBreak, 1), (pomodoro.Mode, pomodoro.CurrentRound));

        time.Advance(pomodoro.Remaining); // mola bitti -> 2. tur
        Assert.Equal((PomodoroMode.Work, 2), (pomodoro.Mode, pomodoro.CurrentRound));

        time.Advance(pomodoro.Remaining); // 2. çalışma bitti -> uzun mola
        Assert.Equal((PomodoroMode.LongBreak, 2), (pomodoro.Mode, pomodoro.CurrentRound));

        time.Advance(pomodoro.Remaining); // uzun mola bitti -> yeni döngü
        Assert.Equal((PomodoroMode.Work, 1), (pomodoro.Mode, pomodoro.CurrentRound));
    }

    [Fact]
    public void Running_KeepsCountingRegardlessOfAnyView()
    {
        // Sekme değiştirmek, çentiği gizlemek ya da başka bir alt sekmeye bakmak görünüm işidir; süre FocusTimers'ta akar.
        var time = new StepTimeProvider();
        using var timers = Create(time);
        timers.Toggle(FocusKind.Focus);

        time.Advance(TimeSpan.FromSeconds(3));
        _ = timers.Read(FocusKind.Stopwatch);
        _ = timers.Read(FocusKind.Countdown);
        time.Advance(TimeSpan.FromSeconds(2));

        var running = timers.Running;
        Assert.NotNull(running);
        Assert.Equal(FocusKind.Focus, running.Value.Kind);
        Assert.Equal(TimeSpan.FromSeconds(5), running.Value.Value);
        Assert.True(running.Value.CountsDown);
    }

    [Fact]
    public void StartingAnotherTimer_PausesTheRunningOne()
    {
        var time = new StepTimeProvider();
        using var timers = Create(time);
        timers.Toggle(FocusKind.Focus);
        time.Advance(TimeSpan.FromSeconds(2));

        timers.Toggle(FocusKind.Stopwatch);
        time.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(PomodoroState.Paused, timers.Pomodoro.State);
        Assert.Equal(TimeSpan.FromSeconds(8), timers.Pomodoro.Remaining);
        Assert.Equal(FocusKind.Stopwatch, timers.Running!.Value.Kind);
        Assert.Equal(TimeSpan.FromSeconds(3), timers.Running.Value.Value);
        Assert.False(timers.Running.Value.CountsDown);
    }

    [Fact]
    public void Toggle_OnRunningTimer_Pauses()
    {
        var time = new StepTimeProvider();
        using var timers = Create(time);
        timers.Toggle(FocusKind.Countdown);
        time.Advance(TimeSpan.FromSeconds(10));

        timers.Toggle(FocusKind.Countdown);
        time.Advance(TimeSpan.FromSeconds(10));

        Assert.Null(timers.Running);
        Assert.Equal(TimeSpan.FromSeconds(50), timers.Countdown.Remaining);
    }

    [Fact]
    public void Idle_HasNoScheduledTimers()
    {
        // Boşta CPU kuralı: hiçbir şey çalışmıyorsa hiçbir zamanlayıcı kurulu kalmaz.
        var time = new StepTimeProvider();
        using var timers = Create(time);

        timers.Toggle(FocusKind.Focus);
        Assert.Equal(1, time.ActiveTimers);
        timers.Toggle(FocusKind.Focus);
        Assert.Equal(0, time.ActiveTimers);

        timers.Toggle(FocusKind.Countdown);
        Assert.Equal(1, time.ActiveTimers);
        timers.Reset(FocusKind.Countdown);
        Assert.Equal(0, time.ActiveTimers);

        timers.Toggle(FocusKind.Stopwatch);
        timers.Reset(FocusKind.Stopwatch);
        Assert.Equal(0, time.ActiveTimers);
        Assert.Null(timers.Running);
    }

    [Fact]
    public void SelectPomodoroKind_SwitchesBetweenFocusAndBreak()
    {
        var time = new StepTimeProvider();
        using var timers = Create(time);

        timers.SelectPomodoroKind(FocusKind.Break);
        Assert.Equal(PomodoroMode.ShortBreak, timers.Pomodoro.Mode);
        Assert.Equal(TimeSpan.FromSeconds(4), timers.Read(FocusKind.Break).Value);

        timers.SelectPomodoroKind(FocusKind.Focus);
        Assert.Equal(PomodoroMode.Work, timers.Pomodoro.Mode);
    }

    [Fact]
    public void Finished_IsRaisedWhenSessionEnds_AndBreakStartsRunning()
    {
        var time = new StepTimeProvider();
        using var timers = Create(time);
        var finished = new List<FocusKind>();
        timers.Finished += (_, kind) => finished.Add(kind);

        timers.Toggle(FocusKind.Focus);
        time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal([FocusKind.Focus], finished);
        Assert.Equal(FocusKind.Break, timers.Running!.Value.Kind);
    }

    [Fact]
    public void Finished_IsRaisedWhenCountdownEnds()
    {
        var time = new StepTimeProvider();
        using var timers = Create(time);
        var finished = new List<FocusKind>();
        timers.Finished += (_, kind) => finished.Add(kind);

        timers.Toggle(FocusKind.Countdown);
        time.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal([FocusKind.Countdown], finished);
        Assert.Null(timers.Running);
        Assert.Equal(0, time.ActiveTimers);
    }

    [Fact]
    public void Changed_IsNotRaisedOnEverySecondTick()
    {
        // Saniyelik tik arayüze olay olarak akmaz; süre metni arayüzün kendi zamanlayıcısıyla okunur.
        var time = new StepTimeProvider();
        using var timers = Create(time);
        timers.Toggle(FocusKind.Focus);
        var changes = 0;
        timers.Changed += (_, _) => changes++;

        time.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(0, changes);

        timers.Toggle(FocusKind.Focus);
        Assert.True(changes > 0);
    }

    [Theory]
    [InlineData(18 * 60 + 24, 18)]
    [InlineData(59, 0)]
    [InlineData(60, 1)]
    [InlineData(-1, 0)]
    public void WholeMinutes_RoundsDown(int seconds, int expected) =>
        Assert.Equal(expected, new FocusReading(FocusKind.Focus, TimeSpan.FromSeconds(seconds), true).WholeMinutes);
}
