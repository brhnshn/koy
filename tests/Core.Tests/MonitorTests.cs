using Koy.Core.Display;
using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class MonitorTests
{
    private sealed class FakeMonitorService : IMonitorService
    {
        public List<DisplayMonitor> Monitors { get; set; } = [];
        public event EventHandler? MonitorsChanged;

        public IReadOnlyList<DisplayMonitor> GetMonitors() => Monitors;

        public DisplayMonitor? GetPrimaryMonitor() =>
            Monitors.FirstOrDefault(m => m.IsPrimary) ?? Monitors.FirstOrDefault();

        public DisplayMonitor? GetMonitor(string idOrName) =>
            Monitors.FirstOrDefault(m => string.Equals(m.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(m.Name, idOrName, StringComparison.OrdinalIgnoreCase));

        public void TriggerMonitorsChanged() => MonitorsChanged?.Invoke(this, EventArgs.Empty);

        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    [Fact]
    public void DisplayMonitor_CalculatesDipDimensionsCorrectly()
    {
        var monitor = new DisplayMonitor(
            Id: @"\\.\DISPLAY1",
            Name: "Built-in Display",
            Bounds: new WindowRect(0, 0, 3840, 2160),
            WorkArea: new WindowRect(0, 0, 3840, 2112),
            IsPrimary: true,
            DpiScaleX: 1.5,
            DpiScaleY: 1.5);

        Assert.Equal(2560, monitor.WidthDip, precision: 1);
        Assert.Equal(1440, monitor.HeightDip, precision: 1);
        Assert.Equal(0, monitor.LeftDip);
        Assert.Equal(0, monitor.TopDip);
        Assert.True(monitor.IsPrimary);
    }

    [Fact]
    public void DisplayMonitor_CalculatesSecondaryMonitorOffset()
    {
        var secondary = new DisplayMonitor(
            Id: @"\\.\DISPLAY2",
            Name: "External Monitor",
            Bounds: new WindowRect(1920, 0, 3840, 1080),
            WorkArea: new WindowRect(1920, 0, 3840, 1080),
            IsPrimary: false,
            DpiScaleX: 1.0,
            DpiScaleY: 1.0);

        Assert.Equal(1920, secondary.LeftDip);
        Assert.Equal(0, secondary.TopDip);
        Assert.Equal(1920, secondary.WidthDip);
        Assert.Equal(1080, secondary.HeightDip);
        Assert.False(secondary.IsPrimary);
    }

    [Fact]
    public void GetPrimaryMonitor_ReturnsPrimaryDisplay()
    {
        var service = new FakeMonitorService
        {
            Monitors =
            [
                new DisplayMonitor("D2", "Ext", new WindowRect(1920, 0, 3840, 1080), WindowRect.Empty, false),
                new DisplayMonitor("D1", "Primary", new WindowRect(0, 0, 1920, 1080), WindowRect.Empty, true)
            ]
        };

        var primary = service.GetPrimaryMonitor();

        Assert.NotNull(primary);
        Assert.Equal("D1", primary.Id);
        Assert.True(primary.IsPrimary);
    }

    [Fact]
    public void GetMonitor_FindsByIdOrName()
    {
        var service = new FakeMonitorService
        {
            Monitors =
            [
                new DisplayMonitor("MON1", "LG UltraWide", new WindowRect(0, 0, 2560, 1080), WindowRect.Empty, true)
            ]
        };

        Assert.NotNull(service.GetMonitor("MON1"));
        Assert.NotNull(service.GetMonitor("LG UltraWide"));
        Assert.Null(service.GetMonitor("Unknown"));
    }
}
