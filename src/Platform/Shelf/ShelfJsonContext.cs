using System.Text.Json.Serialization;
using Koy.Core.Shelf;

namespace Koy.Platform.Shelf;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<ShelfItem>))]
[JsonSerializable(typeof(ShelfItem))]
internal sealed partial class ShelfJsonContext : JsonSerializerContext;
