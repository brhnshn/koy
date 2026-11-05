using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Koy.Core.Dock;

namespace Koy.App.Features.Dock.Recents;

/// <summary>
/// Dock'taki son kullanılanlar yığın düğmesi ve açılan liste (yüzen ve tam genişlik dock'ta ortak).
/// Liste yalnız tıklanınca okunur; seçilen dosya Windows'un ilişkili uygulamasıyla açılır.
/// </summary>
public partial class RecentsButton : UserControl
{
    private DockViewModel? _subscribed;

    public RecentsButton()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Subscribe();
        IsVisibleChanged += (_, _) => SyncPopup();
        RecentsPopup.Closed += OnPopupClosed;
    }

    private DockViewModel? ViewModel => DataContext as DockViewModel;

    // Tepsi düğmesiyle aynı sebep: iki dock biçiminde birer düğme var, yalnız görünen olanın penceresi açılır.
    private void Subscribe()
    {
        if (_subscribed is not null)
            _subscribed.PropertyChanged -= OnViewModelPropertyChanged;

        _subscribed = ViewModel;
        if (_subscribed is not null)
            _subscribed.PropertyChanged += OnViewModelPropertyChanged;

        SyncPopup();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DockViewModel.IsRecentsOpen))
            SyncPopup();
    }

    private void SyncPopup() => RecentsPopup.IsOpen = IsVisible && ViewModel?.IsRecentsOpen == true;

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        // Dışarı tıklanınca (StaysOpen=False) Popup kendini kapatır; durum ViewModel'e yansısın.
        if (ViewModel is { IsRecentsOpen: true } vm)
            vm.IsRecentsOpen = false;
    }

    private void OnRecentClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: RecentFile file })
            ViewModel?.OpenRecent(file);

        e.Handled = true;
    }
}
