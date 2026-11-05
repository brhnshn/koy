using Koy.Core.Settings;
using Koy.Core.Visibility;

namespace Koy.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "koy-tests-" + Guid.NewGuid());

    private SettingsStore NewStore() => new(Path.Combine(_dir, "settings.json"));

    [Fact]
    public void Missing_file_returns_defaults()
    {
        Assert.Equal(new AppSettings(), NewStore().Load());
    }

    [Fact]
    public void Round_trips()
    {
        var store = NewStore();
        var settings = new AppSettings { Language = "en", DockVisibility = VisibilityMode.Peek, AccentColor = "#FF8800" };

        store.Save(settings);

        Assert.Equal(settings, store.Load());
    }

    [Fact]
    public void Partial_file_keeps_defaults_for_missing_fields()
    {
        // Eski ya da elle yazılmış dosyada olmayan alanlar varsayılanını korumalı (NotchEnabled false olursa çentik hiç açılmaz).
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "settings.json"), """{ "DockEnabled": true }""");

        var loaded = NewStore().Load();

        Assert.Equal(new AppSettings { DockEnabled = true }, loaded);
    }

    [Fact]
    public void Corrupt_file_returns_defaults()
    {
        var store = NewStore();
        Directory.CreateDirectory(_dir);
        File.WriteAllText(store.Path, "{ bozuk");

        Assert.Equal(new AppSettings(), store.Load());
    }

    [Fact]
    public void Enums_are_written_as_text()
    {
        var store = NewStore();
        store.Save(new AppSettings { DockVisibility = VisibilityMode.Smart });

        Assert.Contains("\"Smart\"", File.ReadAllText(store.Path));
    }

    [Fact]
    public void Round_trips_with_custom_panels()
    {
        var store = NewStore();
        var settings = new AppSettings
        {
            Language = "en",
            EnabledPanels = ["music", "calendar"],
            ControlCenterLayout = ["bluetooth", "wifi"]
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings, loaded);
        Assert.Equal(["music", "calendar"], loaded.EnabledPanels);
    }

    [Fact]
    public void Round_trips_with_dock_and_monitor_settings()
    {
        var store = NewStore();
        var settings = new AppSettings
        {
            DockEnabled = true,
            DockVisibility = VisibilityMode.Fixed,
            DockHoverMagnify = false,
            DockHideInFullscreen = false,
            MonitorTarget = "All"
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings, loaded);
        Assert.True(loaded.DockEnabled);
        Assert.Equal(VisibilityMode.Fixed, loaded.DockVisibility);
        Assert.False(loaded.DockHoverMagnify);
        Assert.False(loaded.DockHideInFullscreen);
        Assert.Equal("All", loaded.MonitorTarget);
    }

    [Fact]
    public void Equals_HandlesNullListsGracefully()
    {
        var s1 = new AppSettings { ControlCenterLayout = null!, EnabledPanels = null! };
        var s2 = new AppSettings();

        Assert.False(s1.Equals(s2));
        Assert.False(s2.Equals(s1));
        Assert.True(s1.Equals(s1));
    }

    [Fact]
    public void Round_trips_with_first_run_completed_and_theme()
    {
        var store = NewStore();
        var settings = new AppSettings
        {
            FirstRunCompleted = true,
            Theme = "Light",
            CornerRadius = 14.0,
            AnimationSpeed = "Fast"
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings, loaded);
        Assert.True(loaded.FirstRunCompleted);
        Assert.Equal("Light", loaded.Theme);
        Assert.Equal(14.0, loaded.CornerRadius);
    }

    [Fact]
    public void Round_trips_with_new_ui_settings()
    {
        var store = NewStore();
        var settings = new AppSettings
        {
            DockIconSize = 24.0,
            IdleDisplayMode = 1,
            NotchOpenBehavior = 0,
            Material = "Translucent",
            WheelAction = 1,
            MiddleClickAction = 1,
            DockIndicatorStyle = 1
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings, loaded);
        Assert.Equal(24.0, loaded.DockIconSize);
        Assert.Equal(1, loaded.IdleDisplayMode);
        Assert.Equal(0, loaded.NotchOpenBehavior);
        Assert.Equal("Translucent", loaded.Material);
        Assert.Equal(1, loaded.WheelAction);
        Assert.Equal(1, loaded.MiddleClickAction);
        Assert.Equal(1, loaded.DockIndicatorStyle);
    }

    [Fact]
    public void DockDown_migrates_to_Dodge()
    {
        var store = NewStore();
        Directory.CreateDirectory(_dir);
        File.WriteAllText(store.Path, """{ "DockProximity": "DockDown" }""");

        var loaded = store.Load();

        Assert.Equal(Koy.Core.Dock.DockProximityMode.Dodge, loaded.DockProximity);
        Assert.Contains("\"Dodge\"", File.ReadAllText(store.Path));
    }

    [Fact]
    public void DockShowTray_DefaultsOnForOldFilesAndRoundTripsOff()
    {
        // Tepsi alanı olmayan eski dosyada tepsi açık gelmeli; kapatılınca kapalı kalmalı.
        var store = NewStore();
        Directory.CreateDirectory(_dir);
        File.WriteAllText(store.Path, """{ "DockEnabled": true }""");
        Assert.True(store.Load().DockShowTray);

        store.Save(new AppSettings { DockShowTray = false });
        var loaded = store.Load();

        Assert.False(loaded.DockShowTray);
        Assert.NotEqual(new AppSettings(), loaded);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}
