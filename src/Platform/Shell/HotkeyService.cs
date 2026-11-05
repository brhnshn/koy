using Koy.Core.Shortcuts;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Koy.Platform.Shell;

/// <summary>
/// RegisterHotKey / UnregisterHotKey sarmalayıcısı. Kısayollar verilen pencereye WM_HOTKEY olarak gelir;
/// pencere mesajlarını dinleyen taraf (App) <see cref="HandleMessage"/> çağırır, bu sınıf pencere oluşturmaz (WPF'e bağımlı olmaz).
/// </summary>
public sealed class HotkeyService : IHotkeyService
{
    public const int WmHotkey = 0x0312;

    // MOD_NOREPEAT: tuşa basılı tutunca eylem tekrar tekrar tetiklenmesin.
    private const uint ModNoRepeat = 0x4000;
    private const int ErrorHotkeyAlreadyRegistered = 1409;

    private readonly HWND _hwnd;
    private readonly HashSet<int> _registeredIds = [];
    private bool _disposed;

    public event EventHandler<int>? HotkeyPressed;

    public HotkeyService(nint windowHandle)
    {
        _hwnd = new HWND(windowHandle);
    }

    public HotkeyRegistrationResult Register(int id, Hotkey hotkey)
    {
        if (_disposed || _hwnd.IsNull || !hotkey.IsValid)
            return HotkeyRegistrationResult.Failed;

        // Aynı kimlik yeniden kaydedilirse önce eskisi kaldırılır.
        Unregister(id);

        var modifiers = (HOT_KEY_MODIFIERS)((uint)hotkey.Modifiers | ModNoRepeat);
        if (PInvoke.RegisterHotKey(_hwnd, id, modifiers, (uint)hotkey.VirtualKey))
        {
            _registeredIds.Add(id);
            return HotkeyRegistrationResult.Success;
        }

        var error = System.Runtime.InteropServices.Marshal.GetLastPInvokeError();
        return error == ErrorHotkeyAlreadyRegistered
            ? HotkeyRegistrationResult.AlreadyInUse
            : HotkeyRegistrationResult.Failed;
    }

    public void Unregister(int id)
    {
        if (!_registeredIds.Remove(id))
            return;

        PInvoke.UnregisterHotKey(_hwnd, id);
    }

    /// <summary>Pencere işlemcisinden çağrılır; WM_HOTKEY ise olayı yükseltir ve true döner.</summary>
    public bool HandleMessage(int msg, nint wParam)
    {
        if (msg != WmHotkey)
            return false;

        var id = (int)wParam;
        if (_registeredIds.Contains(id))
            HotkeyPressed?.Invoke(this, id);

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var id in _registeredIds.ToList())
            PInvoke.UnregisterHotKey(_hwnd, id);
        _registeredIds.Clear();
    }
}
