using Koy.Core.Settings;

namespace Koy.Core.Tests.Settings;

public sealed class SetupWizardLogicTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "koy-wizard-tests-" + Guid.NewGuid());

    private SettingsStore NewStore() => new(Path.Combine(_dir, "settings.json"));

    [Fact]
    public void AppSettings_Default_FirstRunCompleted_IsFalse()
    {
        var settings = new AppSettings();
        Assert.False(settings.FirstRunCompleted);
        Assert.Equal("Dark", settings.Theme);
        Assert.Equal(18.0, settings.CornerRadius);
        Assert.Equal("Normal", settings.AnimationSpeed);
        Assert.True(settings.RespectReduceMotion);
        Assert.True(settings.HideWindowsTaskbar);
        Assert.True(settings.AutoCheckUpdates);
        Assert.Equal("Kararlı", settings.UpdateChannel);
    }

    [Theory]
    [InlineData(0, true, true)]   // Çentik ve dock
    [InlineData(1, true, false)]  // Yalnız çentik
    [InlineData(2, false, true)]  // Yalnız dock
    public void Wizard_CardSelection_ConfiguresFeaturesCorrectly(int cardIndex, bool expectedNotch, bool expectedDock)
    {
        var store = NewStore();
        var initial = store.Load();
        Assert.False(initial.FirstRunCompleted);

        var notchEnabled = cardIndex is 0 or 1;
        var dockEnabled = cardIndex is 0 or 2;

        var configured = initial with
        {
            FirstRunCompleted = true,
            Language = "tr",
            NotchEnabled = notchEnabled,
            DockEnabled = dockEnabled,
            HideWindowsTaskbar = dockEnabled
        };

        store.Save(configured);

        var loaded = store.Load();
        Assert.True(loaded.FirstRunCompleted);
        Assert.Equal(expectedNotch, loaded.NotchEnabled);
        Assert.Equal(expectedDock, loaded.DockEnabled);
        Assert.Equal(dockEnabled, loaded.HideWindowsTaskbar);
    }

    [Fact]
    public void AppSettings_Equality_ChecksAllNewProperties()
    {
        var s1 = new AppSettings();
        var s2 = new AppSettings();

        Assert.Equal(s1, s2);
        Assert.Equal(s1.GetHashCode(), s2.GetHashCode());

        var modifiedFirstRun = s1 with { FirstRunCompleted = true };
        Assert.NotEqual(s1, modifiedFirstRun);

        var modifiedTheme = s1 with { Theme = "Açık" };
        Assert.NotEqual(s1, modifiedTheme);

        var modifiedRadius = s1 with { CornerRadius = 12.0 };
        Assert.NotEqual(s1, modifiedRadius);

        var modifiedAnim = s1 with { AnimationSpeed = "Hızlı" };
        Assert.NotEqual(s1, modifiedAnim);

        var modifiedMotion = s1 with { RespectReduceMotion = false };
        Assert.NotEqual(s1, modifiedMotion);

        var modifiedTaskbar = s1 with { HideWindowsTaskbar = false };
        Assert.NotEqual(s1, modifiedTaskbar);

        var modifiedUpdates = s1 with { AutoCheckUpdates = false };
        Assert.NotEqual(s1, modifiedUpdates);

        var modifiedChannel = s1 with { UpdateChannel = "Önizleme" };
        Assert.NotEqual(s1, modifiedChannel);

        var modifiedSaverMotion = s1 with { BatterySaverReduceMotion = false };
        Assert.NotEqual(s1, modifiedSaverMotion);

        var modifiedSaverWave = s1 with { BatterySaverStopWaveform = false };
        Assert.NotEqual(s1, modifiedSaverWave);

        var modifiedVisualizer = s1 with { AudioVisualizerEnabled = true };
        Assert.NotEqual(s1, modifiedVisualizer);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}
