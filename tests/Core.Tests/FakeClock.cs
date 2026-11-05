namespace Koy.Core.Tests;

/// <summary>
/// Elle ilerletilen saat: <see cref="Advance"/> zamanı kaydırır ve vadesi gelen tek atımlık zamanlayıcıları tetikler.
/// Zamanlayıcı ve kronometre testleri gerçek beklemeden çalışsın diye.
/// </summary>
internal sealed class FakeClock : TimeProvider
{
    private sealed class FakeTimer(FakeClock owner, TimerCallback callback, object? state, TimeSpan due) : ITimer
    {
        public TimeSpan Due { get; private set; } = due;
        public bool Active { get; private set; } = due != Timeout.InfiniteTimeSpan;
        public TimeSpan DueAt { get; private set; } = owner._now + due;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Due = dueTime;
            Active = dueTime != Timeout.InfiniteTimeSpan;
            DueAt = owner._now + dueTime;
            return true;
        }

        public void Fire()
        {
            Active = false;
            callback(state);
        }

        public void Dispose() => Active = false;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private TimeSpan _now = TimeSpan.Zero;
    private readonly List<FakeTimer> _timers = [];

    public override long TimestampFrequency => 1000;

    public override long GetTimestamp() => (long)_now.TotalMilliseconds;

    public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 11, 6, 9, 0, 0, TimeSpan.Zero) + _now;

    public int ActiveTimers => _timers.Count(t => t.Active);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FakeTimer(this, callback, state, dueTime);
        _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        var target = _now + by;
        while (true)
        {
            var next = _timers.Where(t => t.Active && t.DueAt <= target).OrderBy(t => t.DueAt).FirstOrDefault();
            if (next is null)
                break;

            _now = next.DueAt > _now ? next.DueAt : _now;
            next.Fire();
        }

        _now = target;
    }
}
