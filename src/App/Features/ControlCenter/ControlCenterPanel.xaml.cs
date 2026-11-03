using System.Windows;
using System.Windows.Controls;

namespace Koy.App.Features.ControlCenter;

public partial class ControlCenterPanel : UserControl
{
    private ControlCenterViewModel? ViewModel => DataContext as ControlCenterViewModel;

    public ControlCenterPanel()
    {
        InitializeComponent();
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeSlider.IsMouseCaptureWithin || VolumeSlider.IsFocused)
        {
            ViewModel?.OnVolumeSliderChanged(e.NewValue);
        }
    }

    private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessSlider.IsMouseCaptureWithin || BrightnessSlider.IsFocused)
        {
            ViewModel?.OnBrightnessSliderChanged(e.NewValue);
        }
    }
}
