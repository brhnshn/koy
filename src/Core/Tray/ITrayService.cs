namespace Koy.Core.Tray;

/// <summary>
/// Uygulamaların sistem tepsisi simgelerini toplayan servis. Dock Windows görev çubuğunu gizlediğinde
/// tepsiye ulaşmanın tek yolu budur.
/// </summary>
public interface ITrayService : IDisposable
{
    /// <summary>Simge listesi değişti. Arka plan iş parçacığından gelebilir.</summary>
    event EventHandler? IconsChanged;

    /// <summary>Gösterilecek simgeler (Koy'un kendi simgeleri hariç), ekleniş sırasıyla.</summary>
    IReadOnlyList<TrayIcon> GetVisibleIcons();

    /// <summary>Simgenin PNG görüntüsü; yoksa null.</summary>
    byte[]? GetIconImage(TrayIconKey key);

    /// <summary>Uygulamaya tepsi tıklamasını iletir ve menüsünü öne getirebilmesine izin verir.</summary>
    void SendMouse(TrayIconKey key, TrayMouseAction action, int cursorX, int cursorY);

    /// <summary>Sahibi kapanmış simgeleri temizler.</summary>
    void RemoveDeadIcons();

    void Start();

    void Stop();
}
