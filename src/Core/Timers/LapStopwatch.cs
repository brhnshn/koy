namespace Koy.Core.Timers;

public enum StopwatchState
{
    Idle,
    Running,
    Paused
}

/// <summary>Bir tur: sıra numarası, başlangıçtan toplam süre ve bir önceki turdan beri geçen süre.</summary>
public sealed record StopwatchLap(int Number, TimeSpan Total, TimeSpan Split);

/// <summary>
/// Kronometre durum makinesi. Zamanlayıcı ya da tik yoktur: geçen süre sorulduğunda saatten hesaplanır, bu yüzden
/// arayüz görünmezken hiçbir iş yapılmaz. Başlat / duraklat / tur / sıfırla. Zaman <see cref="TimeProvider"/> ile verilir.
/// </summary>
public sealed class LapStopwatch
{
    /// <summary>Saklanan tur sayısı üst sınırı (listeyi sınırsız büyütmemek için); en eski tur düşer.</summary>
    public const int MaxLaps = 99;

    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private readonly List<StopwatchLap> _laps = [];
    private long _startTimestamp;
    private TimeSpan _accumulated;
    private TimeSpan _lastLapTotal;
    private int _lapCounter;

    public LapStopwatch(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public StopwatchState State { get; private set; } = StopwatchState.Idle;

    public event EventHandler<StopwatchState>? StateChanged;

    public event EventHandler? LapsChanged;

    public TimeSpan Elapsed
    {
        get
        {
            lock (_gate)
            {
                return ElapsedLocked();
            }
        }
    }

    /// <summary>Turlar, en yeni en başta.</summary>
    public IReadOnlyList<StopwatchLap> Laps
    {
        get
        {
            lock (_gate)
            {
                return _laps.AsEnumerable().Reverse().ToList();
            }
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (State == StopwatchState.Running)
                return;

            _startTimestamp = _timeProvider.GetTimestamp();
            State = StopwatchState.Running;
        }

        StateChanged?.Invoke(this, StopwatchState.Running);
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (State != StopwatchState.Running)
                return;

            _accumulated = ElapsedLocked();
            State = StopwatchState.Paused;
        }

        StateChanged?.Invoke(this, StopwatchState.Paused);
    }

    /// <summary>Tur ekler; yalnız çalışırken geçerlidir. Eklenen turu döner (çalışmıyorsa null).</summary>
    public StopwatchLap? Lap()
    {
        StopwatchLap lap;
        lock (_gate)
        {
            if (State != StopwatchState.Running)
                return null;

            var total = ElapsedLocked();
            lap = new StopwatchLap(++_lapCounter, total, total - _lastLapTotal);
            _lastLapTotal = total;
            _laps.Add(lap);
            if (_laps.Count > MaxLaps)
                _laps.RemoveAt(0);
        }

        LapsChanged?.Invoke(this, EventArgs.Empty);
        return lap;
    }

    public void Reset()
    {
        lock (_gate)
        {
            State = StopwatchState.Idle;
            _accumulated = TimeSpan.Zero;
            _lastLapTotal = TimeSpan.Zero;
            _lapCounter = 0;
            _laps.Clear();
        }

        StateChanged?.Invoke(this, StopwatchState.Idle);
        LapsChanged?.Invoke(this, EventArgs.Empty);
    }

    private TimeSpan ElapsedLocked() =>
        State == StopwatchState.Running
            ? _accumulated + _timeProvider.GetElapsedTime(_startTimestamp)
            : _accumulated;
}
