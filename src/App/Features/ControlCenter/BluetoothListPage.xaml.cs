using System.Windows;
using System.Windows.Controls;

namespace Koy.App.Features.ControlCenter;

public partial class BluetoothListPage : UserControl
{
    public BluetoothListPage()
    {
        InitializeComponent();
    }

    private void MoreSettings_Click(object sender, RoutedEventArgs e) =>
        (DataContext as ControlCenterViewModel)?.BluetoothDevices.OpenSettings();
}
