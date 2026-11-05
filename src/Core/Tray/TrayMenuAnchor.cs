using Koy.Core.Shell;

namespace Koy.Core.Tray;

/// <summary>
/// Tepsi simgesine tıklanınca uygulamaya iletilen fare konumu (menünün çıpası). Uygulama bağlam menüsünü bu
/// noktaya göre açar; nokta dock'un üzerine düşerse menü dock'un içine ya da altına taşar. Böyle bir nokta
/// dock'un hemen üstüne çekilir: menü ekranın altına sığmayınca yukarı açıldığı için dock'un üst kenarının
/// üstünde kalır.
/// </summary>
public static class TrayMenuAnchor
{
    /// <summary>Çıpa dock'un üst kenarının bu kadar piksel üstünde durur.</summary>
    public const int Gap = 1;

    public static (int X, int Y) ClampAboveDock(int x, int y, WindowRect dock)
    {
        if (dock.IsEmpty || !dock.Contains(x, y))
            return (x, y);

        return (x, dock.Top - Gap);
    }
}
