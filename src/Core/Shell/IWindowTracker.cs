namespace Koy.Core.Shell;

/// <summary>
/// Pencere tanıtıcı bilgisi ve ekran konumu.
/// </summary>
public sealed record WindowInfo(nint Handle, WindowRect Bounds);

/// <summary>
/// Ön plan penceresi ve konum değişikliklerini izleyen sözleşme.
/// Win32 tipleri içermez, pencere tanıtıcıları nint ile temsil edilir.
/// </summary>
public interface IWindowTracker : IDisposable
{
    event EventHandler<WindowInfo>? ForegroundWindowChanged;

    WindowInfo? CurrentForeground { get; }

    void Start();

    void Stop();

    bool GetCursorPosition(out int x, out int y);
}
