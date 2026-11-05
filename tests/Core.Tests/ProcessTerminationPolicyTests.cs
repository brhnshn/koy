using Koy.Core.Shell;

namespace Koy.Core.Tests;

public class ProcessTerminationPolicyTests
{
    [Theory]
    [InlineData("chrome")]
    [InlineData("notepad.exe")]
    [InlineData("Spotify")]
    public void SradanUygulamaSonlandirilabilir(string name)
    {
        Assert.True(ProcessTerminationPolicy.CanTerminate(1234, name, ownProcessId: 1));
    }

    [Theory]
    [InlineData("ApplicationFrameHost")]
    [InlineData("applicationframehost.exe")]
    [InlineData("explorer")]
    [InlineData("dwm")]
    public void OrtakVeKabukSurecleriSonlandirilmaz(string name)
    {
        Assert.False(ProcessTerminationPolicy.CanTerminate(1234, name, ownProcessId: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void GecersizPidSonlandirilmaz(int pid)
    {
        Assert.False(ProcessTerminationPolicy.CanTerminate(pid, "chrome", ownProcessId: 1));
    }

    [Fact]
    public void KendiSurecimizSonlandirilmaz()
    {
        Assert.False(ProcessTerminationPolicy.CanTerminate(99, "Koy", ownProcessId: 99));
    }

    [Fact]
    public void BilinmeyenAdSonlandirilmaz()
    {
        Assert.False(ProcessTerminationPolicy.CanTerminate(1234, null, ownProcessId: 1));
        Assert.False(ProcessTerminationPolicy.CanTerminate(1234, " ", ownProcessId: 1));
    }
}
