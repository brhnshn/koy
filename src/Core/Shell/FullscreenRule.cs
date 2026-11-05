namespace Koy.Core.Shell;

/// <summary>
/// Windows'un <c>SHQueryUserNotificationState</c> yanıtı, tam ekran kararı için sadeleştirilmiş.
/// Bilgi sistem geneli olup ön plan penceresine aittir; başka ekrandaki pencere için bilinmiyor (Unknown) sayılır.
/// </summary>
public enum ShellNotificationState
{
    /// <summary>Sorgulanmadı, başarısız oldu ya da güvenilmez (Sessiz saatler, ekran koruyucu): yalnız geometri karar verir.</summary>
    Unknown,

    /// <summary>Windows bildirim kabul ediyor: tam ekran uygulama yok diyor.</summary>
    Normal,

    /// <summary>QUNS_BUSY / QUNS_APP: Windows tam ekran bir uygulama çalıştığını söylüyor.</summary>
    Busy,

    /// <summary>QUNS_RUNNING_D3D_FULL_SCREEN: özel kipte Direct3D oyunu.</summary>
    D3dFullScreen,

    /// <summary>QUNS_PRESENTATION_MODE: sunum ayarları açık.</summary>
    Presentation
}

public static class ShellNotificationStates
{
    /// <summary>QUERY_USER_NOTIFICATION_STATE sayısal değerini sadeleştirir (Win32 tipi Core'a sızmaz).</summary>
    public static ShellNotificationState FromQuns(int value) => value switch
    {
        2 or 7 => ShellNotificationState.Busy,          // QUNS_BUSY, QUNS_APP
        3 => ShellNotificationState.D3dFullScreen,      // QUNS_RUNNING_D3D_FULL_SCREEN
        4 => ShellNotificationState.Presentation,       // QUNS_PRESENTATION_MODE
        5 => ShellNotificationState.Normal,             // QUNS_ACCEPTS_NOTIFICATIONS
        _ => ShellNotificationState.Unknown             // NOT_PRESENT (1), QUIET_TIME (6) ve bilinmeyenler
    };
}

/// <summary>
/// Ön plan penceresinin tam ekran (oyun, video, F11 tarayıcı) sayılıp sayılmayacağına karar verir.
/// Üç işaretin birleşimi: Windows'un tam ekran bildirimi, başlık çubuğunun olmayışı, monitörü tamamen kaplama.
/// Tek başına hiçbiri yetmez (Sessiz saatler "meşgul" diyebilir; özel çerçeveli büyütülmüş pencere başlıksız
/// görünür). Büyütülmüş (IsZoomed) pencere ancak monitöre tam oturuyorsa (F11 tarayıcı, video) sayılır; kenar payıyla
/// monitörü aşan normal büyütülmüş pencere asla. Windows açıkça "tam ekran yok" derse (Normal) geometri geçersizdir.
/// Özel kipli Direct3D oyunu ve sunum modu kesin tam ekrandır.
/// </summary>
public static class FullscreenRule
{
    public static bool IsFullscreen(
        WindowRect window,
        WindowRect monitor,
        bool hasCaption,
        bool isMaximized,
        ShellNotificationState state = ShellNotificationState.Unknown)
    {
        if (state is ShellNotificationState.D3dFullScreen or ShellNotificationState.Presentation)
            return true;

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
        // payıyla monitörü aşar (-8, -8, 1928, 1088). F11 tarayıcı veya video oynatıcılar WS_MAXIMIZE
        // bayrağını korusa dahi tam olarak monitör sınırlarına oturur; yine de Windows'un da "tam ekran"
        // demesi gerekir, yoksa özel çerçeveli büyütülmüş bir uygulama olabilir.
        if (isMaximized)
        {
            var isExactMonitorFit = Math.Abs(window.Left - monitor.Left) <= 1 &&
                                    Math.Abs(window.Top - monitor.Top) <= 1 &&
                                    Math.Abs(window.Right - monitor.Right) <= 1 &&
                                    Math.Abs(window.Bottom - monitor.Bottom) <= 1;

            if (!isExactMonitorFit)
                return false;
        }

        // Geometri tutuyor. Windows açıkça "bildirim kabul ediyorum" (tam ekran uygulama yok) diyorsa tam ekran değildir
        // (örn. monitör boyutunda bir yer paylaşımı ya da özel çerçeveli büyütülmüş uygulama). Meşgul ya da bilinmiyorsa
        // (başka ekrandaki pencere, Sessiz saatler) geometri yeter.
        return state != ShellNotificationState.Normal;
    }
}
