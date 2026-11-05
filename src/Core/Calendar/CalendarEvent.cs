namespace Koy.Core.Calendar;

/// <summary>Takvim görünümü: ay ızgarası, hafta listesi ya da tek gün.</summary>
public enum CalendarViewMode
{
    Month,
    Week,
    Day
}

/// <summary>
/// Yerel takvim etkinliği (Google / Outlook yok). <paramref name="Start"/> yerel saattir (saat dilimi taşımaz);
/// <paramref name="DurationMinutes"/> 0 ise tek bir an (hatırlatma gibi).
/// </summary>
public sealed record CalendarEvent(string Id, string Title, DateTime Start, int DurationMinutes)
{
    public DateTime End => Start.AddMinutes(DurationMinutes);

    public DateOnly Date => DateOnly.FromDateTime(Start);
}

/// <summary>Bir günün etkinlikleri (hafta görünümünün satırı).</summary>
public sealed record DayAgenda(DateOnly Date, bool IsToday, IReadOnlyList<CalendarEvent> Events);
