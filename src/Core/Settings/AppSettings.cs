using Koy.Core.Visibility;

namespace Koy.Core.Settings;

/// <summary>
/// settings.json içeriği. Yeni alan eklerken varsayılan değer ver ki eski dosyalar bozulmasın.
/// Alan yeniden adlandırılır ya da silinirse <see cref="Version"/> artırılır.
/// </summary>
public sealed record AppSettings
{
    public int Version { get; init; } = 1;
    public string Language { get; init; } = "tr";
    public bool NotchEnabled { get; init; } = true;

    /// <summary>Kapalıysa Windows görev çubuğuna hiç dokunulmaz.</summary>
    public bool DockEnabled { get; init; }

    public VisibilityMode NotchVisibility { get; init; } = VisibilityMode.Fixed;
    public VisibilityMode DockVisibility { get; init; } = VisibilityMode.Smart;
    public string? AccentColor { get; init; }
}
