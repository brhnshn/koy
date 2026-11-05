using System.Reflection;

namespace Koy.App;

public enum UpdateCheckResult
{
    None,
    UpToDate,
    Failed,
    NotInstalled
}

/// <summary>Bu oturumdaki gerçek güncelleme denetimi sonucu (otomatik ve elle denetim aynı yere yazar). Uydurma zaman gösterilmez.</summary>
public static class UpdateStatus
{
    public static DateTime? LastCheck { get; private set; }
    public static UpdateCheckResult LastResult { get; private set; }

    public static void Record(UpdateCheckResult result)
    {
        LastResult = result;
        // Kurulu olmayan sürümde denetim yapılmadığı için zaman kaydedilmez.
        LastCheck = result == UpdateCheckResult.NotInstalled ? LastCheck : DateTime.Now;
    }
}

/// <summary>Çalışan uygulamanın sürümü.</summary>
public static class AppInfo
{
    public static string Version { get; } = Read();

    private static string Read()
    {
        var asm = typeof(AppInfo).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                   ?? asm.GetName().Version?.ToString(3)
                   ?? "?";
        var plus = info.IndexOf('+');
        return plus > 0 ? info[..plus] : info;
    }
}
