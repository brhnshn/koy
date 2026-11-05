namespace Koy.Core.Tray;

public enum TrayHostAction
{
    /// <summary>Bir şey yapma.</summary>
    None,

    /// <summary>Tepsi penceresini kur ve düğmeyi geri getir.</summary>
    Acquire,

    /// <summary>Tepsi penceresini bırak (koşul bozuldu).</summary>
    Release,

    /// <summary>Elimizdeki tepsi penceresi ölmüş: bırak, sonra yeniden kur.</summary>
    Reacquire
}

/// <summary>
/// Dock'ın tepsi penceresiyle ilgili tek karar yeri (saf mantık). Önceki sürümde ilk kurulum hatası kalıcı
/// bayrağa dönüşüyor ve düğme ekran değişimine kadar geri gelmiyordu; artık başarısız kurulum zamanla yeniden
/// denenir (üstel bekleme, üst sınırlı). Yoklama zamanlayıcısı gerekmez: olaylar (pencere değişimi, tepsi
/// penceresinin ölüm bildirimi) bu sınıfa soru sorar.
/// </summary>
public sealed class TrayHostRecovery
{
    public static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private int _failures;
    private DateTime _retryAtUtc = DateTime.MinValue;

    /// <summary>Ardışık başarısız kurulum sayısı.</summary>
    public int Failures => _failures;

    /// <summary>Şu an ne yapılmalı.</summary>
    /// <param name="shouldHost"><see cref="TrayHostPolicy.ShouldHost"/> sonucu (elle bırakma bayrağı dahil).</param>
    /// <param name="hasHost">Dock'ın elinde bir tepsi servisi var.</param>
    /// <param name="hostRunning">Eldeki servis çalışıyor (pencere ayakta).</param>
    public TrayHostAction Decide(bool shouldHost, bool hasHost, bool hostRunning, DateTime nowUtc)
    {
        if (!shouldHost)
        {
            ResetFailures();
            return hasHost ? TrayHostAction.Release : TrayHostAction.None;
        }

        if (hasHost && hostRunning)
            return TrayHostAction.None;

        // Bekleme sürerken ölü tutamaç yine de bırakılır: düğme ölü pencereye bağlı görünmesin.
        if (nowUtc < _retryAtUtc)
            return hasHost ? TrayHostAction.Release : TrayHostAction.None;

        return hasHost ? TrayHostAction.Reacquire : TrayHostAction.Acquire;
    }

    /// <summary>Kurulum sonucu bildirilir: başarıda sayaç sıfırlanır, hatada bir sonraki deneme ertelenir.</summary>
    public void ReportAcquire(bool success, DateTime nowUtc)
    {
        if (success)
        {
            ResetFailures();
            return;
        }

        _failures++;
        var seconds = Math.Min(MaxRetryDelay.TotalSeconds, FirstRetryDelay.TotalSeconds * Math.Pow(2, Math.Min(_failures - 1, 10)));
        _retryAtUtc = nowUtc + TimeSpan.FromSeconds(seconds);
    }

    /// <summary>Beklemeyi bırakır (ekran değişti, ayar değişti): bir sonraki soruda hemen denenir.</summary>
    public void ResetFailures()
    {
        _failures = 0;
        _retryAtUtc = DateTime.MinValue;
    }
}
