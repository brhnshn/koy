using System.Windows;
using System.Windows.Input;

namespace Koy.App.Features.Wizard;

public partial class SetupWizardWindow : Window
{
    public SetupWizardViewModel ViewModel => (SetupWizardViewModel)DataContext;

    public SetupWizardWindow(SetupWizardViewModel? viewModel = null)
    {
        InitializeComponent();
        DataContext = viewModel ?? new SetupWizardViewModel();

        ViewModel.Completed += () =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Card0_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.SelectedLayoutIndex = 0;
    }

    private void Card1_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.SelectedLayoutIndex = 1;
    }

    private void Card2_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.SelectedLayoutIndex = 2;
    }
}
