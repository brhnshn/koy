namespace Koy.Core.Shortcuts;

public enum HotkeyRegistrationResult
{
    Success,

    /// <summary>Başka bir uygulama (ya da Windows) bu kısayolu zaten tutuyor.</summary>
    AlreadyInUse,

    /// <summary>Başka bir nedenle kaydedilemedi.</summary>
    Failed
}

/// <summary>
/// Sistem çapında kısayol kaydı sözleşmesi. Platform uygulaması RegisterHotKey / UnregisterHotKey kullanır.
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>Kayıtlı bir kısayola basıldı; değer <c>Register</c>'a verilen kimliktir.</summary>
    event EventHandler<int>? HotkeyPressed;

    HotkeyRegistrationResult Register(int id, Hotkey hotkey);

    void Unregister(int id);
}
