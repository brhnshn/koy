using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Resources;
using Koy.Core.Diagnostics;
using Koy.Core.Wireless;

namespace Koy.App.Features.ControlCenter;

/// <summary>Wi-Fi listesindeki tek ağ satırı.</summary>
public sealed partial class WifiRowViewModel : ObservableObject
{
    // Şifre yalnız bu alanda, bellekte durur: özellik, günlük ya da ayar dosyasına gitmez; bağlanma denemesinden sonra silinir.
    private string _password = string.Empty;

    public string Ssid { get; }

    public WifiNetwork Network { get; private set; }

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private int _bars;

    [ObservableProperty]
    private bool _isSecured;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string? _statusText;

    [ObservableProperty]
    private string? _errorText;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isPasswordOpen;

    [ObservableProperty]
    private bool _canConnect;

    /// <summary>Satır tıklanabilir mi (bağlı ve gizli ağlarda tıklama yok; işlevsiz düğme olmaz).</summary>
    [ObservableProperty]
    private bool _isClickable;

    public bool HasStatus => IsBusy || !string.IsNullOrEmpty(StatusText) || !string.IsNullOrEmpty(ErrorText);

    public string DisplayStatus => IsBusy ? Strings.Get("WifiConnecting") : ErrorText ?? StatusText ?? string.Empty;

    public WifiRowViewModel(WifiNetwork network)
    {
        Ssid = network.Ssid;
        Network = network;
        Update(network);
    }

    partial void OnStatusTextChanged(string? value) => NotifyStatus();

    partial void OnErrorTextChanged(string? value) => NotifyStatus();

    partial void OnIsBusyChanged(bool value) => NotifyStatus();

    private void NotifyStatus()
    {
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(DisplayStatus));
    }

    public void Update(WifiNetwork network)
    {
        Network = network;
        DisplayName = network.IsHidden ? Strings.Get("WifiHidden") : network.Ssid;
        Bars = WifiList.SignalBars(network.SignalQuality);
        IsSecured = network.IsSecured;
        IsConnected = network.IsConnected;
        var key = WifiList.StatusKey(network);
        StatusText = key is null ? null : Strings.Get(key);
        IsClickable = WifiList.ActionFor(network) != WifiRowAction.None;
        if (!IsClickable)
            IsPasswordOpen = false;
    }

    public void SetPassword(string password)
    {
        _password = password;
        CanConnect = password.Length > 0;
    }

    /// <summary>Şifreyi verir ve bellekten siler.</summary>
    public string TakePassword()
    {
        var value = _password;
        ClearPassword();
        return value;
    }

    public void ClearPassword()
    {
        _password = string.Empty;
        CanConnect = false;
    }
}

