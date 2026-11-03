namespace Koy.Core.Calendar;

public sealed record CalendarDay(
    DateOnly Date,
    bool IsCurrentMonth,
    bool IsToday,
    bool IsWeekend);

/// <summary>
/// Ay görünümü için 6×7 = 42 günlük ızgara üretir (Pazartesi başlangıçlı).
/// Saf C#, kültür ve tarih bağımsız test edilebilir.
/// </summary>
public static class MonthGrid
{
    public static IReadOnlyList<CalendarDay> Build(int year, int month, DateOnly? today = null)
    {
        var currentToday = today ?? DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(year, month, 1);

        // Pazartesi başlangıç: Pazartesi = 1, Pazar = 0 -> Pazar için 7
        var dayOfWeek = firstOfMonth.DayOfWeek;
        var startOffset = (dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek) - 1;

        var startDate = firstOfMonth.AddDays(-startOffset);
        var days = new List<CalendarDay>(42);

        for (var i = 0; i < 42; i++)
        {
            var date = startDate.AddDays(i);
            var isCurrentMonth = date.Month == month && date.Year == year;
            var isToday = date == currentToday;
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            days.Add(new CalendarDay(date, isCurrentMonth, isToday, isWeekend));
        }

        return days;
    }
}
