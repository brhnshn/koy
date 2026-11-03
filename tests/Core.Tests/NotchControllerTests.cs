using Koy.Core.Notch;
using Xunit;

namespace Koy.Core.Tests;

public class NotchControllerTests
{
    [Fact]
    public void InitialState_IsCollapsed_AndPanelIsMusic()
    {
        using var controller = new NotchController();
        Assert.Equal(NotchState.Collapsed, controller.State);
        Assert.Equal(NotchPanel.Music, controller.CurrentPanel);
    }

    [Fact]
    public void Hover_TransitionsToPeek_AndLeaveReturnsToCollapsed()
    {
        using var controller = new NotchController();
        controller.OnMouseEnter();
        Assert.Equal(NotchState.Peek, controller.State);
        Assert.True(controller.IsMouseOver);

        controller.OnMouseLeave();
        Assert.Equal(NotchState.Collapsed, controller.State);
        Assert.False(controller.IsMouseOver);
    }

    [Fact]
    public void Click_FromCollapsed_Expands()
    {
        using var controller = new NotchController();
        controller.OnClick();
        Assert.Equal(NotchState.Expanded, controller.State);
    }

    [Fact]
    public void Click_FromExpanded_Collapses()
    {
        using var controller = new NotchController();
        controller.OnClick(); // to expanded
        Assert.Equal(NotchState.Expanded, controller.State);

        controller.OnClick(); // to collapsed
        Assert.Equal(NotchState.Collapsed, controller.State);
    }

    [Fact]
    public void TrackChange_AutoPeeks_AndTimesOutWhenMouseOutside()
    {
        using var controller = new NotchController();
        controller.OnTrackChanged();
        Assert.Equal(NotchState.Peek, controller.State);

        // Auto peek timeout fires while mouse is outside
        controller.OnAutoPeekTimeout();
        Assert.Equal(NotchState.Collapsed, controller.State);
    }

    [Fact]
    public void TrackChange_AutoPeeks_DoesNotCollapseIfMouseInside()
    {
        using var controller = new NotchController();
        controller.OnTrackChanged();
        controller.OnMouseEnter();

        controller.OnAutoPeekTimeout();
        Assert.Equal(NotchState.Peek, controller.State);
    }

    [Fact]
    public void Expanded_MouseLeaveTimeout_CollapsesWhenMouseOutside()
    {
        using var controller = new NotchController();
        controller.OnClick(); // Expanded
        controller.OnMouseLeave();

        controller.OnMouseLeaveTimeout();
        Assert.Equal(NotchState.Collapsed, controller.State);
    }

    [Fact]
    public void Expanded_MouseLeaveTimeout_DoesNotCollapseIfMouseReentered()
    {
        using var controller = new NotchController();
        controller.OnClick(); // Expanded
        controller.OnMouseLeave();
        controller.OnMouseEnter(); // Re-entered

        controller.OnMouseLeaveTimeout();
        Assert.Equal(NotchState.Expanded, controller.State);
    }

    [Fact]
    public void PanelCycling_WorksAcrossEnabledPanels()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Music, NotchPanel.ControlCenter, NotchPanel.Status]);

        Assert.Equal(NotchPanel.Music, controller.CurrentPanel);

        controller.NextPanel();
        Assert.Equal(NotchPanel.ControlCenter, controller.CurrentPanel);

        controller.NextPanel();
        Assert.Equal(NotchPanel.Status, controller.CurrentPanel);

        controller.NextPanel();
        Assert.Equal(NotchPanel.Music, controller.CurrentPanel);

        controller.PreviousPanel();
        Assert.Equal(NotchPanel.Status, controller.CurrentPanel);
    }

    [Fact]
    public void StateChanged_FiresCorrectly()
    {
        using var controller = new NotchController();
        NotchState? observed = null;
        controller.StateChanged += (_, s) => observed = s;

        controller.OnClick();
        Assert.Equal(NotchState.Expanded, observed);
    }

    [Fact]
    public void SetEnabledPanels_Empty_DefaultsToMusic()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([]);
        Assert.Single(controller.EnabledPanels);
        Assert.Equal(NotchPanel.Music, controller.EnabledPanels[0]);
    }

    [Fact]
    public void SetEnabledPanels_WhenCurrentRemoved_SwitchesToFirst()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Music, NotchPanel.Shelf]);
        controller.SelectPanel(NotchPanel.Shelf);
        Assert.Equal(NotchPanel.Shelf, controller.CurrentPanel);

        // Remove Shelf
        controller.SetEnabledPanels([NotchPanel.ControlCenter, NotchPanel.Status]);
        Assert.Equal(NotchPanel.ControlCenter, controller.CurrentPanel);
    }

    [Fact]
    public void NextPreviousPanel_WithSinglePanel_DoesNotChange()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Music]);
        controller.NextPanel();
        Assert.Equal(NotchPanel.Music, controller.CurrentPanel);
        controller.PreviousPanel();
        Assert.Equal(NotchPanel.Music, controller.CurrentPanel);
    }

    [Fact]
    public void TrackChange_WhenAlreadyExpanded_StaysExpanded()
    {
        using var controller = new NotchController();
        controller.OnClick(); // Expanded
        Assert.Equal(NotchState.Expanded, controller.State);

        controller.OnTrackChanged();
        Assert.Equal(NotchState.Expanded, controller.State);
    }

    [Fact]
    public void Click_FromPeek_Expands()
    {
        using var controller = new NotchController();
        controller.OnMouseEnter();
        Assert.Equal(NotchState.Peek, controller.State);

        controller.OnClick();
        Assert.Equal(NotchState.Expanded, controller.State);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var controller = new NotchController();
        controller.Dispose();
        controller.Dispose(); // second call should be completely safe
    }
}
