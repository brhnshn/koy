namespace Koy.Core.SystemStats;

/// <summary>Bir örnekleme anındaki sistem verisi.</summary>
/// <param name="CpuPercent">Toplam işlemci kullanımı, 0-100.</param>
/// <param name="TopProcessName">En çok CPU kullanan sürecin adı (uzantısız); henüz bilinmiyorsa null.</param>
/// <param name="TopProcessCpuPercent">O sürecin toplam işlemci içindeki payı, 0-100.</param>
/// <param name="NetworkName">Bağlı ağ adı (INetworkService), yoksa null.</param>
/// <param name="KoyMemoryBytes">Koy'un özel çalışma kümesi (Görev Yöneticisi "Bellek" ile aynı ölçü).</param>
/// <param name="RatesReady">
/// CPU yüzdesi, en çok CPU kullanan süreç ve ağ hızı iki örnek arası fark ister. Servis yeni başlayınca yalnız anlık
/// değerler (RAM, Koy belleği, ağ adı) hemen okunur ve bu false olur; ilk gerçek örnekle true olur.
/// </param>
public sealed record SystemSnapshot(
    double CpuPercent,
    string? TopProcessName,
    double TopProcessCpuPercent,
    long RamUsedBytes,
    long RamTotalBytes,
    double RamPercent,
    long NetDownBytesPerSec,
    long NetUpBytesPerSec,
    string? NetworkName,
    long KoyMemoryBytes,
    bool RatesReady = true)
{
    public static SystemSnapshot Empty { get; } = new(0, null, 0, 0, 0, 0, 0, 0, null, 0, RatesReady: false);
}

/// <summary>
/// Sistem verisi örnekleyicisi. Zamanlayıcı YALNIZ <see cref="Start"/>'tan sonra çalışır (çentik açıkken),
/// <see cref="Stop"/> zamanlayıcıyı tamamen durdurur; boşta sıfır iş. Start/Stop idempotenttir. Birden çok
/// çentik kullanacaksa App sayaçlı paylaşım yapar (SharedServices.AcquireSystemStats).
///
/// İPLİK: <see cref="Updated"/> ARKA PLAN iş parçacığından (zamanlayıcı) gelir; App tarafı Dispatcher'a
/// geçirmeli. <see cref="Current"/> her iş parçacığından okunabilir.
/// </summary>
public interface ISystemStatsService : IDisposable
{
    bool IsRunning { get; }

    /// <summary>Son örnek (Start'tan önce <see cref="SystemSnapshot.Empty"/>).</summary>
    SystemSnapshot Current { get; }

    /// <summary>Her saniye bir kez (Start sonrası). Arka plan iş parçacığından gelir.</summary>
    event EventHandler<SystemSnapshot>? Updated;

    void Start();

    void Stop();
}
