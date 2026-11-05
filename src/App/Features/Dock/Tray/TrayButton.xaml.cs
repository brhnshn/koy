using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Koy.Core.Tray;

namespace Koy.App.Features.Dock.Tray;

/// <summary>
/// Dock'taki sistem tepsisi düğmesi ve açılan simge penceresi (yüzen ve tam genişlik dock'ta ortak).
/// </summary>
public partial class TrayButton : UserControl
{
    private bool _isDoubleClick;
    private DockViewModel? _subscribed;

    public TrayButton()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Subscribe();
        IsVisibleChanged += (_, _) => SyncPopup();
        TrayPopup.Closed += OnPopupClosed;
    }

    private DockViewModel? ViewModel => DataContext as DockViewModel;

    // Yüzen ve tam genişlik dock'ta birer düğme var, biri hep gizli. Popup'ı XAML'de IsTrayPopupOpen'a bağlamak
    // gizli olanın penceresini de açıyordu (Windows tepsisinin yerinde ikinci bir pencere). Yalnız görünen açılır.
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
        if (e.PropertyName == nameof(DockViewModel.IsTrayPopupOpen))
            SyncPopup();
    }

    private void SyncPopup() => TrayPopup.IsOpen = IsVisible && ViewModel?.IsTrayPopupOpen == true;

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        // Dışarı tıklanınca (StaysOpen=False) Popup kendini kapatır; durum ViewModel'e yansısın.
        if (ViewModel is { IsTrayPopupOpen: true } vm)
            vm.IsTrayPopupOpen = false;
    }

    private void OnIconLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Çift tıklama bırakmada gönderilir: Windows görev çubuğu da DBLCLK ardından LBUTTONUP yollar.
        _isDoubleClick = e.ClickCount >= 2;
        e.Handled = true;
    }

    private void OnIconLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TrayIconViewModel icon })
            ViewModel?.SendTrayMouse(icon, _isDoubleClick ? TrayMouseAction.LeftDoubleClick : TrayMouseAction.LeftClick);

        _isDoubleClick = false;
        e.Handled = true;
    }

    private void OnIconRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TrayIconViewModel icon })
            ViewModel?.SendTrayMouse(icon, TrayMouseAction.RightClick);

        e.Handled = true;
    }
}
