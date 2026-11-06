namespace Koy.Core.Tests;

/// <summary>
/// Test saati: zaman yalnız <see cref="Advance"/> ile ilerler ve kurulu zamanlayıcılar (Change ile durdurulup
/// yeniden kurulanlar dahil) gerçek <see cref="System.Threading.Timer"/> gibi tetiklenir.
/// </summary>
internal sealed class StepTimeProvider : TimeProvider
{
    private long _now;
    private readonly List<StepTimer> _timers = [];

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _now;

    /// <summary>Şu an kurulu (tetiklenecek) zamanlayıcı sayısı: boşta CPU kuralını denetlemek için.</summary>
    public int ActiveTimers => _timers.Count(t => t.IsScheduled);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new StepTimer(this, callback, state);
        timer.Change(dueTime, period);
        _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan duration)
    {
        var target = _now + duration.Ticks;
        while (true)
        {
            var next = _timers.Where(t => t.IsScheduled && t.Next <= target).OrderBy(t => t.Next).FirstOrDefault();
            if (next is null)
                break;

            _now = next.Next;
            next.Fire();
        }

        _now = target;
    }

    private sealed class StepTimer(StepTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        private long _period;
        public long Next { get; private set; } = -1;
        public bool IsScheduled => Next >= 0;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Next = dueTime == Timeout.InfiniteTimeSpan ? -1 : owner._now + dueTime.Ticks;
            _period = period == Timeout.InfiniteTimeSpan ? 0 : period.Ticks;
            return true;
        }

        public void Fire()
        {
            Next = _period > 0 ? Next + _period : -1;
            callback(state);
        }

        public void Dispose() => Next = -1;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
