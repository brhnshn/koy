using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Koy.App.Features.ControlCenter;

public partial class WifiListPage : UserControl
{
    public WifiListPage()
    {
        InitializeComponent();
    }

    private ControlCenterViewModel? ViewModel => DataContext as ControlCenterViewModel;

    // Şifre yalnız satırın bellek alanına gider (WifiRowViewModel.SetPassword); özellik ya da bağlama kullanılmaz.
    private void Password_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox { DataContext: WifiRowViewModel row } box)
            row.SetPassword(box.Password);
    }

    private void Password_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not PasswordBox box)
            return;

        // Kutu açılınca odak verilir; kapanınca (satır değişti, sayfa kapandı) içindeki şifre silinir.
        if (e.NewValue is true)
            box.Dispatcher.BeginInvoke(() => box.Focus(), System.Windows.Threading.DispatcherPriority.Input);
        else
            box.Clear();
    }

    private void Password_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not PasswordBox { DataContext: WifiRowViewModel row })
            return;

        var command = ViewModel?.Wifi.ConnectWithPasswordCommand;
        if (command?.CanExecute(row) == true)
            command.Execute(row);
        e.Handled = true;
    }
}
