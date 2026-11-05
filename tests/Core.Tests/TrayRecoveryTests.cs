using Koy.Core.Tray;
using Xunit;

namespace Koy.Core.Tests;

public class TrayHostRecoveryTests
{
    private static readonly DateTime T0 = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Acquires_when_wanted_and_missing() =>
        Assert.Equal(TrayHostAction.Acquire, new TrayHostRecovery().Decide(true, false, false, T0));

    [Fact]
    public void Keeps_running_host() =>
        Assert.Equal(TrayHostAction.None, new TrayHostRecovery().Decide(true, true, true, T0));

    [Fact]
    public void Reacquires_dead_host() =>
        Assert.Equal(TrayHostAction.Reacquire, new TrayHostRecovery().Decide(true, true, false, T0));

    [Fact]
    public void Releases_when_no_longer_wanted()
    {
        var r = new TrayHostRecovery();
        Assert.Equal(TrayHostAction.Release, r.Decide(false, true, true, T0));
        Assert.Equal(TrayHostAction.None, r.Decide(false, false, false, T0));
    }

    [Fact]
    public void Failed_acquire_is_retried_after_backoff_not_blocked_forever()
    {
        var r = new TrayHostRecovery();
        r.ReportAcquire(false, T0);

        Assert.Equal(TrayHostAction.None, r.Decide(true, false, false, T0.AddSeconds(1)));
        Assert.Equal(TrayHostAction.Acquire, r.Decide(true, false, false, T0.Add(TrayHostRecovery.FirstRetryDelay)));
    }

    [Fact]
    public void Dead_host_is_released_but_not_reacquired_during_backoff()
    {
        var r = new TrayHostRecovery();
        r.ReportAcquire(false, T0);
        Assert.Equal(TrayHostAction.Release, r.Decide(true, true, false, T0.AddSeconds(1)));
    }

    [Fact]
    public void Backoff_grows_and_is_capped()
    {
        var r = new TrayHostRecovery();
        for (var i = 0; i < 20; i++)
            r.ReportAcquire(false, T0);

        Assert.Equal(TrayHostAction.None, r.Decide(true, false, false, T0.Add(TrayHostRecovery.MaxRetryDelay).AddSeconds(-1)));
        Assert.Equal(TrayHostAction.Acquire, r.Decide(true, false, false, T0.Add(TrayHostRecovery.MaxRetryDelay)));
    }

    [Fact]
    public void Success_and_reset_clear_backoff()
    {
        var r = new TrayHostRecovery();
        r.ReportAcquire(false, T0);
        r.ReportAcquire(true, T0);
        Assert.Equal(0, r.Failures);
        Assert.Equal(TrayHostAction.Acquire, r.Decide(true, false, false, T0));

        r.ReportAcquire(false, T0);
        r.ResetFailures();
        Assert.Equal(TrayHostAction.Acquire, r.Decide(true, false, false, T0));
    }

    [Fact]
    public void Not_wanted_clears_backoff()
    {
        var r = new TrayHostRecovery();
        r.ReportAcquire(false, T0);
        r.Decide(false, false, false, T0);
        Assert.Equal(TrayHostAction.Acquire, r.Decide(true, false, false, T0));
    }
}

public class TrayHostLeaseTests
{
    [Fact]
    public void Last_release_signals_close()
    {
        var l = new TrayHostLease();
        l.Acquire();
        l.Acquire();
        Assert.False(l.Release());
        Assert.True(l.Release());
        Assert.Equal(0, l.Users);
    }

    [Fact]
    public void Extra_release_never_goes_negative()
    {
        var l = new TrayHostLease();
        Assert.False(l.Release());
        l.Acquire();
        Assert.Equal(1, l.Users);
    }

    [Fact]
    public void Reset_clears_users()
    {
        var l = new TrayHostLease();
        l.Acquire();
        l.Reset();
        Assert.Equal(0, l.Users);
    }
}
