namespace Koy.Core.Shell;

/// <summary>
/// Dock'taki "Sonlandır" eyleminin hangi süreçleri öldürmeyi reddedeceğine karar verir (saf mantık).
/// Birden çok pencerenin ya da kabuğun ortak süreci öldürülürse ilgisiz uygulamalar da kapanır:
/// Mağaza (UWP) pencereleri ApplicationFrameHost.exe çatısında durur, Gezgin pencereleri explorer.exe içindedir.
/// </summary>
public static class ProcessTerminationPolicy
{
    private static readonly HashSet<string> Protected = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApplicationFrameHost",
        "explorer",
        "dwm",
        "csrss",
        "winlogon",
        "wininit",
        "services",
        "lsass",
        "svchost",
        "System"
    };

    /// <param name="processName">Uzantısız süreç adı (ör. "chrome"); bilinmiyorsa null.</param>
    public static bool CanTerminate(int processId, string? processName, int ownProcessId)
    {
        if (processId <= 0 || processId == ownProcessId)
            return false;

        if (string.IsNullOrWhiteSpace(processName))
            return false;

        var name = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName[..^4]
            : processName;

        return !Protected.Contains(name);
    }
}
