namespace Koy.Core.Timers;

/// <summary>
/// Zamanlayıcı süre seçimi: hazır süreler ve artı/eksi düğmesinin adım kuralı. Küçük sürelerde ince, büyüklerde kaba
/// adım atar ki 1 dakikadan 2 saate az tıkla gidilebilsin.
/// </summary>
public static class CountdownPresets
{
    /// <summary>Hazır süreler (dakika).</summary>
    public static readonly IReadOnlyList<int> Minutes = [1, 5, 10, 25];

    /// <summary>Başlangıç süresi (dakika).</summary>
    public const int DefaultMinutes = 5;

    /// <summary>
    /// Artı/eksi düğmesinin yeni süresi. Adım: 10 dakikanın altında 1, bir saatin altında 5, üstünde 15 dakika.
    /// Sonuç her zaman adımın katına oturur (12 → 15 ya da 10). Sonuç 1 dakika - 99 saat.
    /// </summary>
    public static TimeSpan Step(TimeSpan current, bool increase)
    {
        var minutes = (int)Math.Round(current.TotalMinutes);
        var step = StepFor(minutes, increase);

        var next = increase
            ? (minutes / step + 1) * step
            : (int)Math.Ceiling(minutes / (double)step - 1) * step;

        var clamped = Math.Clamp(next, 1, (int)CountdownTimer.MaxDuration.TotalMinutes);
        return TimeSpan.FromMinutes(clamped);
    }

    private static int StepFor(int minutes, bool increase)
    {
        // Eşikte yön önemli: 10'dan aşağı inerken 1'er, 10'dan yukarı çıkarken 5'er.
        var reference = increase ? minutes : minutes - 1;
        return reference < 10 ? 1 : reference < 60 ? 5 : 15;
    }
}
