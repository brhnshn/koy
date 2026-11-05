namespace Koy.Core.Notch;

/// <summary>
/// Kontrol Merkezi döşemelerinin sırası (<c>AppSettings.ControlCenterLayout</c>). Kayıtlı liste eski, eksik ya da
/// bozuk olabilir; <see cref="Resolve"/> her zaman bilinen üç döşemeyi tam olarak bir kez içeren bir sıra üretir.
/// </summary>
public static class ControlCenterTiles
{
    public const string WiFi = "wifi";
    public const string Bluetooth = "bluetooth";
    public const string DoNotDisturb = "donotdisturb";

    public static readonly IReadOnlyList<string> DefaultOrder = [WiFi, Bluetooth, DoNotDisturb];

    /// <summary>
    /// Kayıtlı sırayı geçerli sıraya çevirir: büyük/küçük harf yok sayılır, bilinmeyenler ve tekrarlar atılır,
    /// listede olmayan döşemeler varsayılan sıralarıyla sona eklenir. Liste null/boşsa varsayılan sıra döner.
    /// </summary>
    public static List<string> Resolve(IEnumerable<string>? saved)
    {
        var result = new List<string>(DefaultOrder.Count);
        if (saved is not null)
        {
            foreach (var raw in saved)
            {
                var id = raw?.Trim().ToLowerInvariant();
                if (id is not null && DefaultOrder.Contains(id) && !result.Contains(id))
                    result.Add(id);
            }
        }

        foreach (var id in DefaultOrder)
        {
            if (!result.Contains(id))
                result.Add(id);
        }

        return result;
    }

    /// <summary>
    /// <paramref name="id"/> döşemesini <paramref name="toIndex"/> konumuna taşır (aralık dışı değer uçlara sıkıştırılır).
    /// Bilinmeyen id için sıra olduğu gibi döner. Girdi değiştirilmez.
    /// </summary>
    public static List<string> Move(IReadOnlyList<string> order, string id, int toIndex)
    {
        var result = order.ToList();
        var from = result.IndexOf(id);
        if (from < 0)
            return result;

        result.RemoveAt(from);
        result.Insert(Math.Clamp(toIndex, 0, result.Count), id);
        return result;
    }

    /// <summary>
    /// Sürüklenen döşemenin merkezi <paramref name="centerX"/> konumundayken hangi yuvaya düşeceği.
    /// Yuvalar soldan sağa eşit genişlikte (<paramref name="slotWidth"/>) ve aralarında <paramref name="gap"/> boşlukla dizilir.
    /// </summary>
    public static int SlotAt(double centerX, double slotWidth, double gap, int count)
    {
        if (count <= 1 || slotWidth <= 0)
            return 0;

        var index = (int)Math.Floor(centerX / (slotWidth + gap));
        return Math.Clamp(index, 0, count - 1);
    }

    /// <summary>İki sıra aynı mı (kaydetmeye değer bir değişiklik var mı).</summary>
    public static bool SameOrder(IReadOnlyList<string> a, IReadOnlyList<string> b) => a.SequenceEqual(b);
}
