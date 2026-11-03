using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Dock;

namespace Koy.App.Features.Dock;

/// <summary>
/// Tek bir dock öğesinin (uygulama simgesi, çalışan durumu, büyütme ölçeği) görünüm modeli.
/// </summary>
public sealed partial class DockItemViewModel : ObservableObject
{
    private readonly DockController _controller;

    public DockItem Model { get; private set; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private ImageSource? _icon;

    [ObservableProperty]
    private double _scale = 1.0;

    public DockItemViewModel(DockItem model, DockController controller)
    {
        Model = model;
        _controller = controller;

        _title = model.Title;
        _isPinned = model.IsPinned;
        _isRunning = model.IsRunning;
        _isActive = model.IsActive;

        if (model.IconData is { Length: > 0 })
        {
            _icon = LoadImage(model.IconData);
        }
    }

    public void Update(DockItem model)
    {
        Model = model;
        Title = model.Title;
        IsPinned = model.IsPinned;
        IsRunning = model.IsRunning;
        IsActive = model.IsActive;

        if (Icon is null && model.IconData is { Length: > 0 })
        {
            Icon = LoadImage(model.IconData);
        }
    }

    [RelayCommand]
    private void Click()
    {
        _controller.HandleItemClick(Model);
    }

    [RelayCommand]
    private void MiddleClick()
    {
        _controller.HandleItemMiddleClick(Model);
    }

    [RelayCommand]
    private void Close()
    {
        _controller.HandleItemClose(Model);
    }

    [RelayCommand]
    private void Terminate()
    {
        _controller.HandleItemTerminate(Model);
    }

    private static BitmapImage? LoadImage(byte[] data)
    {
        try
        {
            using var ms = new MemoryStream(data);
            var image = new BitmapImage();
            image.BeginInit();
            image.DecodePixelWidth = 48;
            image.StreamSource = ms;
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
