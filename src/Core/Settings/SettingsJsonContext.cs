using System.Text.Json.Serialization;

namespace Koy.Core.Settings;

// Kaynak üreticili serileştirme: yansıma yok, açılış ve bellek dostu.
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
