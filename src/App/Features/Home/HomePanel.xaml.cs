using System.Windows.Controls;
using System.Windows.Input;

namespace Koy.App.Features.Home;

public partial class HomePanel : UserControl
{
    public HomePanel()
    {
        InitializeComponent();
    }

    private void ProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ProgressBar bar && DataContext is HomeViewModel vm)
        {
            var fraction = bar.ActualWidth > 0 ? e.GetPosition(bar).X / bar.ActualWidth : 0;
            vm.Music.SeekCommand.Execute(fraction);
        }
    }
}
