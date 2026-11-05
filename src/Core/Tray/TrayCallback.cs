namespace Koy.Core.Tray;

public enum TrayMouseAction
{
    LeftClick,
    LeftDoubleClick,
    RightClick
}

/// <summary>Uygulamanın geri çağrı penceresine gönderilecek tek mesaj.</summary>
public readonly record struct TrayCallbackMessage(uint Message, nint WParam, nint LParam);

/// <summary>
/// Dock'taki tepsi simgesine tıklanınca uygulamaya, Windows görev çubuğunun göndereceği mesajların aynısını üretir.
/// NOTIFYICON_VERSION_4 (4) ve sonrası: wParam = imleç konumu, lParam = (mesaj | kimlik &lt;&lt; 16).
/// Eski sürümler: wParam = kimlik, lParam = fare mesajı.
/// </summary>
public static class TrayCallback
{
    public const uint WmContextMenu = 0x007B;
    public const uint WmLButtonDown = 0x0201;
    public const uint WmLButtonUp = 0x0202;
    public const uint WmLButtonDblClk = 0x0203;
    public const uint WmRButtonDown = 0x0204;
    public const uint WmRButtonUp = 0x0205;
    public const uint NinSelect = 0x0400;

    public static IReadOnlyList<TrayCallbackMessage> Build(TrayIcon icon, TrayMouseAction action, int cursorX, int cursorY)
    {
        if (icon.CallbackMessage == 0 || icon.OwnerHwnd == 0)
            return [];

        var modern = icon.Version >= 4;
        uint[] mouse = action switch
        {
            TrayMouseAction.LeftClick => modern ? [WmLButtonDown, WmLButtonUp, NinSelect] : [WmLButtonDown, WmLButtonUp],
            TrayMouseAction.LeftDoubleClick => [WmLButtonDblClk, WmLButtonUp],
            TrayMouseAction.RightClick => modern ? [WmRButtonDown, WmRButtonUp, WmContextMenu] : [WmRButtonDown, WmRButtonUp],
            _ => []
        };

        var position = (nint)(int)(((uint)cursorY & 0xFFFF) << 16 | ((uint)cursorX & 0xFFFF));
        var result = new TrayCallbackMessage[mouse.Length];
        for (var i = 0; i < mouse.Length; i++)
        {
            result[i] = modern
                ? new TrayCallbackMessage(icon.CallbackMessage, position, (nint)(int)((mouse[i] & 0xFFFF) | (icon.Id & 0xFFFF) << 16))
                : new TrayCallbackMessage(icon.CallbackMessage, (nint)icon.Id, (nint)mouse[i]);
        }

        return result;
    }
}
