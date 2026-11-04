using Koy.Core.Display;
using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class DisplayLayoutResolverTests
{
    private static readonly DisplayMonitor PrimaryMon = new(
        Id: @"\\.\DISPLAY1",
        Name: "Main Display",
        Bounds: new WindowRect(0, 0, 1920, 1080),
        WorkArea: new WindowRect(0, 0, 1920, 1040),
        IsPrimary: true,
        DpiScaleX: 1.0,
        DpiScaleY: 1.0,
        Handle: 1001);

    private static readonly DisplayMonitor SecondaryMon = new(
        Id: @"\\.\DISPLAY2",
        Name: "Side Monitor",
        Bounds: new WindowRect(1920, 0, 3840, 1080),
        WorkArea: new WindowRect(1920, 0, 3840, 1080),
        IsPrimary: false,
        DpiScaleX: 1.25,
        DpiScaleY: 1.25,
        Handle: 1002);

    private static readonly DisplayMonitor ThirdMon = new(
        Id: @"\\.\DISPLAY3",
        Name: "Top Display",
        Bounds: new WindowRect(0, -1080, 1920, 0),
        WorkArea: new WindowRect(0, -1080, 1920, 0),
        IsPrimary: false,
        DpiScaleX: 1.0,
        DpiScaleY: 1.0,
        Handle: 1003);

    [Fact]
    public void ResolveTargetMonitors_EmptyList_ReturnsEmpty()
    {
        var resultPrimary = DisplayLayoutResolver.ResolveTargetMonitors([], "Primary", MonitorTargetMode.Primary);
        Assert.Empty(resultPrimary);

        var resultAll = DisplayLayoutResolver.ResolveTargetMonitors([], "All", MonitorTargetMode.All);
        Assert.Empty(resultAll);

        var resultSpecific = DisplayLayoutResolver.ResolveTargetMonitors([], "DISPLAY1", MonitorTargetMode.Specific);
        Assert.Empty(resultSpecific);
    }

    [Fact]
    public void ResolveTargetMonitors_PrimaryMode_ReturnsPrimary()
    {
        var monitors = new[] { SecondaryMon, PrimaryMon, ThirdMon };
        var resolved = DisplayLayoutResolver.ResolveTargetMonitors(monitors, "Primary", MonitorTargetMode.Primary);

        Assert.Single(resolved);
        Assert.Equal(@"\\.\DISPLAY1", resolved[0].Id);
        Assert.True(resolved[0].IsPrimary);
        Assert.Equal((nint)1001, resolved[0].Handle);
    }

    [Fact]
    public void ResolveTargetMonitors_PrimaryMode_NoPrimaryMarked_ReturnsFirst()
    {
        var noPrimary1 = SecondaryMon with { IsPrimary = false };
        var noPrimary2 = ThirdMon with { IsPrimary = false };
        var monitors = new[] { noPrimary1, noPrimary2 };

        var resolved = DisplayLayoutResolver.ResolveTargetMonitors(monitors, "Primary", MonitorTargetMode.Primary);

        Assert.Single(resolved);
        Assert.Equal(@"\\.\DISPLAY2", resolved[0].Id);
    }

    [Fact]
    public void ResolveTargetMonitors_AllMode_ReturnsAllMonitors()
    {
        var monitors = new[] { PrimaryMon, SecondaryMon, ThirdMon };
        var resolved = DisplayLayoutResolver.ResolveTargetMonitors(monitors, "All", MonitorTargetMode.All);

        Assert.Equal(3, resolved.Count);
        Assert.Contains(PrimaryMon, resolved);
        Assert.Contains(SecondaryMon, resolved);
        Assert.Contains(ThirdMon, resolved);
    }

    [Fact]
    public void ResolveTargetMonitors_SpecificMode_MatchesId_ReturnsSpecificMonitor()
    {
        var monitors = new[] { PrimaryMon, SecondaryMon, ThirdMon };
        var resolved = DisplayLayoutResolver.ResolveTargetMonitors(monitors, @"\\.\DISPLAY2", MonitorTargetMode.Specific);

        Assert.Single(resolved);
        Assert.Equal(@"\\.\DISPLAY2", resolved[0].Id);
        Assert.Equal((nint)1002, resolved[0].Handle);
    }

    [Fact]
    public void ResolveTargetMonitors_SpecificMode_MatchesName_ReturnsSpecificMonitor()
    {
        var monitors = new[] { PrimaryMon, SecondaryMon, ThirdMon };
        var resolved = DisplayLayoutResolver.ResolveTargetMonitors(monitors, "Side Monitor", MonitorTargetMode.Specific);

        Assert.Single(resolved);
        Assert.Equal(@"\\.\DISPLAY2", resolved[0].Id);
    }

    [Fact]
    public void ResolveTargetMonitors_SpecificMode_CaseInsensitiveMatch()
    {
        var monitors = new[] { PrimaryMon, SecondaryMon };
        var resolved = DisplayLayoutResolver.ResolveTargetMonitors(monitors, @"\\.\display2", MonitorTargetMode.Specific);

        Assert.Single(resolved);
        Assert.Equal(@"\\.\DISPLAY2", resolved[0].Id);
    }

    [Fact]
    public void ResolveTargetMonitors_SpecificMode_NotFound_FallsBackToPrimary()
    {
        var monitors = new[] { SecondaryMon, PrimaryMon };
        var resolved = DisplayLayoutResolver.ResolveTargetMonitors(monitors, "NonExistentDisplay", MonitorTargetMode.Specific);

        Assert.Single(resolved);
        Assert.Equal(@"\\.\DISPLAY1", resolved[0].Id);
        Assert.True(resolved[0].IsPrimary);
    }

    [Fact]
    public void ResolveTargetMonitors_SpecificMode_NotFoundAndNoPrimary_FallsBackToFirst()
    {
        var noPrimary1 = SecondaryMon with { IsPrimary = false };
        var noPrimary2 = ThirdMon with { IsPrimary = false };
        var monitors = new[] { noPrimary1, noPrimary2 };

        var resolved = DisplayLayoutResolver.ResolveTargetMonitors(monitors, "Unknown", MonitorTargetMode.Specific);

        Assert.Single(resolved);
        Assert.Equal(@"\\.\DISPLAY2", resolved[0].Id);
    }

    [Theory]
    [InlineData("All", MonitorTargetMode.All)]
    [InlineData("all", MonitorTargetMode.All)]
    [InlineData("ALL", MonitorTargetMode.All)]
    [InlineData("Primary", MonitorTargetMode.Primary)]
    [InlineData("primary", MonitorTargetMode.Primary)]
    [InlineData("", MonitorTargetMode.Primary)]
    [InlineData(null, MonitorTargetMode.Primary)]
    [InlineData("   ", MonitorTargetMode.Primary)]
    [InlineData(@"\\.\DISPLAY1", MonitorTargetMode.Specific)]
    [InlineData("HDMI-1", MonitorTargetMode.Specific)]
    public void ParseMode_ParsesSettingStringCorrectly(string? input, MonitorTargetMode expected)
    {
        var mode = DisplayLayoutResolver.ParseMode(input);
        Assert.Equal(expected, mode);
    }

    [Fact]
    public void ResolveTargetMonitors_WithStringOverload_ResolvesCorrectly()
    {
        var monitors = new[] { PrimaryMon, SecondaryMon };

        var all = DisplayLayoutResolver.ResolveTargetMonitors(monitors, "All");
        Assert.Equal(2, all.Count);

        var primary = DisplayLayoutResolver.ResolveTargetMonitors(monitors, "Primary");
        Assert.Single(primary);
        Assert.Equal(@"\\.\DISPLAY1", primary[0].Id);

        var specific = DisplayLayoutResolver.ResolveTargetMonitors(monitors, @"\\.\DISPLAY2");
        Assert.Single(specific);
        Assert.Equal(@"\\.\DISPLAY2", specific[0].Id);
    }

    [Fact]
    public void DisplayMonitor_Handle_PreservedAndDefaultsToZero()
    {
        var defaultMon = new DisplayMonitor("D0", "Default", WindowRect.Empty, WindowRect.Empty, true);
        Assert.Equal((nint)0, defaultMon.Handle);

        var withHandle = defaultMon with { Handle = 0x12345 };
        Assert.Equal((nint)0x12345, withHandle.Handle);
    }
}
