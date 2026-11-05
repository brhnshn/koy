namespace Koy.Core.Timers;

public enum CountdownState
{
    /// <summary>Hiç başlatılmadı ya da sıfırlandı; kalan süre = seçilen süre.</summary>
    Idle,
    Running,
    Paused,
    /// <summary>Süre doldu; sıfırlanana ya da yeniden başlatılana kadar bu durumda kalır.</summary>
    Finished
}

/// <summary>
/// Serbest geri sayım zamanlayıcısı durum makinesi. Saniyelik tik yoktur: çalışırken bitiş anı bir kez hesaplanır
/// ve tek atımlık bir zamanlayıcı yalnız bitişte tetiklenir; kalan süre sorulduğunda saatten hesaplanır
/// (<see cref="Remaining"/>). Böylece arka planda ve arayüz görünmezken hiçbir şey çalışmaz. Zaman
/// <see cref="TimeProvider"/> ile verilir; testler sahte saatle deterministik çalışır.
/// </summary>
public sealed class CountdownTimer : IDisposable
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(99);

    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private ITimer? _timer;
    private long _endTimestamp;
    private TimeSpan _remainingWhenStopped;
    private bool _disposed;

    public TimeSpan Duration { get; private set; }
    public CountdownState State { get; private set; } = CountdownState.Idle;

    /// <summary>Süre doldu. Zamanlayıcı iş parçacığından gelir; arayüz kendi iş parçacığına geçirmelidir.</summary>
    public event EventHandler? Finished;

    /// <summary>Durum değişti (başlat, duraklat, sıfırla, bitti, süre değişti).</summary>
    public event EventHandler<CountdownState>? StateChanged;

    public CountdownTimer(TimeSpan duration, TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        Duration = Clamp(duration);
        _remainingWhenStopped = Duration;
    }

    /// <summary>Kalan süre; çalışırken her sorguda saatten hesaplanır.</summary>
    public TimeSpan Remaining
    {
        get
        {
            lock (_gate)
            {
                return State switch
                {
                    CountdownState.Running => RemainingFromClock(),
                    CountdownState.Finished => TimeSpan.Zero,
                    _ => _remainingWhenStopped
                };
            }
        }
    }

    /// <summary>
    /// Süreyi değiştirir; yalnız çalışmıyorken geçerlidir (çalışan zamanlayıcının süresi oynatılmaz).
    /// Yeni süre kalan süre olur ve zamanlayıcı başlangıç durumuna döner. Geçerli değer 1 sn - 99 saat.
    /// </summary>
    public bool SetDuration(TimeSpan duration)
    {
        lock (_gate)
        {
            if (State == CountdownState.Running)
                return false;

            Duration = Clamp(duration);
            _remainingWhenStopped = Duration;
            State = CountdownState.Idle;
        }

        StateChanged?.Invoke(this, CountdownState.Idle);
        return true;
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_disposed || State == CountdownState.Running)
                return;

            if (State == CountdownState.Finished || _remainingWhenStopped <= TimeSpan.Zero)
                _remainingWhenStopped = Duration;

            _endTimestamp = _timeProvider.GetTimestamp() + ToTicks(_remainingWhenStopped);
            State = CountdownState.Running;

            _timer?.Dispose();
            _timer = _timeProvider.CreateTimer(_ => OnDue(), null, _remainingWhenStopped, Timeout.InfiniteTimeSpan);
        }

        StateChanged?.Invoke(this, CountdownState.Running);
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (State != CountdownState.Running)
                return;

            _remainingWhenStopped = RemainingFromClock();
            State = CountdownState.Paused;
            StopTimer();
        }

        StateChanged?.Invoke(this, CountdownState.Paused);
    }

    public void Reset()
    {
        lock (_gate)
        {
            StopTimer();
            _remainingWhenStopped = Duration;
            State = CountdownState.Idle;
        }

        StateChanged?.Invoke(this, CountdownState.Idle);
    }

    private void OnDue()
    {
        lock (_gate)
        {
            if (State != CountdownState.Running)
                return;

            // Sistem zamanlayıcısı bitişten milisaniyeler önce tetiklenebilir: kalan süre varsa kalanı bekle.
            var left = RemainingFromClock();
            if (left > TimeSpan.Zero)
            {
                _timer?.Dispose();
                _timer = _timeProvider.CreateTimer(_ => OnDue(), null, left + TimeSpan.FromMilliseconds(5), Timeout.InfiniteTimeSpan);
                return;
            }

            State = CountdownState.Finished;
            _remainingWhenStopped = TimeSpan.Zero;
            StopTimer();
        }

        StateChanged?.Invoke(this, CountdownState.Finished);
        Finished?.Invoke(this, EventArgs.Empty);
    }

    private TimeSpan RemainingFromClock()
    {
        var left = _endTimestamp - _timeProvider.GetTimestamp();
        return left <= 0 ? TimeSpan.Zero : _timeProvider.GetElapsedTime(0, left);
    }

    private long ToTicks(TimeSpan span) =>
        (long)(span.TotalSeconds * _timeProvider.TimestampFrequency);

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private static TimeSpan Clamp(TimeSpan value) =>
        value < MinDuration ? MinDuration : value > MaxDuration ? MaxDuration : value;

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            StopTimer();
        }
    }
}
