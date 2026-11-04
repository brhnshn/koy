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

        // Monitörü tamamen kaplamayan pencere tam ekran değildir.
        if (window.Left > monitor.Left ||
            window.Top > monitor.Top ||
            window.Right < monitor.Right ||
            window.Bottom < monitor.Bottom)
        {
            return false;
        }

        // Büyütülmüş (WS_MAXIMIZE) pencere:
        // Normal büyütülmüş pencereler (Chrome, VS Code) görev çubuğu yokken kenar boyutlandırma
        // payıyla monitörü aşar (-8, -8, 1928, 1088). Bunlar tam ekran değildir.
        // Ancak F11 tarayıcı veya video oynatıcılar WS_MAXIMIZE bayrağını korusa dahi
        // tam olarak monitör sınırlarına (0, 0, 1920, 1080) oturur.
        if (isMaximized)
        {
            var isExactMonitorFit = Math.Abs(window.Left - monitor.Left) <= 1 &&
                                    Math.Abs(window.Top - monitor.Top) <= 1 &&
                                    Math.Abs(window.Right - monitor.Right) <= 1 &&
                                    Math.Abs(window.Bottom - monitor.Bottom) <= 1;

            if (!isExactMonitorFit)
                return false;
        }

        return true;
    }
}
