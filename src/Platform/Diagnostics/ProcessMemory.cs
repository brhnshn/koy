using System.Runtime.InteropServices;

namespace Koy.Platform.Diagnostics;

/// <summary>
/// Görev Yöneticisi'nin "Bellek" sütunuyla aynı ölçü: süreç başına özel çalışma kümesi (private working set).
/// <c>Process.PrivateMemorySize64</c> ayrılmış özel belleği (commit) verir; RAM'de olmayan sayfaları da saydığı için
/// Görev Yöneticisi'nden belirgin yüksek görünür (Koy içinde ~125, Görev Yöneticisi'nde ~90 MB).
/// </summary>
public static partial class ProcessMemory
{
    /// <summary>Bu sürecin özel çalışma kümesi (bayt); okunamazsa null.</summary>
    public static long? GetPrivateWorkingSetBytes()
    {
        var counters = new ProcessMemoryCountersEx2 { cb = (uint)Marshal.SizeOf<ProcessMemoryCountersEx2>() };
        if (!K32GetProcessMemoryInfo(GetCurrentProcess(), ref counters, counters.cb))
            return null;

        return (long)counters.PrivateWorkingSetSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCountersEx2
    {
        public uint cb;
        public uint PageFaultCount;
        public nuint PeakWorkingSetSize;
        public nuint WorkingSetSize;
        public nuint QuotaPeakPagedPoolUsage;
        public nuint QuotaPagedPoolUsage;
        public nuint QuotaPeakNonPagedPoolUsage;
        public nuint QuotaNonPagedPoolUsage;
        public nuint PagefileUsage;
        public nuint PeakPagefileUsage;
        public nuint PrivateUsage;
        public nuint PrivateWorkingSetSize;
        public ulong SharedCommitUsage;
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool K32GetProcessMemoryInfo(nint process, ref ProcessMemoryCountersEx2 counters, uint size);
}
