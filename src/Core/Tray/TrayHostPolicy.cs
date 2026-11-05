namespace Koy.Core.Tray;

/// <summary>
/// Koy'un kendi tepsi penceresini (Shell_TrayWnd) ne zaman kuracağına ve hangi simgeleri alacağına dair kararlar.
/// Saf mantık: Win32 çağrısı yok, test edilir (AGENTS.md #11).
/// </summary>
public static class TrayHostPolicy
{
    /// <summary>
    /// Tepsi barındırma yalnız Windows görev çubuğu gerçekten gizliyken anlamlıdır: dock'un gizlediği tepsiye
    /// ulaşmanın tek yolu budur. Görev çubuğu görünürken (ayar kapalı ya da dock penceresi yok) Windows'un
    /// tepsisine dokunulmaz; aksi halde iki pencere Shell_TrayWnd sahipliği için çakışırdı.
    /// </summary>
    /// <param name="hideWindowsTaskbar">"Windows görev çubuğunu gizle" ayarı.</param>
    /// <param name="showTray">"Sistem tepsisini dock'ta göster" ayarı.</param>
    /// <param name="dockWindowCount">Şu an açık dock penceresi sayısı (görev çubuğu yalnız bunlar varken gizlenir).</param>
    public static bool ShouldHost(bool hideWindowsTaskbar, bool showTray, int dockWindowCount) =>
        hideWindowsTaskbar && showTray && dockWindowCount > 0;

    /// <summary>
    /// Simgenin sahibi Koy'un kendisiyse (dock kapalıyken menüye erişim için kurduğu tepsi simgesi) dock'un
    /// tepsi penceresinde gösterilmez; kayıt yine de Windows'a iletilir. Sahip penceresi bilinmiyorsa (0) süzülmez.
    /// </summary>
    public static bool IsOwnIcon(uint ownerProcessId, uint ownProcessId) =>
        ownerProcessId != 0 && ownerProcessId == ownProcessId;
}
