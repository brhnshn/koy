using System.Diagnostics;
using System.Runtime.InteropServices;
using Koy.Core.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.Shell.PropertiesSystem;
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
    public event EventHandler<nint>? WindowFlashing;

    private readonly List<HWND> _shellHookWindows = [];
    public uint ShellHookMessage { get; private set; }

    public WindowInfo? CurrentForeground { get; private set; }

    /// <summary>İzleyici çalışıyor mu (paylaşımlı kullanımda ikinci özellik mevcut durumu elle okumalı).</summary>
    public bool IsStarted => _started;

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

    public void RegisterShellHook(nint windowHandle)
    {
        if (windowHandle == 0) return;
        var hwnd = new HWND(windowHandle);
        if (!_shellHookWindows.Contains(hwnd))
        {
            if (PInvoke.RegisterShellHookWindow(hwnd))
            {
                _shellHookWindows.Add(hwnd);
                ShellHookMessage = PInvoke.RegisterWindowMessage("SHELLHOOK");
            }
        }
    }

    public void DeregisterShellHook(nint windowHandle)
    {
        if (windowHandle == 0) return;
        var hwnd = new HWND(windowHandle);
        if (_shellHookWindows.Remove(hwnd))
        {
            PInvoke.DeregisterShellHookWindow(hwnd);
        }
    }

    public void HandleShellHookMessage(nint wParam, nint lParam)
    {
        var shellEvent = wParam.ToInt32();
        const int HSHELL_WINDOWCREATED = 1;
        const int HSHELL_WINDOWDESTROYED = 2;
        const int HSHELL_ACTIVATESHELLWINDOW = 3;
        const int HSHELL_WINDOWACTIVATED = 4;
        const int HSHELL_REDRAW = 6;
        const int HSHELL_FLASH = 0x8006;
        const int HSHELL_RUDEAPPACTIVATED = 0x8004;

        if (shellEvent == HSHELL_FLASH)
        {
            WindowFlashing?.Invoke(this, lParam);
            WindowsChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (shellEvent is HSHELL_WINDOWCREATED or HSHELL_WINDOWDESTROYED or HSHELL_REDRAW)
        {
            WindowsChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (shellEvent is HSHELL_WINDOWACTIVATED or HSHELL_RUDEAPPACTIVATED or HSHELL_ACTIVATESHELLWINDOW)
        {
            UpdateForegroundWindow();
        }
    }

    public void Stop()
    {
        if (!_started)
            return;

        _started = false;

        foreach (var hwnd in _shellHookWindows)
        {
            PInvoke.DeregisterShellHookWindow(hwnd);
        }
        _shellHookWindows.Clear();

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

            // Gizlenmiş (cloaked) pencereler ekranda yok: arka plandaki mağaza uygulamaları, "Windows Giriş Deneyimi" gibi.
            if (IsCloaked(hwnd))
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

            // Mağaza (UWP) penceresinin sahibi ApplicationFrameHost.exe'dir ve bütün Mağaza uygulamalarına ortaktır.
            // Gerçek uygulama sürecini çocuk CoreWindow'dan bul; bulunamazsa 0 (askıdaki uygulama): "Sonlandır" çalışmaz
            // ama başka uygulamaları da kapatmaz.
            var isFrameWindow = IsApplicationFrameWindow(hwnd);
            if (isFrameWindow)
                pid = ResolveUwpProcessId(hwnd, pid);

            var procPath = GetProcessPath(pid);

            // Mağaza uygulamaları ApplicationFrameHost.exe içinde çalışır; hepsi aynı exe'ye düşüp genel simgeyle
            // tek öğede toplanmasın diye uygulama kimliğinden (AUMID) kabuk yolu üretilir. Bu yol hem simge hem başlatma için geçerli.
            if (isFrameWindow && GetAppUserModelId(hwnd) is { Length: > 0 } aumid)
                procPath = @"shell:AppsFolder\" + aumid;

            var iconData = !string.IsNullOrEmpty(procPath) ? IconHelper.GetIconBytes(procPath) : null;
            var bounds = new WindowRect(wr.left, wr.top, wr.right, wr.bottom);
            windows.Add(new OpenWindowInfo(hwnd.Value, title, procPath, (int)pid, bounds, iconData));

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
            // Ortak çatı süreçleri (ApplicationFrameHost, explorer…) öldürülürse ilgisiz uygulamalar da kapanır.
            if (!ProcessTerminationPolicy.CanTerminate(processId, proc.ProcessName, Environment.ProcessId))
                return;

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
        // Yerel geri çağrıdan sızan istisna süreci düşürür; logla ve yut.
        try
        {
            HandleWinEvent(@event, hwnd, idObject, idChild);
        }
        catch (Exception ex)
        {
            Koy.Core.Diagnostics.CrashLog.Write("WindowTracker.OnWinEvent", ex);
        }
    }

    private void HandleWinEvent(uint @event, HWND hwnd, int idObject, int idChild)
    {
        if (hwnd.IsNull)
            return;

        // Koy'un kendi pencerelerinin (dock, açılır pencereler, önizleme kutucuğu, thumbnail host, menüler)
        // olayları yoksayılır; aksi halde popup açılışında döngüsel tetiklenme (flicker) ve kilitlenme oluşur.
        uint pid = 0;
        unsafe { PInvoke.GetWindowThreadProcessId(hwnd, &pid); }
        if (pid == (uint)Environment.ProcessId)
            return;

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

    private static unsafe string? GetProcessPath(uint pid)
    {
        if (pid == 0)
            return null;

        // Process.MainModule yönetici olarak çalışan süreçlerde erişim hatası verir; sınırlı sorgu hakkı yeterli.
        using var process = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process.IsInvalid)
            return null;

        Span<char> buffer = stackalloc char[1024];
        var size = (uint)buffer.Length;
        fixed (char* pBuffer = buffer)
        {
            if (!PInvoke.QueryFullProcessImageName(process, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, pBuffer, ref size))
                return null;
        }

        return buffer[..(int)size].ToString();
    }

    private static unsafe bool IsCloaked(HWND hwnd)
    {
        int cloaked = 0;
        var hr = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &cloaked, sizeof(int));
        return hr.Succeeded && cloaked != 0;
    }

    private static unsafe uint ResolveUwpProcessId(HWND frameWindow, uint framePid)
    {
        uint result = 0;
        PInvoke.EnumChildWindows(frameWindow, (HWND child, LPARAM _) =>
        {
            uint childPid = 0;
            PInvoke.GetWindowThreadProcessId(child, &childPid);
            if (childPid != 0 && childPid != framePid)
            {
                result = childPid;
                return false;
            }

            return true;
        }, 0);

        return result;
    }

    private static unsafe bool IsApplicationFrameWindow(HWND hwnd)
    {
        Span<char> buffer = stackalloc char[64];
        fixed (char* ptr = buffer)
        {
            var len = PInvoke.GetClassName(hwnd, ptr, buffer.Length);
            return len > 0 && buffer[..len].SequenceEqual("ApplicationFrameWindow");
        }
    }

    // PKEY_AppUserModel_ID
    private static readonly PROPERTYKEY AppUserModelIdKey = new()
    {
        fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        pid = 5
    };

    private static unsafe string? GetAppUserModelId(HWND hwnd)
    {
        try
        {
            PInvoke.SHGetPropertyStoreForWindow(hwnd, typeof(IPropertyStore).GUID, out var obj).ThrowOnFailure();
            if (obj is not IPropertyStore store)
                return null;

            try
            {
                store.GetValue(AppUserModelIdKey, out var value);
                try
                {
                    PInvoke.PropVariantToStringAlloc(value, out var str).ThrowOnFailure();
                    try
                    {
                        return str.ToString();
                    }
                    finally
                    {
                        PInvoke.CoTaskMemFree(str.Value);
                    }
                }
                finally
                {
                    PInvoke.PropVariantClear(ref value);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(store);
            }
        }
        catch
        {
            return null;
        }
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
