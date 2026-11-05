using Koy.Core.Shell;
using Koy.Core.Tray;
using Xunit;

namespace Koy.Core.Tests;

public class TrayMenuAnchorTests
{
    private static readonly WindowRect Dock = new(500, 1000, 1400, 1080);

    [Fact]
    public void Point_above_dock_is_unchanged() =>
        Assert.Equal((800, 900), TrayMenuAnchor.ClampAboveDock(800, 900, Dock));

    [Fact]
    public void Point_inside_dock_moves_above_top_edge() =>
        Assert.Equal((800, 999), TrayMenuAnchor.ClampAboveDock(800, 1040, Dock));

    [Fact]
    public void Point_beside_dock_is_unchanged() =>
        Assert.Equal((200, 1040), TrayMenuAnchor.ClampAboveDock(200, 1040, Dock));

    [Fact]
    public void Empty_dock_rect_is_ignored() =>
        Assert.Equal((800, 1040), TrayMenuAnchor.ClampAboveDock(800, 1040, WindowRect.Empty));
}
