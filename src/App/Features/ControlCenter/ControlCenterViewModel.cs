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
    private readonly IAudioService _audioService;
    private readonly IRadioService _radioService;
    private readonly IBrightnessService _brightnessService;
    private readonly IFocusAssistService _focusAssistService;
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

    public ControlCenterViewModel(
        IAudioService audioService,
        IRadioService radioService,
        IBrightnessService brightnessService,
        IFocusAssistService focusAssistService,
        AppSettings settings,
        Dispatcher? dispatcher = null)
    {
        _audioService = audioService;
        _radioService = radioService;
        _brightnessService = brightnessService;
        _focusAssistService = focusAssistService;
        _settings = settings;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;

        // Başlangıç değerleri
        _volume = _audioService.Volume;
        _volumePercent = (int)Math.Round(_volume * 100);
        _isMuted = _audioService.IsMuted;

        _isWiFiOn = _radioService.WiFiState == RadioState.On;
        _isBluetoothOn = _radioService.BluetoothState == RadioState.On;

        _isBrightnessSupported = _brightnessService.IsSupported;
        _brightness = _brightnessService.Brightness;

        _isDoNotDisturbOn = _focusAssistService.IsActive;

        // Olay dinleyicileri
        _audioService.StateChanged += OnAudioStateChanged;
        _radioService.StateChanged += OnRadioStateChanged;
        _brightnessService.BrightnessChanged += OnBrightnessChanged;
        _focusAssistService.StateChanged += OnFocusAssistChanged;
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
        await _radioService.ToggleAsync(RadioKind.WiFi);
        IsWiFiOn = _radioService.WiFiState == RadioState.On;
    }

    [RelayCommand]
    private async Task ToggleBluetoothAsync()
    {
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
        _audioService.ToggleMute();
    }

    public void OnVolumeSliderChanged(double newVolumePercent)
    {
        var normalized = (float)Math.Clamp(newVolumePercent / 100.0, 0.0, 1.0);
        _audioService.SetVolume(normalized);
        Volume = normalized;
        VolumePercent = (int)Math.Round(newVolumePercent);
    }

    public void OnBrightnessSliderChanged(double newBrightness)
    {
        var intBrightness = (int)Math.Clamp(newBrightness, 0, 100);
        _brightnessService.SetBrightness(intBrightness);
        Brightness = intBrightness;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _audioService.StateChanged -= OnAudioStateChanged;
        _radioService.StateChanged -= OnRadioStateChanged;
        _brightnessService.BrightnessChanged -= OnBrightnessChanged;
        _focusAssistService.StateChanged -= OnFocusAssistChanged;
    }
}
