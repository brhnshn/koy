namespace Koy.Core.Shell;

/// <summary>
/// Ön plan penceresinin tam ekran (oyun, video, F11 tarayıcı) sayılıp sayılmayacağına karar verir.
/// </summary>
public static class FullscreenRule
{
    public static bool IsFullscreen(WindowRect window, WindowRect monitor, bool hasCaption, bool isMaximized)
    {
        // Başlıklı pencere ekranı kaplasa da tam ekran sayılmaz.
        if (hasCaption)
            return false;

        // Dock görev çubuğunu gizleyince büyütülmüş pencereler (Chrome, VS Code, Electron; başlık çubuğu yok)
        // monitörün tamamını kaplar. Bunlar tam ekran değildir; sayılırsa çentik ve dock hiç görünmez.
        if (isMaximized)
            return false;

        return window.Left <= monitor.Left &&
               window.Top <= monitor.Top &&
               window.Right >= monitor.Right &&
               window.Bottom >= monitor.Bottom;
    }
}
