namespace Koy.Core.SystemStats;

/// <summary>Örnekleme hesapları (saf, WinAPI yok).</summary>
public static class SystemStatsMath
{
    /// <summary>
    /// GetSystemTimes değerleri (100 ns birim, birikimli). Not: kernel süresi boşta süresini DE içerir,
    /// bu yüzden toplam = kernel + user, meşgul = toplam - boşta. Delta sıfır/negatifse 0.
    /// </summary>
    public static double CpuPercent(
        ulong prevIdle, ulong prevKernel, ulong prevUser,
        ulong idle, ulong kernel, ulong user)
    {
        if (idle < prevIdle || kernel < prevKernel || user < prevUser)
            return 0;

        var total = (kernel - prevKernel) + (user - prevUser);
        if (total == 0)
            return 0;

        var idleDelta = idle - prevIdle;
        var busy = total > idleDelta ? total - idleDelta : 0;
        return Math.Clamp(busy * 100.0 / total, 0, 100);
    }

    /// <summary>
    /// Bayt sayacından hız (bayt/sn). Sayaç geri giderse (arayüz sıfırlandı, bağdaştırıcı çıktı) 0 döner,
    /// devasa sıçrama göstermez. Geçen süre sıfır/negatifse 0.
    /// </summary>
    public static long BytesPerSecond(ulong previous, ulong current, double elapsedSeconds)
    {
        if (elapsedSeconds <= 0 || current < previous)
            return 0;
        return (long)((current - previous) / elapsedSeconds);
    }

    public static double Percent(double used, double total) =>
        total <= 0 ? 0 : Math.Clamp(used * 100.0 / total, 0, 100);

    /// <summary>
    /// Bir sürecin toplam işlemcideki payı: CPU süresi farkı / (geçen süre x çekirdek sayısı). Süre birimi aynı olmalı.
    /// </summary>
    public static double ProcessCpuPercent(double cpuSecondsDelta, double elapsedSeconds, int processorCount)
    {
        if (cpuSecondsDelta <= 0 || elapsedSeconds <= 0 || processorCount <= 0)
            return 0;
        return Math.Clamp(cpuSecondsDelta * 100.0 / (elapsedSeconds * processorCount), 0, 100);
    }
}

/// <summary>Bir süreç taramasındaki tek süreç: kimlik, ad, birikimli CPU süresi (saniye).</summary>
public readonly record struct ProcessCpuSample(int Pid, string Name, double CpuSeconds);

/// <summary>"En çok CPU kullanan süreç" seçimi: iki tarama arasındaki CPU süresi farkına göre (saf).</summary>
public static class TopProcessPicker
{
    /// <summary>
    /// <paramref name="previous"/> (pid -> CPU saniyesi) ile <paramref name="current"/> arasında en çok artan süreç.
    /// Önceki taramada olmayan süreç (yeni açıldı) yok sayılır: tüm ömür boyu CPU'su tek seferde sıçrama yapar.
    /// pid 0 (Idle) hariç. Hiç aday yoksa null.
    /// </summary>
    public static (string Name, double Percent)? Pick(
        IReadOnlyDictionary<int, double> previous,
        IEnumerable<ProcessCpuSample> current,
        double elapsedSeconds,
        int processorCount)
    {
        string? bestName = null;
        var bestPercent = 0.0;
        foreach (var sample in current)
        {
            if (sample.Pid == 0 || !previous.TryGetValue(sample.Pid, out var before))
                continue;
            var percent = SystemStatsMath.ProcessCpuPercent(sample.CpuSeconds - before, elapsedSeconds, processorCount);
            if (percent > bestPercent)
            {
                bestPercent = percent;
                bestName = sample.Name;
            }
        }

        return bestName is null ? null : (bestName, bestPercent);
    }
}
