namespace Koy.Core.Timers;

public enum PomodoroMode
{
    Work,
    ShortBreak,
    LongBreak
}

public enum PomodoroState
{
    Stopped,
    Running,
    Paused
}

public sealed record PomodoroConfig(
    TimeSpan WorkDuration,
    TimeSpan ShortBreakDuration,
    TimeSpan LongBreakDuration,
    int SessionsBeforeLongBreak = 4)
{
    public static PomodoroConfig Default => new(
        TimeSpan.FromMinutes(25),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        4);
}

/// <summary>
/// Pomodoro durum makinesi ve zamanlayıcısı.
/// Saf C#: TimeProvider enjekte edilir, tüm durum geçişleri deterministik olarak test edilebilir.
/// </summary>
public sealed class Pomodoro : IDisposable
{
    private readonly TimeProvider _timeProvider;
    private ITimer? _timer;
    private bool _disposed;

    public PomodoroConfig Config { get; private set; }
    public PomodoroMode Mode { get; private set; } = PomodoroMode.Work;
    public PomodoroState State { get; private set; } = PomodoroState.Stopped;
    public TimeSpan Remaining { get; private set; }
    public int CompletedWorkSessions { get; private set; }

    public event EventHandler<TimeSpan>? Ticked;
    public event EventHandler<PomodoroState>? StateChanged;
    public event EventHandler<PomodoroMode>? ModeChanged;
    public event EventHandler<PomodoroMode>? SessionCompleted;

    public Pomodoro(PomodoroConfig? config = null, TimeProvider? timeProvider = null)
    {
        Config = config ?? PomodoroConfig.Default;
        _timeProvider = timeProvider ?? TimeProvider.System;
        Remaining = Config.WorkDuration;
    }

    public void Start()
    {
        if (State == PomodoroState.Running)
            return;

        State = PomodoroState.Running;
        StateChanged?.Invoke(this, State);

        _timer ??= _timeProvider.CreateTimer(
            _ => OnTimerTick(),
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));
    }

    public void Pause()
    {
        if (State != PomodoroState.Running)
            return;

        State = PomodoroState.Paused;
        _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        StateChanged?.Invoke(this, State);
    }

    public void Reset()
    {
        State = PomodoroState.Stopped;
        _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        Remaining = GetDurationForMode(Mode);
        StateChanged?.Invoke(this, State);
        Ticked?.Invoke(this, Remaining);
    }

    public void Skip()
    {
        AdvanceToNextMode();
    }

    public void OnTimerTick()
    {
        if (State != PomodoroState.Running)
            return;

        if (Remaining > TimeSpan.FromSeconds(1))
        {
            Remaining -= TimeSpan.FromSeconds(1);
            Ticked?.Invoke(this, Remaining);
        }
        else
        {
            Remaining = TimeSpan.Zero;
            Ticked?.Invoke(this, Remaining);
            CompleteCurrentSession();
        }
    }

    private void CompleteCurrentSession()
    {
        var finishedMode = Mode;
        if (finishedMode == PomodoroMode.Work)
        {
            CompletedWorkSessions++;
        }

        SessionCompleted?.Invoke(this, finishedMode);
        AdvanceToNextMode();
    }

    private void AdvanceToNextMode()
    {
        if (Mode == PomodoroMode.Work)
        {
            if (CompletedWorkSessions > 0 && CompletedWorkSessions % Config.SessionsBeforeLongBreak == 0)
                Mode = PomodoroMode.LongBreak;
            else
                Mode = PomodoroMode.ShortBreak;
        }
        else
        {
            Mode = PomodoroMode.Work;
        }

        Remaining = GetDurationForMode(Mode);
        ModeChanged?.Invoke(this, Mode);
        Ticked?.Invoke(this, Remaining);
    }

    public TimeSpan GetDurationForMode(PomodoroMode mode) => mode switch
    {
        PomodoroMode.Work => Config.WorkDuration,
        PomodoroMode.ShortBreak => Config.ShortBreakDuration,
        PomodoroMode.LongBreak => Config.LongBreakDuration,
        _ => Config.WorkDuration
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer?.Dispose();
        _timer = null;
    }
}
