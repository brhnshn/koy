using System.Globalization;

namespace Koy.Core.SystemStats;

/// <summary>
/// Sistem verisi biçimlendirme: Türkçe ondalık virgül, ikili birimler (1 KB = 1024 bayt, Görev Yöneticisi gibi).
/// Kültür sabit tr-TR: cihaz diline bağlı değil (arayüz Türkçe tasarlandı).
/// </summary>
public static class SystemFormat
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Hız: "512 B/sn", "360 KB/sn", "2,5 MB/sn", "1,2 GB/sn". 10'dan küçük değerde bir ondalık.</summary>
    public static string Speed(long bytesPerSecond)
    {
        var (value, unit) = Scale(Math.Max(0, bytesPerSecond));
        return Number(value, trimZero: true) + " " + unit + "/sn";
    }

    /// <summary>Bellek: "9,2 / 16 GB" (kullanılan her zaman bir ondalık, toplam tam sayıysa ondalıksız).</summary>
    public static string Memory(long usedBytes, long totalBytes)
    {
        const double gb = 1024.0 * 1024 * 1024;
        var used = Math.Max(0, usedBytes) / gb;
        var total = Math.Max(0, totalBytes) / gb;
        return used.ToString("0.0", Tr) + " / " + Number(total, trimZero: true, alwaysOneDecimalUnder: 0) + " GB";
    }

    /// <summary>Tek bellek değeri: "128 MB", "1,4 GB".</summary>
    public static string Size(long bytes)
    {
        var (value, unit) = Scale(Math.Max(0, bytes));
        return Number(value, trimZero: true) + " " + unit;
    }

    /// <summary>Yüzde: "%42" (Türkçe yazım). Girdi 0-100 aralığına kısılır, yuvarlanır.</summary>
    public static string Percent(double percent) =>
        "%" + ((int)Math.Round(Math.Clamp(percent, 0, 100), MidpointRounding.AwayFromZero)).ToString(Tr);

    private static (double Value, string Unit) Scale(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var i = 0;
        while (value >= 1024 && i < units.Length - 1)
        {
            value /= 1024;
            i++;
        }

        // Yuvarlama 1024'e denk gelirse bir üst birime geç ("1024 KB" yerine "1 MB").
        if (Math.Round(value) >= 1024 && i < units.Length - 1)
        {
            value /= 1024;
            i++;
        }

        return (value, units[i]);
    }

    // 10'dan küçükse bir ondalık (2,5), değilse tam sayı (360). ",0" kırpılır (2 MB).
    private static string Number(double value, bool trimZero, int alwaysOneDecimalUnder = 10)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        if (rounded < alwaysOneDecimalUnder || (alwaysOneDecimalUnder == 0 && rounded % 1 != 0))
        {
            var text = rounded.ToString("0.0", Tr);
            return trimZero && text.EndsWith(",0") ? text[..^2] : text;
        }

        return Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", Tr);
    }
}
