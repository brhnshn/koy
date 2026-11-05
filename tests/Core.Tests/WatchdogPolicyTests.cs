using Koy.Core.Shell;

namespace Koy.Core.Tests;

public class WatchdogPolicyTests
{
    [Fact]
    public void Decide_KoyOlurseVeyaYoksaGorevCubuguGeriGelir()
    {
        Assert.Equal(WatchdogAction.RestoreTaskbar, WatchdogPolicy.Decide(WatchdogWaitResult.ProcessExited));
        Assert.Equal(WatchdogAction.RestoreTaskbar, WatchdogPolicy.Decide(WatchdogWaitResult.ProcessNotFound));
    }

    [Fact]
    public void Decide_TemizCikistaVeHatadaDokunmaz()
    {
        Assert.Equal(WatchdogAction.None, WatchdogPolicy.Decide(WatchdogWaitResult.StopRequested));
        Assert.Equal(WatchdogAction.None, WatchdogPolicy.Decide(WatchdogWaitResult.Failed));
    }

    [Fact]
    public void TryParseProcessId_GecerliArguman()
    {
        Assert.True(WatchdogPolicy.TryParseProcessId(["--pid", "4242"], out var pid));
        Assert.Equal(4242, pid);
    }

    [Fact]
    public void TryParseProcessId_BuyukKucukHarfVeBaskaArgumanlariTolereEder()
    {
        Assert.True(WatchdogPolicy.TryParseProcessId(["--x", "--PID", "17"], out var pid));
        Assert.Equal(17, pid);
    }

    [Theory]
    [InlineData("--pid")]
    [InlineData("--pid", "abc")]
    [InlineData("--pid", "0")]
    [InlineData("--pid", "-5")]
    [InlineData("4242")]
    public void TryParseProcessId_GecersizArgumanFalseDoner(params string[] args)
    {
        Assert.False(WatchdogPolicy.TryParseProcessId(args, out var pid));
        Assert.Equal(0, pid);
    }

    [Fact]
    public void TryParseProcessId_ArgumansizFalseDoner()
    {
        Assert.False(WatchdogPolicy.TryParseProcessId([], out _));
    }

    [Fact]
    public void BuildArguments_TryParseIleUyumlu()
    {
        var args = WatchdogPolicy.BuildArguments(321).Split(' ');

        Assert.True(WatchdogPolicy.TryParseProcessId(args, out var pid));
        Assert.Equal(321, pid);
    }

    [Fact]
    public void StopEventName_PidyeOzel()
    {
        Assert.Equal(@"Local\Koy.Watchdog.Stop.99", WatchdogPolicy.StopEventName(99));
        Assert.NotEqual(WatchdogPolicy.StopEventName(1), WatchdogPolicy.StopEventName(2));
    }
}
