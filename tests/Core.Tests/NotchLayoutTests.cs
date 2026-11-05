using Koy.Core.Notch;
using Xunit;

namespace Koy.Core.Tests;

public class NotchLayoutTests
{
    [Theory]
    [InlineData(NotchState.Collapsed, false, true, false)]
    [InlineData(NotchState.Collapsed, true, true, false)]
    // Parça yokken ipucu boş kalır: boşta yerleşimi açık kalır ki Wi-Fi / Bluetooth simgelerine tıklanabilsin.
    [InlineData(NotchState.Peek, false, true, false)]
    [InlineData(NotchState.Peek, true, false, true)]
    [InlineData(NotchState.Expanded, false, false, false)]
    [InlineData(NotchState.Expanded, true, false, false)]
    public void Layout_choice_depends_on_state_and_track(NotchState state, bool hasTrack, bool collapsed, bool peek)
    {
        Assert.Equal(collapsed, NotchLayout.ShowCollapsedLayout(state, hasTrack));
        Assert.Equal(peek, NotchLayout.ShowPeekLayout(state, hasTrack));
    }

    [Fact]
    public void Hover_in_click_mode_without_track_keeps_idle_layout()
    {
        using var controller = new NotchController { OpenOnHover = false };

        controller.OnMouseEnter();

        Assert.Equal(NotchState.Peek, controller.State);
        Assert.True(NotchLayout.ShowCollapsedLayout(controller.State, hasTrack: false));
        Assert.False(NotchLayout.ShowPeekLayout(controller.State, hasTrack: false));
    }
}
