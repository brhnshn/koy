using System.Windows;
using System.Windows.Media;

namespace Koy.UiSnapshots;

/// <summary>Uygulama kaynaklarını (App.xaml'in yaptığı gibi) yükler.</summary>
internal static class SnapshotTheme
{
    public static void Install(Application app)
    {
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Koy;component/Resources/Styles.xaml")
        });
        app.Resources["NotchBackground"] = new SolidColorBrush(Color.FromArgb(0xF0, 0, 0, 0));
        app.Resources["NotchForeground"] = new SolidColorBrush(Colors.White);
    }

    public static void Apply(Application app, string theme)
    {
        // Tema sistemi henüz yok: yalnız koyu çizilir.
    }
}
