namespace Koy.Core.Calendar;

/// <summary>
/// Yerel etkinlik defteri: ekleme, silme, sınırlar. Saf bellek içi mantık; diske yazmayı <see cref="EventStore"/> yapar
/// (kimin ne zaman kaydedeceğine çağıran karar verir).
/// </summary>
public sealed class EventBook
{
    /// <summary>Defterdeki en çok etkinlik; sınırsız büyüyüp ayar dosyasını şişirmesin.</summary>
    public const int MaxEvents = 500;

    private readonly List<CalendarEvent> _events;

    public EventBook(IEnumerable<CalendarEvent>? initial = null)
    {
        _events = initial is null ? [] : initial.Take(MaxEvents).OrderBy(e => e.Start).ToList();
    }

    /// <summary>Etkinlik eklendi ya da silindi.</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<CalendarEvent> Events => _events;

    /// <summary>
    /// Etkinlik ekler. Başlık boşsa ya da defter doluysa eklenmez ve null döner. Süre 0 - 24 saate sıkıştırılır.
    /// </summary>
    public CalendarEvent? Add(string? title, DateOnly day, TimeOnly time, int durationMinutes = 60)
    {
        var normalized = EventAgenda.NormalizeTitle(title);
        if (normalized is null || _events.Count >= MaxEvents)
            return null;

        var created = new CalendarEvent(
            Guid.NewGuid().ToString("N"),
            normalized,
            day.ToDateTime(time),
            EventAgenda.ClampDuration(durationMinutes));

        var index = _events.FindIndex(e => e.Start > created.Start);
        _events.Insert(index < 0 ? _events.Count : index, created);
        Changed?.Invoke(this, EventArgs.Empty);
        return created;
    }

    public bool Remove(string id)
    {
        var removed = _events.RemoveAll(e => e.Id == id) > 0;
        if (removed)
            Changed?.Invoke(this, EventArgs.Empty);
        return removed;
    }

    /// <summary>Bitişi <paramref name="cutoff"/> gününden önce olan eski etkinlikleri siler; silinen sayısını döner.</summary>
    public int PruneBefore(DateOnly cutoff)
    {
        var removed = _events.RemoveAll(e => DateOnly.FromDateTime(e.End) < cutoff);
        if (removed > 0)
            Changed?.Invoke(this, EventArgs.Empty);
        return removed;
    }
}
