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

    [Fact]
    public void CreateReset_keeps_FirstRunCompleted_and_restores_defaults()
    {
        var current = new AppSettings
        {
            FirstRunCompleted = true,
            NotchEnabled = false,
            DockProximity = Koy.Core.Dock.DockProximityMode.Dodge,
            Language = "en"
        };

        var reset = FirstRunPolicy.CreateReset(current);

        Assert.True(reset.FirstRunCompleted);
        Assert.Equal(new AppSettings { FirstRunCompleted = true }, reset);
    }

    [Fact]
    public void CreateReset_does_not_mark_unfinished_setup_as_completed()
    {
        var reset = FirstRunPolicy.CreateReset(new AppSettings { FirstRunCompleted = false });

        Assert.False(reset.FirstRunCompleted);
    }
}
