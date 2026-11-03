namespace Koy.Core.Shell;

/// <summary>
/// Başlat menüsü, sistem tepsisi ve süreç başlatma gibi kabuk eylemleri sözleşmesi.
/// </summary>
public interface IShellActionService
{
    /// <summary>
    /// Windows Başlat menüsünü açmak için Win tuşu girdisi gönderir.
    /// </summary>
    void SendWinKey();

    /// <summary>
    /// Windows Sistem Tepsisi taşma menüsünü açmak için Win+B girdisi gönderir.
    /// </summary>
    void ToggleSystemTray();

    /// <summary>
    /// Belirtilen dosya veya kısayolu çalıştırır.
    /// </summary>
    void Launch(string path, string? arguments = null);
}
