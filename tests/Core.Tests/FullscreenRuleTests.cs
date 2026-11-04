using Koy.Core.Shell;

namespace Koy.Core.Tests;

public class FullscreenRuleTests
{
    private static readonly WindowRect Monitor = new(0, 0, 1920, 1080);

    [Fact]
    public void BorderlessWindowCoveringMonitor_IsFullscreen()
    {
        // Medya Oynatıcı tam ekran: başlık yok, büyütülmüş değil, monitörü tam kaplıyor.
        Assert.True(FullscreenRule.IsFullscreen(Monitor, Monitor, hasCaption: false, isMaximized: false));
    }

    [Fact]
    public void MaximizedBorderlessWindow_WithHiddenTaskbar_IsNotFullscreen()
    {
        // Görev çubuğu gizliyken büyütülmüş Chrome/Electron penceresi kenar payıyla monitörü aşar.
        var maximized = new WindowRect(-9, -9, 1929, 1089);
        Assert.False(FullscreenRule.IsFullscreen(maximized, Monitor, hasCaption: false, isMaximized: true));
    }

    [Fact]
    public void CaptionedWindowCoveringMonitor_IsNotFullscreen()
    {
        Assert.False(FullscreenRule.IsFullscreen(Monitor, Monitor, hasCaption: true, isMaximized: false));
    }

    [Fact]
    public void F11BrowserCoveringMonitor_WithMaximizedFlag_IsFullscreen()
    {
        // F11 tarayıcı veya video oynatıcı: WS_MAXIMIZE bayrağını korusa dahi monitöre tam oturur.
        Assert.True(FullscreenRule.IsFullscreen(Monitor, Monitor, hasCaption: false, isMaximized: true));
    }

    [Fact]
    public void WindowSmallerThanMonitor_IsNotFullscreen()
    {
        var window = new WindowRect(100, 100, 900, 700);
        Assert.False(FullscreenRule.IsFullscreen(window, Monitor, hasCaption: false, isMaximized: false));
    }
}
