using System.Diagnostics;
using Koy.Core.Diagnostics;
using Koy.Core.Radio;
using Windows.Foundation;
using WinRadio = Windows.Devices.Radios.Radio;
using WinRadioAccessStatus = Windows.Devices.Radios.RadioAccessStatus;
using WinRadioKind = Windows.Devices.Radios.RadioKind;
using WinRadioState = Windows.Devices.Radios.RadioState;

namespace Koy.Platform.Radio;

public sealed class RadioService : IRadioService
{
    private readonly Lock _gate = new();
    private Task? _refreshTask;
    private WinRadio? _wifiRadio;
    private WinRadio? _bluetoothRadio;
    private bool _scanned;
    private bool _disposed;

    public RadioAccess Access { get; private set; }
    public RadioState WiFiState => GetState(_wifiRadio);
    public RadioState BluetoothState => GetState(_bluetoothRadio);

    public event EventHandler<RadioStateChangedEventArgs>? StateChanged;

    public Task RefreshAsync()
    {
        lock (_gate)
        {
            if (_refreshTask is { IsCompleted: false })
                return _refreshTask;

            return _refreshTask = RefreshCoreAsync();
        }
    }

    private async Task RefreshCoreAsync()
    {
        WinRadio? wifi = null;
        WinRadio? bluetooth = null;

        try
        {
            Access = MapAccess(await WinRadio.RequestAccessAsync());
            if (Access == RadioAccess.Allowed)
            {
                foreach (var radio in await WinRadio.GetRadiosAsync())
                {
                    if (radio.Kind == WinRadioKind.WiFi)
                        wifi ??= radio;
                    else if (radio.Kind == WinRadioKind.Bluetooth)
                        bluetooth ??= radio;
                }
            }
        }
        catch (Exception ex)
        {
            // Radyo yığını hata verirse radyo "yok" sayılır: döşeme sessizce "Kapalı" kalmak yerine
            // nedenini yazar ve Windows ayarlarını açar. Önceki taramadan kalan erişim durumu bayat
            // kalmasın diye bilinmiyor yapılır.
            CrashLog.Write("RadioService.RefreshCoreAsync", ex);
            Access = RadioAccess.Unknown;
            wifi = null;
            bluetooth = null;
        }

        if (_disposed)
            return;

        // Her taramada yeni RCW örnekleri gelir; eski abonelik bırakılmazsa çıkarılan USB adaptör tutulur.
        Rebind(ref _wifiRadio, wifi, OnWiFiStateChanged);
        Rebind(ref _bluetoothRadio, bluetooth, OnBluetoothStateChanged);
        _scanned = true;

        Raise(RadioKind.WiFi, WiFiState);
        Raise(RadioKind.Bluetooth, BluetoothState);
    }

    private static void Rebind(ref WinRadio? field, WinRadio? next, TypedEventHandler<WinRadio, object> handler)
    {
        if (field is not null)
            field.StateChanged -= handler;

        field = next;

        if (field is not null)
            field.StateChanged += handler;
    }

    // WinRT olayı iş parçacığı havuzundan gelir; adaptör çıkarılmışsa State okuması hata verebilir.
    private void OnWiFiStateChanged(WinRadio sender, object args) => Raise(RadioKind.WiFi, GetState(sender));

    private void OnBluetoothStateChanged(WinRadio sender, object args) => Raise(RadioKind.Bluetooth, GetState(sender));

    // Aboneden gelen istisna tarama görevini (fire-and-forget) ya da WinRT iş parçacığını bozmasın.
    private void Raise(RadioKind kind, RadioState state)
    {
        try
        {
            StateChanged?.Invoke(this, new RadioStateChangedEventArgs(kind, state));
        }
        catch (Exception ex)
        {
            CrashLog.Write("RadioService.StateChanged abonesi", ex);
        }
    }

    public async Task<bool> SetStateAsync(RadioKind kind, bool enable)
    {
        // Panel açılır açılmaz tıklanırsa tarama henüz bitmemiş olabilir; tıklama yutulmasın.
        if (!_scanned)
            await RefreshAsync();

        var radio = kind == RadioKind.WiFi ? _wifiRadio : _bluetoothRadio;
        if (radio is null)
            return false;

        try
        {
            var result = await radio.SetStateAsync(enable ? WinRadioState.On : WinRadioState.Off);
            if (result != WinRadioAccessStatus.Allowed)
            {
                if (result is WinRadioAccessStatus.DeniedByUser or WinRadioAccessStatus.DeniedBySystem)
                    Access = MapAccess(result);

                Raise(kind, MapState(radio.State));
                return false;
            }

            Raise(kind, MapState(radio.State));
            return true;
        }
        catch (Exception ex)
        {
            CrashLog.Write("RadioService.SetStateAsync", ex);
            return false;
        }
    }

    public async Task<bool> ToggleAsync(RadioKind kind)
    {
        if (!_scanned)
            await RefreshAsync();

        var current = kind == RadioKind.WiFi ? WiFiState : BluetoothState;
        return await SetStateAsync(kind, current != RadioState.On);
    }

    public void OpenSettings(RadioKind kind) =>
        OpenUri(kind == RadioKind.WiFi ? "ms-settings:network-wifi" : "ms-settings:bluetooth");

    public void OpenPrivacySettings() => OpenUri("ms-settings:privacy-radios");

    private static void OpenUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // Ayarlar uygulaması kaldırılmış ya da politika ile engellenmiş olabilir.
        }
    }

    private RadioState GetState(WinRadio? radio)
    {
        if (radio is null)
            return _scanned ? RadioState.NotPresent : RadioState.Unknown;

        try
        {
            return MapState(radio.State);
        }
        catch
        {
            // Adaptör çıkarıldıysa eski nesne hata verir.
            return RadioState.NotPresent;
        }
    }

    private static RadioState MapState(WinRadioState state) => state switch
    {
        WinRadioState.On => RadioState.On,
        WinRadioState.Off => RadioState.Off,
        WinRadioState.Disabled => RadioState.Disabled,
        _ => RadioState.Unknown
    };

    private static RadioAccess MapAccess(WinRadioAccessStatus status) => status switch
    {
        WinRadioAccessStatus.Allowed => RadioAccess.Allowed,
        WinRadioAccessStatus.DeniedByUser => RadioAccess.DeniedByUser,
        WinRadioAccessStatus.DeniedBySystem => RadioAccess.DeniedBySystem,
        _ => RadioAccess.Unknown
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Rebind(ref _wifiRadio, null, OnWiFiStateChanged);
        Rebind(ref _bluetoothRadio, null, OnBluetoothStateChanged);
    }
}
