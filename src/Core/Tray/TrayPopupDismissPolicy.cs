namespace Koy.Core.Tray;

public enum TrayPopupDismissAction
{
    None,

    /// <summary>Tepsi açılır penceresi (animasyonla) kapanmalı.</summary>
    Close
}

/// <summary>
/// Dock'taki tepsi açılır penceresinin ne zaman kapanacağı (saf mantık; Windows'un tepsi taşma penceresi gibi).
/// Pencere odak kaybıyla değil, bilinçli olaylarla kapanır:
///  - pencere ve uygulamanın bağlam menüsü dışına tıklanınca,
///  - bağlam menüsünden seçim yapılınca (menü kapanınca).
/// Uygulamanın menüsü açıkken pencere açık kalır. Win32 sorguları dışarıda yapılır, buraya bayraklar gelir.
/// </summary>
public sealed class TrayPopupDismissPolicy
{
    /// <summary>Sağ tıktan sonra menünün belirmesi için beklenen süre; menü gelmezse (özel pencere açan uygulama) bekleme biter.</summary>
    public static readonly TimeSpan MenuAppearTimeout = TimeSpan.FromMilliseconds(800);

    private enum Phase { Idle, WaitingForMenu, MenuOpen }

    private Phase _phase;
    private TimeSpan _deadline;
    private bool _lastPressInsidePopup;

    /// <summary>Menü bekleniyor ya da açık: çağıran menü sorgusunu bu sürede yapar (boştayken Win32 sorgusu yok).</summary>
    public bool IsExpectingMenu => _phase != Phase.Idle;

    /// <summary>Bir simgeye sağ tıklandı ve uygulamaya iletildi: menü açılmasını beklemeye başla.</summary>
    public void OnIconRightClicked(TimeSpan now)
    {
        _phase = Phase.WaitingForMenu;
        _deadline = now + MenuAppearTimeout;
        _lastPressInsidePopup = false;
    }

    public void Reset()
    {
        _phase = Phase.Idle;
        _lastPressInsidePopup = false;
    }

    /// <summary>Yoklama adımı. Penceredeyken ya da menü varken kısa aralıklarla çağrılır.</summary>
    /// <param name="pointerPressed">Bir fare tuşu bu adımda yeni basıldı (basma kenarı).</param>
    /// <param name="pointerInsidePopup">İmleç tepsi penceresinin içinde.</param>
    /// <param name="pointerInsideMenu">İmleç görünen bir bağlam menüsünün (alt menüler dahil) içinde.</param>
    /// <param name="menuVisible">Ekranda herhangi bir bağlam menüsü görünüyor.</param>
    public TrayPopupDismissAction Tick(
        TimeSpan now, bool pointerPressed, bool pointerInsidePopup, bool pointerInsideMenu, bool menuVisible)
    {
        if (pointerPressed)
        {
            if (!pointerInsidePopup && !pointerInsideMenu)
            {
                Reset();
                return TrayPopupDismissAction.Close;
            }

            _lastPressInsidePopup = pointerInsidePopup && !pointerInsideMenu;
        }

        switch (_phase)
        {
            case Phase.WaitingForMenu:
                if (menuVisible)
                    _phase = Phase.MenuOpen;
                else if (now >= _deadline)
                    _phase = Phase.Idle;
                break;

            case Phase.MenuOpen:
                if (!menuVisible)
                {
                    _phase = Phase.Idle;
                    // Menü pencereye tıklanarak kapandıysa (başka simge) pencere kalır; seçim yapıldıysa ya da
                    // klavyeyle kapandıysa pencere de kapanır.
                    return _lastPressInsidePopup ? TrayPopupDismissAction.None : TrayPopupDismissAction.Close;
                }
                break;
        }

        return TrayPopupDismissAction.None;
    }
}
