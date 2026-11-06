using Koy.Core.Notch;
using Xunit;

namespace Koy.Core.Tests;

public class NotchControllerTests
{
    [Fact]
    public void InitialState_IsCollapsed_AndPanelIsHome()
    {
        using var controller = new NotchController();
        Assert.Equal(NotchState.Collapsed, controller.State);
        Assert.Equal(NotchPanel.Home, controller.CurrentPanel);
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
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.ControlCenter, NotchPanel.Focus]);

        Assert.Equal(NotchPanel.Home, controller.CurrentPanel);

        controller.NextPanel();
        Assert.Equal(NotchPanel.ControlCenter, controller.CurrentPanel);

        controller.NextPanel();
        Assert.Equal(NotchPanel.Focus, controller.CurrentPanel);

        controller.NextPanel();
        Assert.Equal(NotchPanel.Home, controller.CurrentPanel);

        controller.PreviousPanel();
        Assert.Equal(NotchPanel.Focus, controller.CurrentPanel);
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
    public void SetEnabledPanels_Empty_DefaultsToHome()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([]);
        Assert.Single(controller.EnabledPanels);
        Assert.Equal(NotchPanel.Home, controller.EnabledPanels[0]);
    }

    [Fact]
    public void SetEnabledPanels_WhenCurrentRemoved_SwitchesToFirst()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.Shelf]);
        controller.SelectPanel(NotchPanel.Shelf);
        Assert.Equal(NotchPanel.Shelf, controller.CurrentPanel);

        // Remove Shelf
        controller.SetEnabledPanels([NotchPanel.ControlCenter, NotchPanel.Focus]);
        Assert.Equal(NotchPanel.ControlCenter, controller.CurrentPanel);
    }

    [Fact]
    public void NextPreviousPanel_WithSinglePanel_DoesNotChange()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home]);
        controller.NextPanel();
        Assert.Equal(NotchPanel.Home, controller.CurrentPanel);
        controller.PreviousPanel();
        Assert.Equal(NotchPanel.Home, controller.CurrentPanel);
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
    public void Expand_DirectlyTransitionsToExpanded()
    {
        using var controller = new NotchController();
        Assert.Equal(NotchState.Collapsed, controller.State);

        controller.Expand();
        Assert.Equal(NotchState.Expanded, controller.State);
    }

    [Fact]
    public void Expand_WithPanel_SelectsSpecifiedPanel()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.Shelf]);

        controller.Expand(NotchPanel.Shelf);
        Assert.Equal(NotchState.Expanded, controller.State);
        Assert.Equal(NotchPanel.Shelf, controller.CurrentPanel);
    }

    [Fact]
    public void Expand_FromPeek_TransitionsToExpanded()
    {
        using var controller = new NotchController();
        controller.OnMouseEnter();
        Assert.Equal(NotchState.Peek, controller.State);

        controller.Expand(NotchPanel.Shelf);
        Assert.Equal(NotchState.Expanded, controller.State);
        Assert.Equal(NotchPanel.Shelf, controller.CurrentPanel);
    }

    [Fact]
    public void Expand_WhenAlreadyExpanded_StaysExpandedAndDoesNotToggle()
    {
        using var controller = new NotchController();
        controller.Expand(NotchPanel.Home);
        Assert.Equal(NotchState.Expanded, controller.State);

        // Subsequent drag or expand call must keep it Expanded (must NOT toggle like OnClick)
        controller.Expand(NotchPanel.Shelf);
        Assert.Equal(NotchState.Expanded, controller.State);
        Assert.Equal(NotchPanel.Shelf, controller.CurrentPanel);
    }

    [Fact]
    public void Expand_WithUnenabledPanel_AddsToEnabledAndSelects()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home]); // Shelf is not enabled

        controller.Expand(NotchPanel.Shelf);
        Assert.Equal(NotchState.Expanded, controller.State);
        Assert.Equal(NotchPanel.Shelf, controller.CurrentPanel);
        Assert.Contains(NotchPanel.Shelf, controller.EnabledPanels);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var controller = new NotchController();
        controller.Dispose();
        controller.Dispose(); // second call should be completely safe
    }

    [Fact]
    public void GoToPanel_SelectsPanelAndSlidesInPanelOrder()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.ControlCenter, NotchPanel.Focus, NotchPanel.Shelf]);
        var directions = new List<int>();
        controller.PanelTransitionTriggered += directions.Add;

        controller.GoToPanel(NotchPanel.Focus);
        Assert.Equal(NotchPanel.Focus, controller.CurrentPanel);

        controller.GoToPanel(NotchPanel.ControlCenter);
        Assert.Equal(NotchPanel.ControlCenter, controller.CurrentPanel);

        Assert.Equal([1, -1], directions);
    }

    [Fact]
    public void GoToPanel_IgnoresCurrentAndDisabledPanels()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.ControlCenter]);
        var transitions = 0;
        controller.PanelTransitionTriggered += _ => transitions++;

        controller.GoToPanel(NotchPanel.Home);
        controller.GoToPanel(NotchPanel.Shelf);

        Assert.Equal(NotchPanel.Home, controller.CurrentPanel);
        Assert.Equal(0, transitions);
    }

    [Fact]
    public void SetEnabledPanels_RaisesEnabledPanelsChanged_WithNewOrder()
    {
        using var controller = new NotchController();
        var raised = 0;
        controller.EnabledPanelsChanged += (_, _) => raised++;

        controller.SetEnabledPanels([NotchPanel.Focus, NotchPanel.Home]);

        Assert.Equal(1, raised);
        Assert.Equal([NotchPanel.Focus, NotchPanel.Home], controller.EnabledPanels);
    }

    [Fact]
    public void Expand_ToDisabledPanel_AddsItAndRaisesEnabledPanelsChanged()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.Focus]);
        var raised = 0;
        controller.EnabledPanelsChanged += (_, _) => raised++;

        controller.Expand(NotchPanel.Shelf);

        Assert.Equal(1, raised);
        Assert.Equal([NotchPanel.Home, NotchPanel.Focus, NotchPanel.Shelf], controller.EnabledPanels);
        Assert.Equal(NotchPanel.Shelf, controller.CurrentPanel);
    }

    [Fact]
    public void Expand_ToEnabledPanel_DoesNotRaiseEnabledPanelsChanged()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.ControlCenter]);
        var raised = 0;
        controller.EnabledPanelsChanged += (_, _) => raised++;

        controller.Expand(NotchPanel.ControlCenter);

        Assert.Equal(0, raised);
        Assert.Equal(NotchPanel.ControlCenter, controller.CurrentPanel);
    }
}
