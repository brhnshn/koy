using System.Diagnostics;
using Koy.Core.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// SetWinEventHook kullanarak ön plandaki pencereyi, konum değişikliklerini ve açık pencereleri izler.
/// Hook delegesini GC'den korumak için alan olarak tutar.
/// </summary>
public sealed class WindowTracker : IWindowTracker
{
    private readonly WINEVENTPROC _hookProc;
    private HWINEVENTHOOK _foregroundHook;
    private HWINEVENTHOOK _locationHook;
    private HWINEVENTHOOK _lifecycleHook;
    private bool _started;
    private bool _disposed;

    public event EventHandler<WindowInfo>? ForegroundWindowChanged;
    public event EventHandler? WindowsChanged;

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

        // Pencere oluşturulma, yok edilme, gösterilme, gizlenme olayları (0x8000..0x8003)
        _lifecycleHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_CREATE,
            PInvoke.EVENT_OBJECT_HIDE,
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

        if (_lifecycleHook.Value != 0)
        {
            PInvoke.UnhookWinEvent(_lifecycleHook);
            _lifecycleHook = default;
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

    public unsafe IReadOnlyList<OpenWindowInfo> GetOpenWindows()
    {
        var windows = new List<OpenWindowInfo>();

        PInvoke.EnumWindows((HWND hwnd, LPARAM _) =>
        {
            if (hwnd.IsNull || !PInvoke.IsWindowVisible(hwnd))
                return true;

            // Sahip penceresi olanları (diyaloglar, alt pencereler) filtrele
            var owner = PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
            if (!owner.IsNull)
                return true;

            // Araç çubuğu pencerelerini (WS_EX_TOOLWINDOW) filtrele
            var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            if ((exStyle & WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) != 0)
                return true;

            if (IsShellOrDesktop(hwnd))
                return true;

            // Pencere başlığı oku
            Span<char> titleBuffer = stackalloc char[256];
            int titleLen;
            fixed (char* pTitle = titleBuffer)
            {
                titleLen = PInvoke.GetWindowText(hwnd, pTitle, 256);
            }

            if (titleLen == 0)
                return true;

            var title = titleBuffer[..titleLen].ToString();

            // Boyut kontrolü
            if (!PInvoke.GetWindowRect(hwnd, out var wr) || wr.right <= wr.left || wr.bottom <= wr.top)
                return true;

            // Süreç bilgisi
            uint pid = 0;
            PInvoke.GetWindowThreadProcessId(hwnd, &pid);
            string? procPath = null;
            if (pid != 0)
            {
                try
                {
                    using var proc = Process.GetProcessById((int)pid);
                    procPath = proc.MainModule?.FileName;
                }
                catch
                {
                    // Bazı sistem süreçlerinin modül bilgisine erişim kısıtlıdır
                }
            }

            var bounds = new WindowRect(wr.left, wr.top, wr.right, wr.bottom);
            windows.Add(new OpenWindowInfo(hwnd.Value, title, procPath, (int)pid, bounds));

            return true;
        }, 0);

        return windows;
    }

    public void ActivateWindow(nint handle)
    {
        if (handle == 0) return;
        var hwnd = new HWND(handle);

        if (PInvoke.IsIconic(hwnd))
            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);

        PInvoke.SetForegroundWindow(hwnd);
        PInvoke.BringWindowToTop(hwnd);
    }

    public void MinimizeWindow(nint handle)
    {
        if (handle == 0) return;
        PInvoke.ShowWindow(new HWND(handle), SHOW_WINDOW_CMD.SW_MINIMIZE);
    }

    public void CloseWindow(nint handle)
    {
        if (handle == 0) return;
        PInvoke.PostMessage(new HWND(handle), 0x0010 /* WM_CLOSE */, default, default);
    }

    public void TerminateProcess(int processId)
    {
        if (processId <= 0) return;
        try
        {
            using var proc = Process.GetProcessById(processId);
            proc.Kill();
        }
        catch
        {
            // Süreç zaten sonlanmış olabilir
        }
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
        // Yaşam döngüsü olayları (pencere açılma / kapanma)
        if (@event is PInvoke.EVENT_OBJECT_CREATE or PInvoke.EVENT_OBJECT_DESTROY
                   or PInvoke.EVENT_OBJECT_SHOW or PInvoke.EVENT_OBJECT_HIDE)
        {
            if (idObject == 0 && idChild == 0)
            {
                WindowsChanged?.Invoke(this, EventArgs.Empty);
            }
            return;
        }

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
                        className.SequenceEqual("Shell_TrayWnd") ||
                        className.SequenceEqual("Shell_SecondaryTrayWnd"))
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
