namespace Koy.Core.Dock;

/// <summary>
/// Dock üzerinde gösterilen bir uygulamanın (sabitli, çalışan veya her ikisi) durum modeli.
/// Win32 veya WPF tipi içermez, saf C# modelidir.
/// </summary>
public sealed record DockItem(
    string Id,
    string Title,
    string? FilePath,
    string? TargetPath,
    byte[]? IconData,
    bool IsPinned,
    bool IsRunning,
    bool IsActive,
    IReadOnlyList<nint> WindowHandles,
    int ProcessId = 0);
