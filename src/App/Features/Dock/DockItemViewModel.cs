using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Animation;
using Koy.Core.Dock;

namespace Koy.App.Features.Dock;

/// <summary>
/// Tek bir dock öğesinin (uygulama simgesi, çalışan durumu, büyütme ölçeği) görünüm modeli.
/// </summary>
public sealed partial class DockItemViewModel : ObservableObject
{
    private readonly DockController _controller;
    private readonly Spring _scaleSpring = new(1.0) { Stiffness = 320, Damping = 26 };

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
    private bool _isFlashing;

    [ObservableProperty]
    private bool _isAttention;

    [ObservableProperty]
    private int _badgeCount;

    [ObservableProperty]
    private bool _hasBadge;

    [ObservableProperty]
    private bool _isMultiWindow;

    [ObservableProperty]
    private IReadOnlyList<DockWindowEntry> _windows = [];

    [ObservableProperty]
    private ImageSource? _icon;

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _iconSize = 28.0;

    [ObservableProperty]
    private double _itemSize = 38.0;

    public bool HasOpenWindows => Windows.Count > 0;
    public DockWindowEntry? SingleWindow => Windows.Count == 1 ? Windows[0] : null;

    public DockItemViewModel(DockItem model, DockController controller)
    {
        Model = model;
        _controller = controller;

        _title = model.Title;
        _isPinned = model.IsPinned;
        _isRunning = model.IsRunning;
        _isActive = model.IsActive;
        _isFlashing = model.IsFlashing;
        _isAttention = model.IsFlashing;
        _badgeCount = model.BadgeCount;
        _hasBadge = model.BadgeCount > 0;
        _windows = model.Windows ?? [];
        _isMultiWindow = _windows.Count > 1;

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
        IsFlashing = model.IsFlashing;
        IsAttention = model.IsFlashing;
        BadgeCount = model.BadgeCount;
        HasBadge = model.BadgeCount > 0;
        var newWindows = model.Windows ?? [];
        if (!Windows.SequenceEqual(newWindows))
        {
            Windows = newWindows;
            IsMultiWindow = Windows.Count > 1;
            OnPropertyChanged(nameof(HasOpenWindows));
            OnPropertyChanged(nameof(SingleWindow));
        }

        if (Icon is null && model.IconData is { Length: > 0 })
        {
            Icon = LoadImage(model.IconData);
        }
    }

    public void SetTargetScale(double target)
    {
        _scaleSpring.Target = target;
    }

    public bool StepScale(double dt)
    {
        var moving = _scaleSpring.Step(dt);
        Scale = _scaleSpring.Value;
        return moving;
    }

    public void SnapScale(double scale)
    {
        _scaleSpring.SnapTo(scale);
        Scale = scale;
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

    [RelayCommand]
    private void ActivateSpecificWindow(nint handle)
    {
        _controller.ActivateWindow(handle);
    }

    [RelayCommand]
    private void CloseSpecificWindow(nint handle)
    {
        _controller.CloseWindow(handle);
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
