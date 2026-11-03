using System.Windows;
using System.Windows.Interop;
using Koy.Platform.Shell;

namespace Koy.App.Features.Settings;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        BackdropService.ApplyBackdrop(hwnd, WindowBackdropType.Mica);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
