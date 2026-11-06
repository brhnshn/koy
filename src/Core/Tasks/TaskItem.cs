namespace Koy.Core.Tasks;

/// <summary>
/// Odak görevi. <see cref="Day"/> görevin ait olduğu gün (varsayılan oluşturulduğu gün); <see cref="Time"/> o gün
/// için isteğe bağlı saat (HH:mm).
/// </summary>
public sealed record TaskItem(
    Guid Id,
    string Title,
    DateOnly Day,
    TimeOnly? Time,
    bool Done,
    DateTimeOffset CreatedAt);
