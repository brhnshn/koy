using Koy.Core.Dock;

namespace Koy.Core.Tests;

public class RefreshDebouncerTests
{
    internal sealed class ManualTimeProvider : TimeProvider
    {
        private sealed class ManualTimer(Action<object?> callback, object? state) : ITimer
        {
            public bool Active { get; private set; } = true;
            public void Fire() { if (Active) { Active = false; callback(state); } }
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() => Active = false;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }

        public List<ITimer> Timers { get; } = [];
        public int Created => Timers.Count;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(s => callback(s), state);
            Timers.Add(timer);
            return timer;
        }

        public void FireLast() => ((ManualTimer)Timers[^1]).Fire();
    }

    [Fact]
    public void BirdenCokIstekTekEylemeIner()
    {
        var time = new ManualTimeProvider();
        var count = 0;
        using var debouncer = new RefreshDebouncer(() => count++, TimeSpan.FromMilliseconds(100), timeProvider: time);

        debouncer.Request();
        debouncer.Request();
        debouncer.Request();

        Assert.Equal(1, time.Created);
        Assert.Equal(0, count);

        time.FireLast();

        Assert.Equal(1, count);
    }

    [Fact]
    public void EylemSonrasiYeniIstekYeniZamanlayiciKurar()
    {
        var time = new ManualTimeProvider();
        var count = 0;
        using var debouncer = new RefreshDebouncer(() => count++, TimeSpan.FromMilliseconds(100), timeProvider: time);

        debouncer.Request();
        time.FireLast();
        debouncer.Request();
        time.FireLast();

        Assert.Equal(2, count);
        Assert.Equal(2, time.Created);
    }

    [Fact]
    public void CancelBekleyenIstegiIptalEder()
    {
        var time = new ManualTimeProvider();
        var count = 0;
        using var debouncer = new RefreshDebouncer(() => count++, TimeSpan.FromMilliseconds(100), timeProvider: time);

        debouncer.Request();
        debouncer.Cancel();
        time.FireLast();

        Assert.Equal(0, count);
        Assert.False(debouncer.IsPending);
    }

    [Fact]
    public void DisposeSonrasiEylemCalismaz()
    {
        var time = new ManualTimeProvider();
        var count = 0;
        var debouncer = new RefreshDebouncer(() => count++, TimeSpan.FromMilliseconds(100), timeProvider: time);

        debouncer.Request();
        debouncer.Dispose();
        time.FireLast();
        debouncer.Request();

        Assert.Equal(0, count);
        Assert.Equal(1, time.Created);
    }

    [Fact]
    public void PostVerilirseEylemOnunUzerindenCalisir()
    {
        var time = new ManualTimeProvider();
        var count = 0;
        Action? posted = null;
        using var debouncer = new RefreshDebouncer(() => count++, TimeSpan.FromMilliseconds(100), a => posted = a, time);

        debouncer.Request();
        time.FireLast();

        Assert.Equal(0, count);
        Assert.NotNull(posted);

        posted!();
        Assert.Equal(1, count);
    }
}
