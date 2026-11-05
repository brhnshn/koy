using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Koy.App.Features.Settings;

public partial class KoyWindow : Window
{
    private const int WmDisplayChange = 0x007E;

    private static KoyWindow? _instance;

    public static KoyWindow Instance => _instance ??= new KoyWindow();

    public KoyWindowViewModel ViewModel => (KoyWindowViewModel)DataContext;

    public KoyWindow(KoyWindowViewModel? viewModel = null)
    {
        InitializeComponent();
        DataContext = viewModel ?? new KoyWindowViewModel();
    }

    public static void ShowInstance(int pageIndex = 0)
    {
        var win = Instance;
        win.ViewModel.LoadFromSettings();
        win.ViewModel.SelectPage(pageIndex);
        win.ContentScrollViewer?.ScrollToTop();

        if (win.WindowState == WindowState.Minimized)
        {
            win.WindowState = WindowState.Normal;
        }

        win.Show();
        win.Topmost = true;
        win.Activate();
        win.Topmost = false;
        win.Focus();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Pencere açıkken ekran takılır ya da çıkarılırsa Ekranlar haritası beklemeden güncellensin.
        (PresentationSource.FromVisual(this) as HwndSource)?.AddHook(WndProc);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmDisplayChange)
            ViewModel.RefreshMonitors();

        return 0;
    }

    private void Navigate(int index)
    {
        ViewModel.SelectPage(index);
        ContentScrollViewer?.ScrollToTop();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveSettingsCommand.Execute(null);
        Hide();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Pencereyi kapatmak Koy'u kapatmaz; gizler
        e.Cancel = true;
        ViewModel.SaveSettingsCommand.Execute(null);
        Hide();
    }

    private void NavGenel_Click(object sender, RoutedEventArgs e) => Navigate(0);
    private void NavCentik_Click(object sender, RoutedEventArgs e) => Navigate(1);
    private void NavDock_Click(object sender, RoutedEventArgs e) => Navigate(2);
    private void NavEkranlar_Click(object sender, RoutedEventArgs e) => Navigate(3);
    private void NavGorunum_Click(object sender, RoutedEventArgs e) => Navigate(4);
    private void NavKisayollar_Click(object sender, RoutedEventArgs e) => Navigate(5);
    private void NavPerformans_Click(object sender, RoutedEventArgs e) => Navigate(6);
    private void NavHakkinda_Click(object sender, RoutedEventArgs e) => Navigate(7);
}
