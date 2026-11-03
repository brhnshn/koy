using Koy.Core.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Koy.Platform.Shell;

/// <summary>
/// SetWinEventHook kullanarak ön plandaki pencereyi ve konum değişikliklerini izler.
/// Hook delegesini GC'den korumak için alan olarak tutar.
/// </summary>
public sealed class WindowTracker : IWindowTracker
{
    private readonly WINEVENTPROC _hookProc;
    private HWINEVENTHOOK _foregroundHook;
    private HWINEVENTHOOK _locationHook;
    private bool _started;
    private bool _disposed;

    public event EventHandler<WindowInfo>? ForegroundWindowChanged;

    public WindowInfo? CurrentForeground { get; private set; }

    public WindowTracker()
    {
        // Delegasyon alan olarak tutulur; aksi halde Win32 geri çağrısı sırasında GC toplayabilir.
        _hookProc = OnWinEvent;
    }

    public void Start()
    {
        if (_started)
            return;

        _started = true;

        // Ön plan pencere değişimi (EVENT_SYSTEM_FOREGROUND = 0x0003)
        _foregroundHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_SYSTEM_FOREGROUND,
            PInvoke.EVENT_SYSTEM_FOREGROUND,
            default(HMODULE),
            _hookProc,
            0,
            0,
            PInvoke.WINEVENT_OUTOFCONTEXT);

        // Pencere konum/boyut değişimi (EVENT_OBJECT_LOCATIONCHANGE = 0x800B)
        _locationHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
            PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
            default(HMODULE),
            _hookProc,
            0,
            0,
            PInvoke.WINEVENT_OUTOFCONTEXT);

        UpdateForegroundWindow();
    }

    public void Stop()
    {
        if (!_started)
            return;

        _started = false;

        if (_foregroundHook.Value != 0)
        {
            PInvoke.UnhookWinEvent(_foregroundHook);
            _foregroundHook = default;
        }

        if (_locationHook.Value != 0)
        {
            PInvoke.UnhookWinEvent(_locationHook);
            _locationHook = default;
        }
    }

    public bool GetCursorPosition(out int x, out int y)
    {
        if (PInvoke.GetCursorPos(out var pt))
        {
            x = pt.X;
            y = pt.Y;
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    private void OnWinEvent(
        HWINEVENTHOOK hWinEventHook,
        uint @event,
        HWND hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime)
    {
        // Konum değişiminde yalnızca ana pencere (idObject == 0) seviyesindeki hareketleri dikkate al
        if (@event == PInvoke.EVENT_OBJECT_LOCATIONCHANGE)
        {
            if (idObject != 0)
                return;

            // Yalnızca mevcut ön plan penceresi hareket ettiyse güncelle (arka plan pencerelerinin gürültüsünü filtrele)
            if (CurrentForeground is not null && hwnd.Value != CurrentForeground.Handle)
                return;
        }

        UpdateForegroundWindow();
    }

    private void UpdateForegroundWindow()
    {
        var hwnd = PInvoke.GetForegroundWindow();
        if (hwnd.IsNull)
        {
            if (CurrentForeground is not null)
            {
                CurrentForeground = null;
                ForegroundWindowChanged?.Invoke(this, new WindowInfo(nint.Zero, WindowRect.Empty));
            }
            return;
        }

        var bounds = WindowRect.Empty;
        if (!IsShellOrDesktop(hwnd) && PInvoke.GetWindowRect(hwnd, out var wr))
        {
            bounds = new WindowRect(wr.left, wr.top, wr.right, wr.bottom);
        }

        var info = new WindowInfo(hwnd.Value, bounds);
        if (Equals(CurrentForeground, info))
            return;

        CurrentForeground = info;
        ForegroundWindowChanged?.Invoke(this, info);
    }

    private static bool IsShellOrDesktop(HWND hwnd)
    {
        if (hwnd.IsNull || hwnd == PInvoke.GetDesktopWindow() || hwnd == PInvoke.GetShellWindow())
            return true;

        unsafe
        {
            Span<char> buffer = stackalloc char[256];
            fixed (char* ptr = buffer)
            {
                var len = PInvoke.GetClassName(hwnd, ptr, 256);
                if (len > 0)
                {
                    var className = buffer[..len];
                    if (className.SequenceEqual("Progman") ||
                        className.SequenceEqual("WorkerW") ||
                        className.SequenceEqual("Shell_TrayWnd"))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
    }
}
