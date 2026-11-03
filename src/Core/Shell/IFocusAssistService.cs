namespace Koy.Core.Shell;

/// <summary>
/// Windows Odak Yardımı / Rahatsız Etme sözleşmesi.
/// </summary>
public interface IFocusAssistService : IDisposable
{
    bool IsActive { get; }
    event EventHandler<bool>? StateChanged;

    void OpenSettings();
}
