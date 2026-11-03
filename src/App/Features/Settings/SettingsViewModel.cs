using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Settings;
using Koy.Core.Visibility;
using Koy.Platform.Shell;

namespace Koy.App.Features.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;

    [ObservableProperty]
    private string _language;

    [ObservableProperty]
    private bool _runAtStartup;

    [ObservableProperty]
    private VisibilityMode _notchVisibility;

    [ObservableProperty]
    private bool _enableMusic = true;

    [ObservableProperty]
    private bool _enableControlCenter = true;

    [ObservableProperty]
    private bool _enableStatus = true;

    [ObservableProperty]
    private bool _enableCalendar = true;

    [ObservableProperty]
    private bool _enableShelf = true;

    [ObservableProperty]
    private string _accentColor;

    [ObservableProperty]
    private string _appVersion = "0.1.0-alpha";

    public event Action? SettingsSaved;

    public SettingsViewModel(SettingsStore? store = null)
    {
        _store = store ?? new SettingsStore(SettingsStore.DefaultPath);
        var settings = _store.Load();

        _language = settings.Language;
        _runAtStartup = StartupService.IsEnabled;
        _notchVisibility = settings.NotchVisibility;
        _accentColor = settings.AccentColor ?? "#3B82F6";
    }

    [RelayCommand]
    private void Save()
    {
        StartupService.SetEnabled(RunAtStartup);

        var current = _store.Load();
        var updated = current with
        {
            Language = Language,
            NotchVisibility = NotchVisibility,
            AccentColor = AccentColor
        };

        _store.Save(updated);
        SettingsSaved?.Invoke();
    }

    [RelayCommand]
    private void CopyDiagnostics()
    {
        var os = Environment.OSVersion.VersionString;
        var dotNet = Environment.Version.ToString();
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var memMb = process.PrivateMemorySize64 / (1024.0 * 1024.0);

        var diag = $"Koy v{AppVersion}\nOS: {os}\n.NET: {dotNet}\nPrivate Memory: {memMb:N1} MB\nProcess ID: {process.Id}";
        try
        {
            Clipboard.SetText(diag);
        }
        catch
        {
        }
    }
}
