using Koy.Platform.Display;
using Microsoft.Win32;

namespace Koy.App;

/// <summary>
/// Sistemin ekran değişikliği olayını (takma, çıkarma, çözünürlük, DPI) <see cref="MonitorService"/>'e bağlar.
/// Pencereler WM_DISPLAYCHANGE ile aynı haberi verir ama o ekranda hiç pencere yoksa (çentik ve dock orada kapalı)
/// kimse duymaz; bu yüzden sistem olayı da dinlenir. Tekrarları servis kendi imzasıyla eler.
/// </summary>
public sealed class DisplayEvents : IDisposable
{
    private readonly MonitorService _service;

    public DisplayEvents(MonitorService service)
    {
        _service = service;
        SystemEvents.DisplaySettingsChanged += OnChanged;
    }

    private void OnChanged(object? sender, EventArgs e) => _service.NotifyDisplayChanged();

    public void Dispose() => SystemEvents.DisplaySettingsChanged -= OnChanged;
}
