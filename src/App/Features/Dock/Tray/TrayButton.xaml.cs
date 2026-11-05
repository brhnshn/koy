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

    public TrayButton()
    {
        InitializeComponent();
    }

    private DockViewModel? ViewModel => DataContext as DockViewModel;

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
