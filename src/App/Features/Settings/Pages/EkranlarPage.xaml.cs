using System.Windows;
using System.Windows.Controls;

namespace Koy.App.Features.Settings.Pages;

public partial class EkranlarPage : UserControl
{
    public EkranlarPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private DisplaysViewModel? Displays => (DataContext as KoyWindowViewModel)?.Displays;

    // Ekran takma/çıkarma dinlemesi yalnız sayfa canlıyken (pencere açıkken) çalışır.
    private void OnLoaded(object sender, RoutedEventArgs e) => Displays?.Activate();

    private void OnUnloaded(object sender, RoutedEventArgs e) => Displays?.Deactivate();
}
