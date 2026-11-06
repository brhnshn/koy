using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.ProcessStatus;

namespace Koy.Platform.Diagnostics;

/// <summary>Süreç bellek ölçümü.</summary>
public static class ProcessMemory
{
    /// <summary>
    /// Koy'un özel çalışma kümesi (private working set), bayt. Görev Yöneticisi'nin "Bellek" sütunuyla aynı ölçü
    /// (Process.PrivateMemorySize64 ayrılmış/commit belleği verir, bu değil). Tek ucuz çağrı, süreç taraması yok.
    /// Ölçülemezse 0.
    /// </summary>
    public static long GetPrivateWorkingSetBytes()
    {
        var counters = new PROCESS_MEMORY_COUNTERS_EX2 { cb = (uint)Marshal.SizeOf<PROCESS_MEMORY_COUNTERS_EX2>() };
        unsafe
        {
            // Sahte tanıtıcı (-1): kapatmaya gerek yok.
            if (!PInvoke.K32GetProcessMemoryInfo(PInvoke.GetCurrentProcess(), (PROCESS_MEMORY_COUNTERS*)&counters, counters.cb))
                return 0;
        }

        return (long)counters.PrivateWorkingSetSize;
    }
}
