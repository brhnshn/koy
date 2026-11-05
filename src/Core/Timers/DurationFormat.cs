using System.Globalization;

namespace Koy.Core.Timers;

/// <summary>Zamanlayıcı ve kronometre metinleri. Kültürden bağımsız (iki nokta ve nokta sabit).</summary>
public static class DurationFormat
{
    /// <summary>"05:00", bir saati geçince "1:05:00". Saniyenin kesri yukarı yuvarlanır: 0,4 sn kalmışsa "00:01".</summary>
    public static string Countdown(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
            return "00:00";

        var seconds = (long)Math.Ceiling(value.TotalSeconds);
        return Clock(seconds / 3600, seconds / 60 % 60, seconds % 60);
    }

    /// <summary>"00:12.3" (dakika:saniye.onda bir); bir saati geçince "1:00:12.3". Kesir aşağı yuvarlanır.</summary>
    public static string Stopwatch(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
            value = TimeSpan.Zero;

        var tenths = (long)(value.TotalMilliseconds / 100);
        var seconds = tenths / 10;
        return Clock(seconds / 3600, seconds / 60 % 60, seconds % 60) + "." + (tenths % 10).ToString(CultureInfo.InvariantCulture);
    }

    private static string Clock(long hours, long minutes, long seconds) =>
        hours > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{hours}:{minutes:D2}:{seconds:D2}")
            : string.Create(CultureInfo.InvariantCulture, $"{minutes:D2}:{seconds:D2}");
}
