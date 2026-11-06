using Koy.Core.Shell;

namespace Koy.Core.Dock;

/// <summary>
/// Ön plandaki pencerenin hangi ekranın dock'unu etkileyeceğine karar verir.
/// Pencere yalnız bulunduğu ekranın (Windows'un MonitorFromWindow ile seçtiği, alanın çoğunu kaplayan) dock'unu etkiler.
/// Büyütülmüş pencere kenarlardan birkaç piksel taşar (ör. 0–1920 ekranda -8…1928); sınırlar bütün dock'lara verilince
/// bu taşma yan ekranın kenarına değiyor ve yan ekrandaki dock "pencere üstüme bindi" sanıp yapışık moda geçiyordu.
/// </summary>
public static class DockForegroundFilter
{
    /// <summary>
    /// Bu dock'a verilecek pencere sınırları: pencere başka ekrandaysa boş (çakışma yok).
    /// Pencerenin ekranı bilinmiyorsa (0) eski davranış korunur, sınırlar olduğu gibi verilir.
    /// </summary>
    public static WindowRect BoundsForDock(WindowRect windowBounds, nint windowMonitor, nint dockMonitor) =>
        AppliesToDock(windowMonitor, dockMonitor) ? windowBounds : WindowRect.Empty;

    /// <summary>Pencere bu dock'un ekranında mı (ekranı bilinmiyorsa evet sayılır).</summary>
    public static bool AppliesToDock(nint windowMonitor, nint dockMonitor) =>
        windowMonitor == 0 || windowMonitor == dockMonitor;
}
