namespace Koy.Core.Shell;

/// <summary>
/// Windows kabuğunun görev çubuğuna bağlı açılır pencereleri (Başlat, Arama, bildirim merkezi / hızlı ayarlar).
/// Bunlar ön plana gelince dock "üstüne binen pencere" gibi çekilmez; Windows görev çubuğu gibi o ekranda görünür kalır.
/// </summary>
public static class ShellFlyouts
{
    private static readonly string[] FlyoutProcesses =
    [
        "StartMenuExperienceHost", // Başlat menüsü
        "SearchHost",              // Windows 11 arama
        "SearchApp",               // eski arama süreci
        "ShellExperienceHost",     // bildirim merkezi, hızlı ayarlar, takvim açılır penceresi
    ];

    /// <summary>Süreç adı (uzantısız, <c>Process.ProcessName</c>) kabuk açılır penceresine mi ait.</summary>
    public static bool IsFlyoutProcess(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return false;

        foreach (var name in FlyoutProcesses)
        {
            if (string.Equals(name, processName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
