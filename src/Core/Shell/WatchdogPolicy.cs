using System.Globalization;

namespace Koy.Core.Shell;

/// <summary>Gözcünün Koy sürecini beklerken aldığı sonuç.</summary>
public enum WatchdogWaitResult
{
    /// <summary>Koy süreci sonlandı (Görev Yöneticisi, çökme ya da normal çıkış).</summary>
    ProcessExited,

    /// <summary>Koy "temiz çıkış" işaretini bıraktı (dock durdu, görev çubuğunu kendisi geri getirdi).</summary>
    StopRequested,

    /// <summary>Gözcü başladığında Koy süreci zaten yoktu.</summary>
    ProcessNotFound,

    /// <summary>Bekleme beklenmedik biçimde başarısız oldu.</summary>
    Failed
}

public enum WatchdogAction
{
    /// <summary>Hiçbir şey yapma, çık.</summary>
    None,

    /// <summary>Windows görev çubuğunu geri getir.</summary>
    RestoreTaskbar
}

/// <summary>
/// Gözcü sürecin (Koy.Watchdog.exe) saf kararları: argüman çözümü, ortak adlar, bekleme sonucundan eylem.
/// Win32 yok; hem Koy hem gözcü aynı adları kullanır.
/// </summary>
public static class WatchdogPolicy
{
    public const string PidArgument = "--pid";

    /// <summary>Aynı anda tek gözcü: ikinci örnek bu muteksi alamayınca hemen çıkar.</summary>
    public const string SingletonMutexName = @"Local\Koy.Watchdog.Singleton";

    /// <summary>Koy'un temiz çıkış işareti için adlandırılmış olay (Koy PID'ine özel).</summary>
    public static string StopEventName(int koyProcessId)
        => @"Local\Koy.Watchdog.Stop." + koyProcessId.ToString(CultureInfo.InvariantCulture);

    /// <summary>Gözcüye verilecek komut satırı argümanları.</summary>
    public static string BuildArguments(int koyProcessId)
        => PidArgument + " " + koyProcessId.ToString(CultureInfo.InvariantCulture);

    /// <summary>`--pid N` argümanını çözer. Eksik, sayı olmayan ya da pozitif olmayan değerde false döner.</summary>
    public static bool TryParseProcessId(IReadOnlyList<string> args, out int processId)
    {
        processId = 0;
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (!string.Equals(args[i], PidArgument, StringComparison.OrdinalIgnoreCase))
                continue;

            return int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out processId)
                   && processId > 0;
        }

        return false;
    }

    /// <summary>
    /// Koy öldüyse (ya da hiç bulunamadıysa) görev çubuğu geri getirilir; temiz çıkışta ve beklenmedik
    /// bekleme hatasında dokunulmaz (hatada yanlışlıkla kullanıcının ayarını bozmamak güvenli taraftır).
    /// </summary>
    public static WatchdogAction Decide(WatchdogWaitResult result) => result switch
    {
        WatchdogWaitResult.ProcessExited => WatchdogAction.RestoreTaskbar,
        WatchdogWaitResult.ProcessNotFound => WatchdogAction.RestoreTaskbar,
        _ => WatchdogAction.None
    };
}
