using Koy.Core.Notch;
using Koy.Core.Settings;

namespace Koy.Core.Tests;

public sealed class NotchPanelsTests
{
    [Fact]
    public void DefaultSettings_EnableAllPanels_InTabOrder()
    {
        Assert.Equal(NotchPanels.Order, NotchPanels.ParseEnabled(new AppSettings().EnabledPanels));
    }

    [Fact]
    public void ParseEnabled_AlwaysIncludesHome()
    {
        Assert.Equal([NotchPanel.Home], NotchPanels.ParseEnabled(null));
        Assert.Equal([NotchPanel.Home], NotchPanels.ParseEnabled([]));
        Assert.Equal([NotchPanel.Home, NotchPanel.Shelf], NotchPanels.ParseEnabled(["shelf"]));
    }

    [Fact]
    public void ParseEnabled_IgnoresOrderInSettings()
    {
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.Focus, NotchPanel.Shelf, NotchPanel.ControlCenter],
            NotchPanels.ParseEnabled(["controlcenter", "shelf", "focus", "home"]));
    }

    [Fact]
    public void ParseEnabled_MigratesOldKeys()
    {
        // Eski varsayılan: music, controlcenter, status, calendar, shelf.
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.Focus, NotchPanel.Shelf, NotchPanel.ControlCenter],
            NotchPanels.ParseEnabled(["music", "controlcenter", "status", "calendar", "shelf"]));
    }

    [Fact]
    public void ParseEnabled_SkipsUnknownKeysAndIgnoresCase()
    {
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.ControlCenter],
            NotchPanels.ParseEnabled(["status", "clipboard", "", "ControlCenter"]));
    }

    [Fact]
    public void ToKey_RoundTrips()
    {
        foreach (var panel in NotchPanels.Order)
        {
            Assert.True(NotchPanels.TryParse(NotchPanels.ToKey(panel), out var parsed));
            Assert.Equal(panel, parsed);
        }
    }

    [Fact]
    public void Expand_ToDisabledPanel_KeepsTabOrder()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.ControlCenter]);

        controller.Expand(NotchPanel.Shelf);

        Assert.Equal([NotchPanel.Home, NotchPanel.Shelf, NotchPanel.ControlCenter], controller.EnabledPanels);
    }
}

public sealed class NotchPinTests
{
    [Fact]
    public void Pinned_Expanded_DoesNotCollapseOnMouseLeave()
    {
        var time = new StepTimeProvider();
        using var controller = new NotchController(time);
        controller.Expand();
        controller.OnMouseEnter();
        controller.SetPinned(true);

        controller.OnMouseLeave();
        time.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(NotchState.Expanded, controller.State);
        Assert.True(controller.IsPinned);
    }

    [Fact]
    public void Unpinning_WithMouseOutside_CollapsesAfterTheUsualDelay()
    {
        var time = new StepTimeProvider();
        using var controller = new NotchController(time);
        controller.Expand();
        controller.SetPinned(true);
        controller.OnMouseLeave();

        controller.SetPinned(false);
        time.Advance(TimeSpan.FromMilliseconds(500));

        Assert.Equal(NotchState.Collapsed, controller.State);
    }

    [Fact]
    public void Collapsing_ClearsPin()
    {
        using var controller = new NotchController();
        controller.Expand();
        controller.SetPinned(true);

        controller.OnClick(); // Expanded -> Collapsed

        Assert.Equal(NotchState.Collapsed, controller.State);
        Assert.False(controller.IsPinned);
    }

    [Fact]
    public void CannotPin_WhenNotExpanded()
    {
        using var controller = new NotchController();

        controller.TogglePinned();

        Assert.False(controller.IsPinned);
    }
}
