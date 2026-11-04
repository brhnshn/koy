using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Koy.App.Resources;

/// <summary>
/// Dinamik arayüz yerelleştirme sağlayıcısı.
/// Dil değiştirildiğinde tüm XAML bağlamalarını anında yeniler.
/// Kullanım: Text="{Binding [KeyName], Source={x:Static res:Loc.Instance}}"
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key]
    {
        get => Strings.Get(key);
        set { } // WPF Run.Text gibi TwoWay bağlama girişimlerini sessizce kabul et
    }

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
