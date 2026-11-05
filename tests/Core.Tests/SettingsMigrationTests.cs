using Koy.Core.Display;
using Koy.Core.Settings;
using Xunit;

namespace Koy.Core.Tests;

public class SettingsMigrationTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), "koy-mig-" + Guid.NewGuid().ToString("N") + ".json");

    [Theory]
    [InlineData("Primary")]
    [InlineData("All")]
    [InlineData("")]
    public void Version2_primary_or_all_becomes_all_screens_open(string target)
    {
        var old = new AppSettings { Version = 2, MonitorTarget = target };

        var migrated = SettingsMigration.Migrate(old);

        Assert.Equal(AppSettings.CurrentVersion, migrated.Version);
        Assert.Empty(migrated.Monitors);
        Assert.Equal(NewScreenMode.NotchAndDock, migrated.NewScreenDefault);
    }

    [Fact]
    public void Version2_specific_monitor_keeps_only_that_screen_open()
    {
        var old = new AppSettings { Version = 2, MonitorTarget = @"\\.\DISPLAY2" };

        var migrated = SettingsMigration.Migrate(old);

        Assert.True(migrated.Monitors[@"\\.\DISPLAY2"].NotchEnabled);
        Assert.True(migrated.Monitors[@"\\.\DISPLAY2"].DockEnabled);
        Assert.Equal(NewScreenMode.None, migrated.NewScreenDefault);
    }

    [Fact]
    public void Current_version_is_untouched()
    {
        var current = new AppSettings { MonitorTarget = "Elle", NewScreenDefault = NewScreenMode.NotchOnly };

        Assert.Same(current, SettingsMigration.Migrate(current));
    }

    [Fact]
    public void Migration_preserves_other_settings()
    {
        var old = new AppSettings { Version = 2, Language = "en", DockIconSize = 55 };

        var migrated = SettingsMigration.Migrate(old);

        Assert.Equal("en", migrated.Language);
        Assert.Equal(55, migrated.DockIconSize);
    }

    [Fact]
    public void Store_migrates_a_version2_file_on_load()
    {
        var path = TempPath();
        try
        {
            File.WriteAllText(path, "{ \"Version\": 2, \"MonitorTarget\": \"\\\\\\\\.\\\\DISPLAY2\" }");

            var loaded = new SettingsStore(path).Load();

            Assert.Equal(AppSettings.CurrentVersion, loaded.Version);
            Assert.True(loaded.Monitors.ContainsKey(@"\\.\DISPLAY2"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Monitor_settings_round_trip_through_json_including_disconnected_ones()
    {
        var path = TempPath();
        try
        {
            var store = new SettingsStore(path);
            var settings = new AppSettings
            {
                NewScreenDefault = NewScreenMode.None,
                Monitors = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["kimlik-1"] = new MonitorSettings { NotchEnabled = true, DockEnabled = false, Name = "Ofis" },
                    ["kimlik-2"] = new MonitorSettings { NotchEnabled = false, DockEnabled = true }
                }
            };
            store.Save(settings);

            var loaded = store.Load();

            Assert.Equal(settings, loaded);
            Assert.Equal("Ofis", loaded.Monitors["kimlik-1"].Name);
            Assert.Equal(NewScreenMode.None, loaded.NewScreenDefault);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
