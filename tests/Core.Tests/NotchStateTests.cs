using Koy.Core.Notch;
using Xunit;

namespace Koy.Core.Tests;

public class NotchStateTests
{
    [Theory]
    [InlineData(NotchState.Collapsed, NotchTrigger.MouseEnter, NotchState.Peek)]
    [InlineData(NotchState.Collapsed, NotchTrigger.TrackChanged, NotchState.Peek)]
    [InlineData(NotchState.Collapsed, NotchTrigger.Click, NotchState.Expanded)]
    [InlineData(NotchState.Collapsed, NotchTrigger.MouseLeave, NotchState.Collapsed)]
    [InlineData(NotchState.Collapsed, NotchTrigger.AutoPeekTimeout, NotchState.Collapsed)]
    [InlineData(NotchState.Collapsed, NotchTrigger.MouseLeaveDelayTimeout, NotchState.Collapsed)]
    public void Collapsed_Transitions(NotchState initial, NotchTrigger trigger, NotchState expected)
    {
        var result = NotchStateTransitions.Transition(initial, trigger);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(NotchState.Peek, NotchTrigger.Click, NotchState.Expanded)]
    [InlineData(NotchState.Peek, NotchTrigger.MouseLeave, NotchState.Collapsed)]
    [InlineData(NotchState.Peek, NotchTrigger.AutoPeekTimeout, NotchState.Collapsed)]
    [InlineData(NotchState.Peek, NotchTrigger.MouseEnter, NotchState.Peek)]
    [InlineData(NotchState.Peek, NotchTrigger.TrackChanged, NotchState.Peek)]
    public void Peek_Transitions(NotchState initial, NotchTrigger trigger, NotchState expected)
    {
        var result = NotchStateTransitions.Transition(initial, trigger);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(NotchState.Expanded, NotchTrigger.Click, NotchState.Collapsed)]
    [InlineData(NotchState.Expanded, NotchTrigger.MouseLeaveDelayTimeout, NotchState.Collapsed)]
    [InlineData(NotchState.Expanded, NotchTrigger.MouseEnter, NotchState.Expanded)]
    [InlineData(NotchState.Expanded, NotchTrigger.MouseLeave, NotchState.Expanded)]
    [InlineData(NotchState.Expanded, NotchTrigger.TrackChanged, NotchState.Expanded)]
    public void Expanded_Transitions(NotchState initial, NotchTrigger trigger, NotchState expected)
    {
        var result = NotchStateTransitions.Transition(initial, trigger);
        Assert.Equal(expected, result);
    }
}
