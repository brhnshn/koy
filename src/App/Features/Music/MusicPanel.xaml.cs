using System.Windows.Controls;
using System.Windows.Input;

namespace Koy.App.Features.Music;

public partial class MusicPanel : UserControl
{
    public MusicPanel()
    {
        InitializeComponent();
    }

    private void ProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ProgressBar bar && DataContext is MusicViewModel vm)
        {
            var pos = e.GetPosition(bar);
            var fraction = bar.ActualWidth > 0 ? pos.X / bar.ActualWidth : 0;
            vm.SeekCommand.Execute(fraction);
        }
    }
}
