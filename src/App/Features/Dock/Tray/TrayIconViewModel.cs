using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Koy.Core.Tray;

namespace Koy.App.Features.Dock.Tray;

/// <summary>Dock'un tepsi penceresindeki tek bir uygulama simgesi.</summary>
public sealed partial class TrayIconViewModel : ObservableObject
{
    public TrayIconKey Key { get; }

    /// <summary>Görüntünün hangi simge kopyasından çözüldüğü; değişince görüntü yeniden çözülür.</summary>
    public nint IconHandle { get; private set; }

    private bool _decoded;

    /// <summary>Boş ipucunda null: WPF boş bir ipucu kutusu göstermesin.</summary>
    [ObservableProperty]
    private string? _tooltip;

    [ObservableProperty]
    private ImageSource? _icon;

    public TrayIconViewModel(TrayIcon model)
    {
        Key = model.Key;
        _tooltip = NullIfEmpty(model.Tooltip);
    }

    public void Update(TrayIcon model, Func<TrayIconKey, byte[]?> loadImage)
    {
        Tooltip = NullIfEmpty(model.Tooltip);
        // Çözülemeyen simge de bir kez denenir; aynı tanıtıcıyla her eşitlemede GDI ve PNG işi tekrarlanmaz.
        if (_decoded && model.IconHandle == IconHandle)
            return;

        _decoded = true;
        IconHandle = model.IconHandle;
        Icon = Decode(loadImage(Key));
    }

    private static string? NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private static BitmapImage? Decode(byte[]? png)
    {
        if (png is not { Length: > 0 })
            return null;

        try
        {
            using var stream = new MemoryStream(png);
            var image = new BitmapImage();
            image.BeginInit();
            // Simge 20 DIP gösterilir; %200 ölçekte de net kalsın diye 40 px'e kadar.
            image.DecodePixelWidth = 40;
            image.StreamSource = stream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
