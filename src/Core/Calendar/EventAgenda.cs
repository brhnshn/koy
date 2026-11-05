namespace Koy.Core.Calendar;

/// <summary>
/// Etkinlik listesinden gün, hafta ve "sıradaki" görünümlerini üretir. Saf mantık: saat ve liste dışarıdan gelir.
/// Hafta Pazartesi başlar (ay ızgarasıyla aynı).
/// </summary>
public static class EventAgenda
{
    public const int MaxTitleLength = 60;
    public const int MaxDurationMinutes = 24 * 60;

    /// <summary>Verilen günün etkinlikleri, başlangıç saatine göre.</summary>
    public static IReadOnlyList<CalendarEvent> ForDay(IEnumerable<CalendarEvent> events, DateOnly day) =>
        events.Where(e => e.Date == day).OrderBy(e => e.Start).ThenBy(e => e.Title, StringComparer.CurrentCultureIgnoreCase).ToList();

    /// <summary>Günün ait olduğu haftanın Pazartesi'si.</summary>
    public static DateOnly WeekStart(DateOnly day)
    {
        var offset = day.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)day.DayOfWeek - 1;
        return day.AddDays(-offset);
    }

    /// <summary>Günün haftasındaki 7 gün (Pazartesi - Pazar) ve her günün etkinlikleri.</summary>
    public static IReadOnlyList<DayAgenda> ForWeek(IEnumerable<CalendarEvent> events, DateOnly day, DateOnly today)
    {
        var list = events as IReadOnlyCollection<CalendarEvent> ?? events.ToList();
        var start = WeekStart(day);
        var days = new List<DayAgenda>(7);
        for (var i = 0; i < 7; i++)
        {
            var date = start.AddDays(i);
            days.Add(new DayAgenda(date, date == today, ForDay(list, date)));
        }

        return days;
    }

    /// <summary>
    /// Bugünün sıradaki etkinliği: şu an sürenler ve henüz başlamamış olanlar arasında en erken başlayan.
    /// Bitmiş etkinlikler ve başka günler sayılmaz; yoksa null.
    /// </summary>
    public static CalendarEvent? NextToday(IEnumerable<CalendarEvent> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        return events
            .Where(e => e.Date == today && (e.DurationMinutes == 0 ? e.Start >= now : e.End > now))
            .OrderBy(e => e.Start)
            .FirstOrDefault();
    }

    /// <summary>Başlığı kırpar ve uzunluğu sınırlar; boşsa null (etkinlik eklenemez).</summary>
    public static string? NormalizeTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        return trimmed.Length > MaxTitleLength ? trimmed[..MaxTitleLength].TrimEnd() : trimmed;
    }

    /// <summary>"14:30", "9:05", "1430" gibi girdiyi saate çevirir; geçersizse false.</summary>
    public static bool TryParseTime(string? text, out TimeOnly time)
    {
        time = default;
        var value = text?.Trim().Replace('.', ':');
        if (string.IsNullOrEmpty(value))
            return false;

        if (!value.Contains(':') && value.All(char.IsDigit) && value.Length is 3 or 4)
            value = value.Insert(value.Length - 2, ":");

        var parts = value.Split(':');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var hour) ||
            !int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var minute) ||
            hour is < 0 or > 23 || minute is < 0 or > 59 || parts[1].Length != 2)
            return false;

        time = new TimeOnly(hour, minute);
        return true;
    }

    public static int ClampDuration(int minutes) => Math.Clamp(minutes, 0, MaxDurationMinutes);
}
