namespace Koy.Core.Dock;

/// <summary>Dock'ta simge sürüklerken sıra ve kayma hesapları (saf mantık; yay animasyonu hedefleri bunlardan gelir).</summary>
public static class DockReorderMath
{
    /// <summary>
    /// Sürüklenen simgenin merkezi, başlangıç yuvasından <paramref name="dragOffset"/> kadar kaydıysa hangi yuvaya düşer.
    /// Yuvalar eşit genişliktedir (<paramref name="slotWidth"/>); sonuç [0, count) aralığına kırpılır.
    /// </summary>
    public static int TargetIndex(int count, int originalIndex, double dragOffset, double slotWidth)
    {
        if (count <= 0 || slotWidth <= 0)
            return Math.Max(0, originalIndex);

        var shifted = (int)Math.Round(dragOffset / slotWidth, MidpointRounding.AwayFromZero);
        return Math.Clamp(originalIndex + shifted, 0, count - 1);
    }

    /// <summary>
    /// Sürükleme sürerken bir öğenin (sürüklenen hariç) yatay kayması: sürüklenen sağa geçtikçe aradakiler sola,
    /// sola geçtikçe sağa birer yuva kayar.
    /// </summary>
    public static double ShiftFor(int index, int draggedIndex, int targetIndex, double slotWidth)
    {
        if (index == draggedIndex)
            return 0;

        if (draggedIndex < targetIndex && index > draggedIndex && index <= targetIndex)
            return -slotWidth;

        if (targetIndex < draggedIndex && index >= targetIndex && index < draggedIndex)
            return slotWidth;

        return 0;
    }

    /// <summary>Sürükleme eşiği aşıldı mı (tıklama ile sürüklemeyi ayırır).</summary>
    public static bool ExceedsThreshold(double dx, double dy, double threshold)
        => Math.Abs(dx) >= threshold || Math.Abs(dy) >= threshold;
}
