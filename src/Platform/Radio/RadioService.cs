using Koy.Core.Radio;
using WinRadio = Windows.Devices.Radios.Radio;
using WinRadioKind = Windows.Devices.Radios.RadioKind;
using WinRadioState = Windows.Devices.Radios.RadioState;

namespace Koy.Platform.Radio;

public sealed class RadioService : IRadioService
{
    private WinRadio? _wifiRadio;
    private WinRadio? _bluetoothRadio;
    private bool _disposed;

    public RadioState WiFiState => MapState(_wifiRadio?.State);
    public RadioState BluetoothState => MapState(_bluetoothRadio?.State);

    public event EventHandler<RadioStateChangedEventArgs>? StateChanged;

    public async Task StartAsync()
    {
        try
        {
            var access = await WinRadio.RequestAccessAsync();
            if (access != Windows.Devices.Radios.RadioAccessStatus.Allowed)
                return;

            var radios = await WinRadio.GetRadiosAsync();
            foreach (var radio in radios)
            {
                if (radio.Kind == WinRadioKind.WiFi)
                {
                    _wifiRadio = radio;
                    radio.StateChanged += OnWiFiStateChanged;
                }
                else if (radio.Kind == WinRadioKind.Bluetooth)
                {
                    _bluetoothRadio = radio;
                    radio.StateChanged += OnBluetoothStateChanged;
                }
            }

            StateChanged?.Invoke(this, new RadioStateChangedEventArgs(RadioKind.WiFi, WiFiState));
            StateChanged?.Invoke(this, new RadioStateChangedEventArgs(RadioKind.Bluetooth, BluetoothState));
        }
        catch
        {
            // Radyo erişim hatası veya donanım yok
        }
    }

    private void OnWiFiStateChanged(WinRadio sender, object args)
    {
        StateChanged?.Invoke(this, new RadioStateChangedEventArgs(RadioKind.WiFi, MapState(sender.State)));
    }

    private void OnBluetoothStateChanged(WinRadio sender, object args)
    {
        StateChanged?.Invoke(this, new RadioStateChangedEventArgs(RadioKind.Bluetooth, MapState(sender.State)));
    }

    public async Task<bool> SetStateAsync(RadioKind kind, bool enable)
    {
        var targetRadio = kind == RadioKind.WiFi ? _wifiRadio : _bluetoothRadio;
        if (targetRadio is null)
            return false;

        try
        {
            var targetState = enable ? WinRadioState.On : WinRadioState.Off;
            var result = await targetRadio.SetStateAsync(targetState);
            var success = result == Windows.Devices.Radios.RadioAccessStatus.Allowed;
            if (success)
            {
                StateChanged?.Invoke(this, new RadioStateChangedEventArgs(kind, MapState(targetRadio.State)));
            }
            return success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleAsync(RadioKind kind)
    {
        var current = kind == RadioKind.WiFi ? WiFiState : BluetoothState;
        var newState = current != RadioState.On;
        return await SetStateAsync(kind, newState);
    }

    private static RadioState MapState(WinRadioState? state) => state switch
    {
        WinRadioState.On => RadioState.On,
        WinRadioState.Off => RadioState.Off,
        WinRadioState.Disabled => RadioState.Disabled,
        _ => RadioState.Unknown
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_wifiRadio is not null)
            _wifiRadio.StateChanged -= OnWiFiStateChanged;

        if (_bluetoothRadio is not null)
            _bluetoothRadio.StateChanged -= OnBluetoothStateChanged;

        _wifiRadio = null;
        _bluetoothRadio = null;
    }
}
