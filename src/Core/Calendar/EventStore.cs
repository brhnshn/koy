using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koy.Core.Calendar;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<CalendarEvent>))]
internal sealed partial class EventJsonContext : JsonSerializerContext;

/// <summary>Etkinlikleri <c>%AppData%\Koy\events.json</c> dosyasında saklar (raf ve ayarlarla aynı klasör).</summary>
public sealed class EventStore(string path)
{
    public static string DefaultPath { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Koy",
        "events.json");

    public string Path { get; } = path;

    /// <summary>Dosya yoksa, okunamıyorsa ya da bozuksa boş liste; takvim açılışı asla bozuk dosyada çökmez.</summary>
    public IReadOnlyList<CalendarEvent> Load()
    {
        try
        {
            if (!File.Exists(Path))
                return [];

            using var stream = File.OpenRead(Path);
            var list = JsonSerializer.Deserialize(stream, EventJsonContext.Default.ListCalendarEvent);
            return list?.Where(e => e is not null && !string.IsNullOrWhiteSpace(e.Id) && !string.IsNullOrWhiteSpace(e.Title)).ToList() ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Önce geçici dosyaya yazar, sonra yerine taşır; yazma yarıda kesilirse eski dosya bozulmaz.</summary>
    public void Save(IReadOnlyList<CalendarEvent> events)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var temp = Path + ".tmp";
        using (var stream = File.Create(temp))
            JsonSerializer.Serialize(stream, events.ToList(), EventJsonContext.Default.ListCalendarEvent);
        File.Move(temp, Path, overwrite: true);
    }
}
