using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Settings;
using Koy.Platform.Shell;

namespace Koy.App.Features.Wizard;

public sealed partial class SetupWizardViewModel : ObservableObject
{
    private readonly SettingsStore _store;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    private int _currentStep = 1;

    [ObservableProperty]
    private int _selectedLayoutIndex; // 0: Çentik ve dock, 1: Yalnız çentik, 2: Yalnız dock

    [ObservableProperty]
    private string _selectedLanguage = "tr";

    [ObservableProperty]
    private bool _runAtStartup = true;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool CanGoBack => CurrentStep > 1;
    public string NextButtonText => Koy.App.Resources.Strings.Get(CurrentStep == 3 ? "WizStart" : "WizContinue");

    public event Action? Completed;

    public SetupWizardViewModel(SettingsStore? store = null)
    {
        _store = store ?? new SettingsStore(SettingsStore.DefaultPath);
    }

    [RelayCommand]
    private void Next()
    {
        if (CurrentStep < 3)
        {
            CurrentStep++;
        }
        else
        {
            Complete();
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    [RelayCommand]
    private void Complete()
    {
        var settings = _store.Load();

        var notchEnabled = SelectedLayoutIndex is 0 or 1;
        var dockEnabled = SelectedLayoutIndex is 0 or 2;

        var updated = settings with
        {
            FirstRunCompleted = true,
            Language = SelectedLanguage,
            NotchEnabled = notchEnabled,
            DockEnabled = dockEnabled,
            HideWindowsTaskbar = dockEnabled
        };

        _store.Save(updated);

        StartupService.SetEnabled(RunAtStartup);

        Completed?.Invoke();
    }
}
