namespace Koy.Core.Timers;

/// <summary>Odak sekmesindeki dört alt sekme.</summary>
public enum FocusKind
{
    Focus,
    Break,
    Countdown,
    Stopwatch
}

/// <summary>
/// Bir zamanlayıcının o anki okuması. <paramref name="CountsDown"/> yanlışsa değer geçen süredir (kronometre).
/// </summary>
public readonly record struct FocusReading(FocusKind Kind, TimeSpan Value, bool CountsDown)
{
    /// <summary>Tam dakika (alta yuvarlanır): 18:24 için 18. Ana'daki "18 dk kaldı" satırı için.</summary>
    public int WholeMinutes => Value <= TimeSpan.Zero ? 0 : (int)Value.TotalMinutes;
}

/// <summary>
/// Pomodoro, serbest zamanlayıcı ve kronometrenin tek sahibi. Görünümden bağımsızdır: çentik sekme değiştirse,
/// gizlense ya da birden çok monitörde pencere olsa da süre aynı yerden akar (NotchFeature tek örnek kurar ve
/// bütün pencerelere verir). Aynı anda en çok bir zamanlayıcı çalışır: birini başlatmak çalışanı duraklatır.
/// Kapalı çentikteki kalan süre ve Ana'daki "Odak · 18 dk kaldı" satırı <see cref="Running"/> okumasından beslenir.
/// Olaylar zamanlayıcının iş parçacığından gelir; arayüz kendi iş parçacığına aktarmalıdır.
/// </summary>
public sealed class FocusTimers : IDisposable
{
    private bool _disposed;

    public Pomodoro Pomodoro { get; }
    public CountdownTimer Countdown { get; }
    public LapStopwatch Stopwatch { get; }

    /// <summary>
    /// Bir zamanlayıcının durumu değişti (başlat, duraklat, sıfırla, kip, süre, tur). Saniyelik tik DEĞİLDİR:
    /// süre metnini arayüz kendi tazeleme zamanlayıcısıyla, yalnız görünürken okur.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>Bir Pomodoro oturumu ya da serbest zamanlayıcı bitti.</summary>
    public event EventHandler<FocusKind>? Finished;

    public FocusTimers(PomodoroConfig? config = null, TimeSpan? countdownDuration = null, TimeProvider? timeProvider = null)
    {
        Pomodoro = new Pomodoro(config, timeProvider);
        Countdown = new CountdownTimer(
            countdownDuration ?? TimeSpan.FromMinutes(CountdownPresets.DefaultMinutes), timeProvider);
        Stopwatch = new LapStopwatch(timeProvider);

        Pomodoro.StateChanged += (_, _) => RaiseChanged();
        Pomodoro.ModeChanged += (_, _) => RaiseChanged();
        Pomodoro.SessionCompleted += OnPomodoroSessionCompleted;
        Countdown.StateChanged += (_, _) => RaiseChanged();
        Countdown.Finished += (_, _) => Finished?.Invoke(this, FocusKind.Countdown);
        Stopwatch.StateChanged += (_, _) => RaiseChanged();
        Stopwatch.LapsChanged += (_, _) => RaiseChanged();
    }

    public bool IsRunning(FocusKind kind) => kind switch
    {
        FocusKind.Focus or FocusKind.Break => Pomodoro.State == PomodoroState.Running
            && (Pomodoro.Mode == PomodoroMode.Work) == (kind == FocusKind.Focus),
        FocusKind.Countdown => Countdown.State == CountdownState.Running,
        _ => Stopwatch.State == StopwatchState.Running
    };

    /// <summary>Çalışan zamanlayıcının okuması; hiçbiri çalışmıyorsa null.</summary>
    public FocusReading? Running
    {
        get
        {
            if (Pomodoro.State == PomodoroState.Running)
                return Read(Pomodoro.Mode == PomodoroMode.Work ? FocusKind.Focus : FocusKind.Break);
            if (Countdown.State == CountdownState.Running)
                return Read(FocusKind.Countdown);
            if (Stopwatch.State == StopwatchState.Running)
                return Read(FocusKind.Stopwatch);
            return null;
        }
    }

    public FocusReading Read(FocusKind kind) => kind switch
    {
        FocusKind.Focus or FocusKind.Break => new FocusReading(kind, Pomodoro.Remaining, true),
        FocusKind.Countdown => new FocusReading(kind, Countdown.Remaining, true),
        _ => new FocusReading(kind, Stopwatch.Elapsed, false)
    };

    /// <summary>
    /// Odak / Mola alt sekmesi: Pomodoro'yu çalışma ya da mola kipine geçirir (çalışıyorsa duraklatır).
    /// Uzun mola sırasında "Mola" seçmek bir şey yapmaz. Sayaç ve kronometre için işlem yoktur.
    /// </summary>
    public void SelectPomodoroKind(FocusKind kind)
    {
        if (kind == FocusKind.Focus)
            Pomodoro.SetMode(PomodoroMode.Work);
        else if (kind == FocusKind.Break && Pomodoro.Mode == PomodoroMode.Work)
            Pomodoro.SetMode(PomodoroMode.ShortBreak);
    }

    /// <summary>Başlatır ya da duraklatır; başlatırken çalışan diğer zamanlayıcıyı duraklatır.</summary>
    public void Toggle(FocusKind kind)
    {
        if (IsRunning(kind))
        {
            Pause(kind);
            return;
        }

        if (kind is not (FocusKind.Focus or FocusKind.Break))
            Pomodoro.Pause();
        if (kind != FocusKind.Countdown)
            Countdown.Pause();
        if (kind != FocusKind.Stopwatch)
            Stopwatch.Pause();

        switch (kind)
        {
            case FocusKind.Focus or FocusKind.Break:
                Pomodoro.Start();
                break;
            case FocusKind.Countdown:
                Countdown.Start();
                break;
            default:
                Stopwatch.Start();
                break;
        }
    }

    public void Reset(FocusKind kind)
    {
        switch (kind)
        {
            case FocusKind.Focus or FocusKind.Break:
                Pomodoro.Reset();
                break;
            case FocusKind.Countdown:
                Countdown.Reset();
                break;
            default:
                Stopwatch.Reset();
                break;
        }
    }

    private void Pause(FocusKind kind)
    {
        switch (kind)
        {
            case FocusKind.Focus or FocusKind.Break:
                Pomodoro.Pause();
                break;
            case FocusKind.Countdown:
                Countdown.Pause();
                break;
            default:
                Stopwatch.Pause();
                break;
        }
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private void OnPomodoroSessionCompleted(object? sender, PomodoroMode mode) =>
        Finished?.Invoke(this, mode == PomodoroMode.Work ? FocusKind.Focus : FocusKind.Break);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Pomodoro.Dispose();
        Countdown.Dispose();
    }
}
