namespace Koy.Core.SystemStats;

/// <summary>
/// Sayfa geri dönünce gösterilecek değeri seçer. Servis yalnız sayfa görünürken tutulduğundan her dönüşte
/// yeni kurulur ve ilk örneğe kadar boştur; bu aralıkta "–" yerine son bilinen değer gösterilir.
/// </summary>
public static class SystemSnapshotMerge
{
    /// <summary>
    /// <paramref name="fresh"/> tam örnekse (<see cref="SystemSnapshot.RatesReady"/>) aynen döner.
    /// Yalnız anlık değerleri varsa (RAM, Koy belleği, ağ adı) onlar taze alınır, CPU / süreç / ağ hızı
    /// <paramref name="lastKnown"/>'dan gelir. İkisinde de oran yoksa <paramref name="fresh"/> döner (CPU henüz bilinmiyor).
    /// </summary>
    public static SystemSnapshot Merge(SystemSnapshot fresh, SystemSnapshot? lastKnown)
    {
        if (fresh.RatesReady || lastKnown is not { RatesReady: true })
            return fresh;

        return fresh with
        {
            CpuPercent = lastKnown.CpuPercent,
            TopProcessName = lastKnown.TopProcessName,
            TopProcessCpuPercent = lastKnown.TopProcessCpuPercent,
            NetDownBytesPerSec = lastKnown.NetDownBytesPerSec,
            NetUpBytesPerSec = lastKnown.NetUpBytesPerSec,
            RatesReady = true
        };
    }

    /// <summary>Anlık değerler (RAM) okunabildi mi: hiç değer yoksa ekran "–" gösterir.</summary>
    public static bool HasInstantData(SystemSnapshot s) => s.RamTotalBytes > 0;
}
