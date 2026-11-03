using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Settings;

namespace Koy.App.Features.Notch;

public sealed partial class NotchViewModel(AppSettings settings) : ObservableObject
{
    [ObservableProperty]
    private string _title = "Koy";

    public AppSettings Settings { get; } = settings;

    [RelayCommand]
    private static void Quit() => System.Windows.Application.Current.Shutdown();
}
