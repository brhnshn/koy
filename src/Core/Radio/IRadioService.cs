namespace Koy.Core.Radio;

public enum RadioKind
{
    WiFi,
    Bluetooth
}

public enum RadioState
{
    Unknown,
    On,
    Off,
    Disabled
}

/// <summary>
/// Wi-Fi ve Bluetooth radyo durumu ve açma/kapatma sözleşmesi.
/// </summary>
public interface IRadioService : IDisposable
{
    RadioState WiFiState { get; }
    RadioState BluetoothState { get; }

    event EventHandler<RadioStateChangedEventArgs>? StateChanged;

    Task StartAsync();
    Task<bool> SetStateAsync(RadioKind kind, bool enable);
    Task<bool> ToggleAsync(RadioKind kind);
}

public sealed record RadioStateChangedEventArgs(RadioKind Kind, RadioState State);
