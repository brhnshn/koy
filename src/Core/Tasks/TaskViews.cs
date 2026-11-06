using System.Globalization;

namespace Koy.Core.Tasks;

/// <summary>Odak sayfasındaki görev listesinin kapsamı (Gün · Hafta · Ay).</summary>
public enum TaskScope
{
    Day,
    Week,
    Month
}

/// <summary>Görev listesinin saf yardımcıları: kapsam aralıkları, saat girişi, sıradaki görev. WPF yok.</summary>
public static class TaskViews
{
    /// <summary>
    /// Kapsamın günleri (uçlar dahil). Hafta pazartesi başlar (Türkiye). Gün kapsamı tek gündür; sorgu yerine
    /// <see cref="ITaskService.GetToday"/> kullanılır (önceki günden kalan tamamlanmamışlar da görünür).
    /// </summary>
    public static (DateOnly From, DateOnly To) Range(TaskScope scope, DateOnly today)
    {
        switch (scope)
        {
            case TaskScope.Week:
                var offset = ((int)today.DayOfWeek + 6) % 7; // Pazartesi = 0
                var monday = today.AddDays(-offset);
                return (monday, monday.AddDays(6));
            case TaskScope.Month:
                var first = new DateOnly(today.Year, today.Month, 1);
                return (first, first.AddMonths(1).AddDays(-1));
            default:
                return (today, today);
        }
    }

    /// <summary>Gün kapsamı için <see cref="ITaskService.GetToday"/>, diğerleri için <see cref="ITaskService.GetRange"/>.</summary>
    public static IReadOnlyList<TaskItem> Load(ITaskService tasks, TaskScope scope, DateOnly today)
    {
        if (scope == TaskScope.Day)
            return tasks.GetToday();
        var (from, to) = Range(scope, today);
        return tasks.GetRange(from, to);
    }

    /// <summary>
    /// "14:30", "9:05", "14.30" ya da "1430" okunur. Boş giriş "saat yok" demektir (true, null);
    /// geçersiz (25:00, "abc") için false.
    /// </summary>
    public static bool TryParseTime(string? text, out TimeOnly? time)
    {
        time = null;
        var t = text?.Trim();
        if (string.IsNullOrEmpty(t))
            return true;

        t = t.Replace('.', ':');
        if (!t.Contains(':') && t.Length is 3 or 4 && t.All(char.IsAsciiDigit))
            t = t.Insert(t.Length - 2, ":");

        if (TimeOnly.TryParseExact(t, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            time = parsed;
            return true;
        }

        return false;
    }

    /// <summary>"14:30" biçimi (kültürden bağımsız).</summary>
    public static string FormatTime(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Sıradaki görev: listedeki ilk tamamlanmamış (liste zaten saate göre sıralı).</summary>
    public static Guid? NextId(IEnumerable<TaskItem> items) =>
        items.FirstOrDefault(t => !t.Done)?.Id;

    /// <summary>
    /// Sıradaki görevin sağ etiketi: saati geldiyse ya da saati yoksa "şimdi" (<c>true</c>), yoksa saatin kendisi.
    /// Dönüş (şimdiMi, saatMetni); saati olmayan ve sıradaki olmayan görev için saat metni boş.
    /// </summary>
    public static (bool IsNow, string TimeText) Label(TaskItem task, bool isNext, TimeOnly now, DateOnly today)
    {
        var timeText = task.Time is { } t ? FormatTime(t) : "";
        if (!isNext || task.Done)
            return (false, timeText);
        var due = task.Day < today || task.Day == today && (task.Time is null || task.Time <= now);
        return (due, due ? "" : timeText);
    }
}