/// <summary>
/// Wi-Fi listesi sayfası. Tarama yalnız sayfa açıkken yapılır: açılışta bir kez, sonra 10 sn'de bir; sayfa kapanınca
/// zamanlayıcı ve servis bırakılır (boşta %0 CPU).
/// </summary>
public sealed partial class WifiPageViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(10);

    private readonly Func<IWifiService> _acquire;
    private readonly Action? _release;
    private readonly Func<bool> _isRadioOn;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<string, WifiRowViewModel> _rows = new(StringComparer.Ordinal);
    private IWifiService? _service;
    private WifiScanResult? _last;
    private bool _scanning;
    private bool _disposed;

    public ObservableCollection<WifiRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    private WifiListState _state = WifiListState.Loading;

    /// <summary>Liste yerine gösterilen açıklama (izin yok, adaptör yok…); liste hazırken boş.</summary>
    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private bool _showMessage;

    /// <summary>İzin açıklamasının altındaki "Konum ayarlarını aç" bağlantısı.</summary>
    [ObservableProperty]
    private bool _showLocationLink;

    [ObservableProperty]
    private bool _showList;

    public WifiPageViewModel(Func<IWifiService> acquire, Action? release, Func<bool> isRadioOn, Dispatcher? dispatcher = null)
    {
        _acquire = acquire;
        _release = release;
        _isRadioOn = isRadioOn;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
        _timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher) { Interval = ScanInterval };
        _timer.Tick += (_, _) => _ = RefreshAsync();
        ApplyState();
    }

    /// <summary>Sayfa açılınca true: servis alınır, bir kez taranır, zamanlayıcı başlar. false: her şey bırakılır.</summary>
    public void SetActive(bool active)
    {
        if (_disposed)
            return;

        if (active)
        {
            if (_service is not null)
                return;

            _service = _acquire();
            _timer.Start();
            _ = RefreshAsync();
            return;
        }

        _timer.Stop();
        if (_service is not null)
        {
            if (_release is not null)
                _release();
            else
                _service.Dispose();
            _service = null;
        }

        foreach (var row in _rows.Values)
            row.ClearPassword();
        _rows.Clear();
        Rows.Clear();
        _last = null;
        State = WifiListState.Loading;
        ApplyState();
    }

    /// <summary>Radyo açılıp kapanınca (başlık anahtarı) liste yeniden değerlendirilir ve açıldıysa taranır.</summary>
    public void OnRadioChanged()
    {
        if (_service is null)
            return;

        ApplyState();
        if (_isRadioOn())
            _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var service = _service;
        if (service is null || _scanning || _disposed)
            return;

        // Bağlanma sürerken satırlar yeniden sıralanmaz.
        if (_rows.Values.Any(r => r.IsBusy))
            return;

        _scanning = true;
        try
        {
            var result = await service.ScanAsync();
            if (!ReferenceEquals(service, _service))
                return;

            _last = result;
            ApplyResult();
        }
        catch (Exception ex)
        {
            CrashLog.Write("Wi-Fi listesi taraması", ex);
        }
        finally
        {
            _scanning = false;
        }
    }

    private void ApplyResult()
    {
        ApplyState();
        if (State != WifiListState.Ready || _last is null)
            return;

        var networks = WifiList.Normalize(_last.Networks);
        var next = new List<WifiRowViewModel>(networks.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var network in networks)
        {
            seen.Add(network.Ssid);
            if (_rows.TryGetValue(network.Ssid, out var row))
                row.Update(network);
            else
                _rows[network.Ssid] = row = new WifiRowViewModel(network);
            next.Add(row);
        }

        foreach (var stale in _rows.Keys.Where(s => !seen.Contains(s)).ToList())
        {
            _rows[stale].ClearPassword();
            _rows.Remove(stale);
        }

        for (var i = Rows.Count - 1; i >= 0; i--)
        {
            if (!next.Contains(Rows[i]))
                Rows.RemoveAt(i);
        }

        for (var i = 0; i < next.Count; i++)
        {
            var current = Rows.IndexOf(next[i]);
            if (current < 0)
                Rows.Insert(i, next[i]);
            else if (current != i)
                Rows.Move(current, i);
        }
    }

    private void ApplyState()
    {
        State = WifiList.Resolve(_last, _isRadioOn());
        ShowList = State == WifiListState.Ready;
        ShowLocationLink = State is WifiListState.LocationBlocked or WifiListState.Empty or WifiListState.NoAccess;
        MessageText = State switch
        {
            WifiListState.Loading => Strings.Get("Loading"),
            WifiListState.RadioOff => Strings.Get("WifiRadioOff"),
            WifiListState.NoAdapter => Strings.Get("WifiNoAdapter"),
            WifiListState.NoAccess => Strings.Get("WifiNoAccess"),
            WifiListState.LocationBlocked => Strings.Get("WifiLocationBlocked"),
            WifiListState.Empty => Strings.Get("WifiEmpty"),
            WifiListState.ScanFailed => Strings.Get("WifiScanFailed"),
            _ => string.Empty
        };
        ShowMessage = !ShowList;
    }

    /// <summary>
    /// Satıra tıklama: kayıtlı/açık ağa doğrudan bağlanır, güvenli ve kayıtsız ağda satırın içinde şifre kutusunu açar
    /// (bir seferde yalnız bir satırda).
    /// </summary>
    [RelayCommand]
    private async Task ActivateAsync(WifiRowViewModel? row)
    {
        if (row is null || row.IsBusy)
            return;

        switch (WifiList.ActionFor(row.Network))
        {
            case WifiRowAction.ConnectDirect:
                await ConnectAsync(row, null);
                break;
            case WifiRowAction.AskPassword:
                var open = !row.IsPasswordOpen;
                foreach (var other in _rows.Values)
                {
                    other.IsPasswordOpen = false;
                    other.ClearPassword();
                }
                row.ErrorText = null;
                row.IsPasswordOpen = open;
                break;
        }
    }

    /// <summary>Şifre kutusundaki "Bağlan".</summary>
    [RelayCommand]
    private async Task ConnectWithPasswordAsync(WifiRowViewModel? row)
    {
        if (row is null || row.IsBusy || !row.CanConnect)
            return;

        await ConnectAsync(row, row.TakePassword());
    }

    private async Task ConnectAsync(WifiRowViewModel row, string? password)
    {
        var service = _service;
        if (service is null)
            return;

        row.ErrorText = null;
        row.IsBusy = true;
        try
        {
            var status = await service.ConnectAsync(row.Ssid, password);
            var key = WifiList.ErrorKey(status);
            row.ErrorText = key is null ? null : Strings.Get(key);

            // Yanlış şifre: kutu yeniden açık kalır, kullanıcı düzeltebilsin.
            row.IsPasswordOpen = status == WifiConnectStatus.WrongPassword;
        }
        catch (Exception ex)
        {
            CrashLog.Write("Wi-Fi bağlanma", ex);
            row.ErrorText = Strings.Get("WifiErrFailed");
        }
        finally
        {
            password = null;
            row.IsBusy = false;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DisconnectAsync(WifiRowViewModel? row)
    {
        var service = _service;
        if (row is null || service is null)
            return;

        service.Disconnect();

        // Windows bağlantıyı kestikten sonra durum değişir; kısa bekleyip listeyi tazele.
        await Task.Delay(700);
        await RefreshAsync();
    }

    [RelayCommand]
    private void OpenLocationSettings()
    {
        if (_service is not null)
            _service.OpenLocationSettings();
        else
            Launch("ms-settings:privacy-location");
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (_service is not null)
            _service.OpenSettings();
        else
            Launch("ms-settings:network-wifi");
    }

    private static void Launch(string uri)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // Ayarlar uygulaması kaldırılmış olabilir.
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        SetActive(false);
        _disposed = true;
    }
}
