using Koy.Core.Display;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class MonitorPolicyTests
{
    private const string IdMain = @"\\?\DISPLAY#GSM5B09#5&1a2b&0&UID4352#{e6f07b5f}";
    private const string IdSide = @"\\?\DISPLAY#DEL4161#5&1a2b&0&UID4353#{e6f07b5f}";

    private static DisplayMonitor MainMon(string deviceName = @"\\.\DISPLAY1", int width = 1920) => new(
        IdMain, "LG ULTRAGEAR", new WindowRect(0, 0, width, 1080), new WindowRect(0, 0, width, 1040),
        IsPrimary: true, DpiScaleX: 1.0, DpiScaleY: 1.0, Handle: 1, DeviceName: deviceName);

    private static DisplayMonitor Side(string deviceName = @"\\.\DISPLAY2") => new(
        IdSide, "DELL U2415", new WindowRect(1920, 0, 3840, 1200), new WindowRect(1920, 0, 3840, 1160),
        IsPrimary: false, DpiScaleX: 1.25, DpiScaleY: 1.25, Handle: 2, DeviceName: deviceName);

    [Fact]
    public void Unknown_monitor_gets_new_screen_default()
    {
        var result = MonitorPolicy.Resolve(null, NewScreenMode.NotchOnly, [MainMon(), Side()]);

        Assert.All(result, a => Assert.False(a.IsKnown));
        Assert.All(result, a => Assert.True(a.NotchEnabled));
        Assert.All(result, a => Assert.False(a.DockEnabled));
    }

    [Theory]
    [InlineData(NewScreenMode.NotchAndDock, true, true)]
    [InlineData(NewScreenMode.NotchOnly, true, false)]
    [InlineData(NewScreenMode.None, false, false)]
    public void New_screen_default_maps_to_notch_and_dock(NewScreenMode mode, bool notch, bool dock)
    {
        var s = MonitorPolicy.DefaultFor(mode);
        Assert.Equal(notch, s.NotchEnabled);
        Assert.Equal(dock, s.DockEnabled);
    }

    [Fact]
    public void Saved_settings_apply_per_monitor()
    {
        var saved = new Dictionary<string, MonitorSettings>
        {
            [IdMain] = new() { NotchEnabled = true, DockEnabled = true },
            [IdSide] = new() { NotchEnabled = true, DockEnabled = false }
        };

        var result = MonitorPolicy.Resolve(saved, NewScreenMode.None, [MainMon(), Side()]);

        Assert.True(result[0].DockEnabled);
        Assert.True(result[1].NotchEnabled);
        Assert.False(result[1].DockEnabled);
        Assert.All(result, a => Assert.True(a.IsKnown));
        Assert.Equal([MainMon().Id], MonitorPolicy.DockMonitors(result).Select(m => m.Id));
        Assert.Equal(2, MonitorPolicy.NotchMonitors(result).Count);
    }

    [Fact]
    public void Identity_survives_resolution_position_and_device_name_changes()
    {
        var saved = new Dictionary<string, MonitorSettings>
        {
            [IdSide] = new() { NotchEnabled = false, DockEnabled = true }
        };

        // Aygıt adı DISPLAY2 -> DISPLAY3, çözünürlük ve konum da değişti: ayar yine bu ekrana gider.
        var moved = Side(@"\\.\DISPLAY3") with { Bounds = new WindowRect(-2560, 0, 0, 1440) };
        var result = MonitorPolicy.Resolve(saved, NewScreenMode.NotchAndDock, [MainMon(), moved]);

        Assert.False(result[1].NotchEnabled);
        Assert.True(result[1].IsKnown);
        Assert.True(result[0].NotchEnabled);
        Assert.False(result[0].IsKnown);
    }

    [Fact]
    public void Legacy_device_name_key_matches_until_rewritten_with_stable_id()
    {
        var saved = new Dictionary<string, MonitorSettings>
        {
            [@"\\.\display2"] = new() { NotchEnabled = true, DockEnabled = false }
        };

        var result = MonitorPolicy.Resolve(saved, NewScreenMode.None, [MainMon(), Side()]);
        Assert.True(result[1].IsKnown);
        Assert.False(result[1].DockEnabled);

        var updated = MonitorPolicy.Update(saved, NewScreenMode.None, Side(), s => s with { DockEnabled = true });
        Assert.False(updated.ContainsKey(@"\\.\display2"));
        Assert.True(updated[IdSide].DockEnabled);
        Assert.True(updated[IdSide].NotchEnabled);
        Assert.Equal("DELL U2415", updated[IdSide].Name);
    }

    [Fact]
    public void Update_keeps_settings_of_disconnected_monitors_and_does_not_mutate_input()
    {
        var saved = new Dictionary<string, MonitorSettings>
        {
            ["ayrilmis-ekran"] = new() { NotchEnabled = false, DockEnabled = true, Name = "Eski TV" }
        };

        var updated = MonitorPolicy.Update(saved, NewScreenMode.NotchOnly, MainMon(), s => s with { NotchEnabled = false });

        Assert.Single(saved);
        Assert.Equal(2, updated.Count);
        Assert.Equal("Eski TV", updated["ayrilmis-ekran"].Name);
        // Kayıtsız ekranın düzenlemesi yeni ekran varsayılanından (yalnız çentik) başlar.
        Assert.False(updated[IdMain].NotchEnabled);
        Assert.False(updated[IdMain].DockEnabled);
    }

    [Fact]
    public void Same_model_on_two_ports_is_two_different_monitors()
    {
        var twin = Side() with { Id = @"\\?\DISPLAY#GSM5B09#5&1a2b&0&UID4999#{e6f07b5f}", Name = "LG ULTRAGEAR" };
        var saved = new Dictionary<string, MonitorSettings> { [IdMain] = new() { DockEnabled = false } };

        var result = MonitorPolicy.Resolve(saved, NewScreenMode.NotchAndDock, [MainMon(), twin]);

        Assert.False(result[0].DockEnabled);
        Assert.True(result[1].DockEnabled);
    }
}
