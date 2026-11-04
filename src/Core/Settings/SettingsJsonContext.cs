using System.Text.Json.Serialization;
using Koy.Core.Dock;

namespace Koy.Core.Settings;

// Kaynak üreticili serileştirme: yansıma yok, açılış ve bellek dostu.
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(DockStyleMode))]
[JsonSerializable(typeof(DockProximityMode))]
[JsonSerializable(typeof(List<string>))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
