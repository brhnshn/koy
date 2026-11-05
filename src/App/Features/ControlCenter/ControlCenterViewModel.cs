using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Audio;
using Koy.Core.Display;
using Koy.Core.Radio;
using Koy.Core.Settings;
using Koy.Core.Shell;

namespace Koy.App.Features.ControlCenter;

public sealed partial class ControlCenterViewModel : ObservableObject, IDisposable
{
    private readonly Func<IAudioService> _audioFactory;
    private readonly Func<IRadioService> _radioFactory;
    private readonly Func<IBrightnessService> _brightnessFactory;
    private readonly IFocusAssistService _focusAssistService;
    private IAudioService? _audioService;
    private IRadioService? _radioService;
    private IBrightnessService? _brightnessService;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;
    private bool _disposed;

    [ObservableProperty]
    private bool _isWiFiOn;

    [ObservableProperty]
    private bool _isBluetoothOn;

    [ObservableProperty]
    private bool _isDoNotDisturbOn;

    [ObservableProperty]
    private float _volume;

    [ObservableProperty]
    private int _volumePercent;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private int _brightness;

    [ObservableProperty]
    private bool _isBrightnessSupported;

    // Servisler panel ilk açıldığında kurulur (AGENTS.md #5): WMI parlaklık sorgusu, CoreAudio ve
    // radyo erişimi açılışta birkaç MB tutuyor ve panel hiç açılmayabilir. Kurulduktan sonra
    // notch kapanana kadar yaşar ve bu ViewModel tarafından bırakılır.
    public ControlCenterViewModel(
        Func<IAudioService> audioFactory,
        Func<IRadioService> radioFactory,
        Func<IBrightnessService> brightnessFactory,
        IFocusAssistService focusAssistService,
        AppSettings settings,
        Dispatcher? dispatcher = null)
    {
        _audioFactory = audioFactory;
        _radioFactory = radioFactory;
        _brightnessFactory = brightnessFactory;
        _focusAssistService = focusAssistService;
        _settings = settings;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;

        _focusAssistService.StateChanged += OnFocusAssistChanged;
    }

    public void SetActive(bool active)
    {
        if (!active || _disposed)
            return;

        IsDoNotDisturbOn = _focusAssistService.IsActive;

        EnsureAudioService();

        // Ses tekerlekle önceden kurulmuş olabilir; radyo ve parlaklık yine de ilk açılışta başlar.
        if (_radioService is not null)
            return;

        // Radyo durumu StartAsync bitince StateChanged ile gelir.
        _radioService = _radioFactory();
        _radioService.StateChanged += OnRadioStateChanged;
        _ = _radioService.StartAsync();

        _ = InitializeBrightnessAsync();
    }

    private void EnsureAudioService()
    {
        if (_audioService is not null || _disposed)
            return;

        _audioService = _audioFactory();
        _audioService.StateChanged += OnAudioStateChanged;
        Volume = _audioService.Volume;
        VolumePercent = (int)Math.Round(Volume * 100);
        IsMuted = _audioService.IsMuted;
    }

    /// <summary>Çentik tekerlek eylemi "Ses" iken: panel açılmadan sistem sesini yüzde olarak artırır ya da azaltır.</summary>
    public void AdjustVolume(int deltaPercent)
    {
        EnsureAudioService();
        if (_audioService is null)
            return;

        var current = (int)Math.Round(_audioService.Volume * 100);
        OnVolumeSliderChanged(Math.Clamp(current + deltaPercent, 0, 100));
    }

    private async Task InitializeBrightnessAsync()
    {
        // WMI sorgusu UI iş parçacığını bekletmesin.
        var service = await Task.Run(_brightnessFactory);
        if (_disposed)
        {
            service.Dispose();
            return;
        }

        _brightnessService = service;
        _brightnessService.BrightnessChanged += OnBrightnessChanged;
        IsBrightnessSupported = _brightnessService.IsSupported;
        Brightness = _brightnessService.Brightness;
    }

    private void OnAudioStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            Volume = e.Volume;
            VolumePercent = (int)Math.Round(e.Volume * 100);
            IsMuted = e.IsMuted;
        });
    }

    private void OnRadioStateChanged(object? sender, RadioStateChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (e.Kind == RadioKind.WiFi)
                IsWiFiOn = e.State == RadioState.On;
            else if (e.Kind == RadioKind.Bluetooth)
                IsBluetoothOn = e.State == RadioState.On;
        });
    }

    private void OnBrightnessChanged(object? sender, int e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            Brightness = e;
        });
    }

    private void OnFocusAssistChanged(object? sender, bool e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            IsDoNotDisturbOn = e;
        });
    }

    [RelayCommand]
    private async Task ToggleWiFiAsync()
    {
        if (_radioService is null)
            return;
        await _radioService.ToggleAsync(RadioKind.WiFi);
        IsWiFiOn = _radioService.WiFiState == RadioState.On;
    }

    [RelayCommand]
    private async Task ToggleBluetoothAsync()
    {
        if (_radioService is null)
            return;
        await _radioService.ToggleAsync(RadioKind.Bluetooth);
        IsBluetoothOn = _radioService.BluetoothState == RadioState.On;
    }

    [RelayCommand]
    private void ToggleDoNotDisturb()
    {
        _focusAssistService.OpenSettings();
        IsDoNotDisturbOn = _focusAssistService.IsActive;
    }

    [RelayCommand]
    private void ToggleMute()
    {
        _audioService?.ToggleMute();
    }

    public void OnVolumeSliderChanged(double newVolumePercent)
    {
        var normalized = (float)Math.Clamp(newVolumePercent / 100.0, 0.0, 1.0);
        _audioService?.SetVolume(normalized);
        Volume = normalized;
        VolumePercent = (int)Math.Round(newVolumePercent);
    }

    public void OnBrightnessSliderChanged(double newBrightness)
    {
        var intBrightness = (int)Math.Clamp(newBrightness, 0, 100);
        _brightnessService?.SetBrightness(intBrightness);
        Brightness = intBrightness;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _focusAssistService.StateChanged -= OnFocusAssistChanged;

        if (_audioService is not null)
        {
            _audioService.StateChanged -= OnAudioStateChanged;
            _audioService.Dispose();
            _audioService = null;
        }

        if (_radioService is not null)
        {
            _radioService.StateChanged -= OnRadioStateChanged;
            _radioService.Dispose();
            _radioService = null;
        }

        if (_brightnessService is not null)
        {
            _brightnessService.BrightnessChanged -= OnBrightnessChanged;
            _brightnessService.Dispose();
            _brightnessService = null;
        }
    }
}
