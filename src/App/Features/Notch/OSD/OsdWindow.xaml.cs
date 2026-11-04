using System.Windows;
using System.Windows.Interop;
using Koy.Platform.Shell;

namespace Koy.App.Features.Notch.OSD;

public partial class OsdWindow : Window
{
    private nint _hwnd;

    public OsdWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PlaceAtTopCenter();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowStyles.ApplyToolWindowAndNoActivate(_hwnd);
        WindowStyles.ReassertTopmost(_hwnd);
        PlaceAtTopCenter();
    }

    public void PlaceAtTopCenter()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - Width) / 2;
        Top = 0;
    }
}
