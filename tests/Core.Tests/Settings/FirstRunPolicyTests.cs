using Koy.Core.Settings;

namespace Koy.Core.Tests.Settings;

public class FirstRunPolicyTests
{
    [Fact]
    public void SihirbazTamamlanmadiysaDockKapaliKalir()
    {
        var settings = new AppSettings(); // FirstRunCompleted = false, DockEnabled varsayılan true

        var result = FirstRunPolicy.ApplySafeDefaultsIfIncomplete(settings);

        Assert.False(result.DockEnabled);
        Assert.False(result.HideWindowsTaskbar);
        Assert.True(result.NotchEnabled);
    }

    [Fact]
    public void SihirbazTamamlandiysaAyarlarDegismez()
    {
        var settings = new AppSettings { FirstRunCompleted = true, DockEnabled = true, HideWindowsTaskbar = true };

        var result = FirstRunPolicy.ApplySafeDefaultsIfIncomplete(settings);

        Assert.Same(settings, result);
    }

    [Fact]
    public void OrijinalAyarNesnesiniDegistirmez()
    {
        var settings = new AppSettings();

        FirstRunPolicy.ApplySafeDefaultsIfIncomplete(settings);

        Assert.True(settings.DockEnabled);
    }
}
