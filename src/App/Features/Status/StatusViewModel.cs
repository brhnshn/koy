using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Resources;
using Koy.Core.Network;
using Koy.Core.Power;
using Koy.Core.Settings;
using Koy.Core.Weather;

namespace Koy.App.Features.Status;

/// <summary>
/// Saat, tarih, pil, ağ ve hava durumu. Ana görünümün durum sütunu ve ayrıntılı Durum paneli bunu paylaşır.
/// Saat zamanlayıcısı yalnız panel görünürken çalışır ve dakika başına bir kez tikler.
/// </summary>
public sealed partial class StatusViewModel : ObservableObject, IDisposable
{
    private readonly IPowerService _powerService;
    private readonly IWeatherService _weatherService;
    private readonly INetworkService? _networkService;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _clockTimer;
    private bool _isActive;
    private bool _disposed;

    [ObservableProperty]
    private string _currentTime = "";

    [ObservableProperty]
    private string _currentDate = "";

    /// <summary>Kısa tarih ("6 Kas Cum"): ana görünümün durum sütunu için.</summary>
    [ObservableProperty]
    private string _shortDate = "";

    /// <summary>Uzun tarih ("6 Kasım 2026 Cuma"): boşta ana görünümde büyük saatin altında.</summary>
    [ObservableProperty]
    private string _fullDate = "";

    [ObservableProperty]
    private bool _hasBattery;

    [ObservableProperty]
    private int _batteryPercent = 100;

    [ObservableProperty]
    private bool _isCharging;

    [ObservableProperty]
    private string _batteryText = "";

    /// <summary>Cihaz şu an pille (şarj olmadan) çalışıyor mu. Pil tasarrufu kararlarında kullanılır.</summary>
    public bool IsOnBattery { get; private set; }

    /// <summary>Hava durumu için bir konum var mı (izin verildi ya da şehir seçildi). Yoksa hava durumu hiç gösterilmez.</summary>
    [ObservableProperty]
    private bool _hasWeatherLocation;

    /// <summary>Hava verisi geldi mi (konum var ama ağ yoksa false: sıcaklık yerine "--" gösterilir).</summary>
    [ObservableProperty]
    private bool _hasWeather;

    [ObservableProperty]
    private string _temperatureText = "";

    [ObservableProperty]
    private string _weatherDescription = "";

    [ObservableProperty]
    private string _weatherIconKind = "Cloud";

    [ObservableProperty]
    private string _city = "";

    // --- Ağ ---
    [ObservableProperty]
    private NetworkDisplay _networkDisplay = NetworkDisplay.Disconnected;

    /// <summary>Durum sütunundaki ağ satırı: Wi-Fi adı, "Ethernet", "Bağlı değil"…</summary>
    [ObservableProperty]
    private string _networkText = "";

    /// <summary>Ağ simgesi normal mi (internet var) yoksa üstü çizili mi.</summary>
    [ObservableProperty]
    private bool _networkIsConnected;

    /// <summary>Ağ durumu servisi var mı; yoksa satır hiç çizilmez.</summary>
    public bool HasNetworkInfo => _networkService is not null;

    public StatusViewModel(
        IPowerService powerService,
        IWeatherService weatherService,
        AppSettings settings,
        Dispatcher? dispatcher = null,
        INetworkService? networkService = null)
    {
        _powerService = powerService;
        _weatherService = weatherService;
        _networkService = networkService;
        _settings = settings;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;

        UpdateLocationState();
        UpdateClock();
        UpdatePowerState();
        UpdateWeatherState(_weatherService.CurrentWeather);

        _powerService.PowerChanged += OnPowerChanged;
        _weatherService.WeatherChanged += OnWeatherChanged;

        if (_networkService is not null)
        {
            ApplyNetwork(_networkService.Current);
            _networkService.Changed += OnNetworkChanged;
        }

        // Hava durumu panel açılınca istenir (SetActive): açılıştaki HTTPS isteği TLS yığınıyla
        // birkaç MB tutuyordu ve panel hiç açılmayabilir (AGENTS.md #5).
    }

    public void SetActive(bool active)
    {
        if (_isActive == active)
            return;

        _isActive = active;

        if (_isActive)
        {
            UpdateClock();
            ScheduleNextClockTick();

            // Panel açıldığında hava durumunu tazele (önbellek kontrolü servis içindedir)
            RequestWeather();
        }
        else
        {
            _clockTimer?.Stop();
            _clockTimer = null;
        }
    }

    /// <summary>Konum ayarı değişti (cihaz konumu alındı, şehir seçildi, anahtar kapandı): hava durumunu yeniden kurar.</summary>
    public void OnLocationChanged()
    {
        if (_disposed)
            return;

        UpdateLocationState();
        UpdateWeatherState(_weatherService.CurrentWeather);
        if (_isActive)
            RequestWeather();
    }

    private void UpdateLocationState()
    {
        HasWeatherLocation = WeatherLocationPolicy.HasLocation(_settings);
        City = _settings.WeatherLocationSource == WeatherLocationSource.City ? CityShortName(_settings.WeatherCity) : "";
    }

    // "Ankara, Türkiye" -> "Ankara": dar sütunda yalnız şehir adı.
    private static string CityShortName(string displayName)
    {
        var comma = displayName.IndexOf(',');
        return comma > 0 ? displayName[..comma] : displayName;
    }

    private void RequestWeather()
    {
        if (!HasWeatherLocation)
            return;

        _ = _weatherService.GetCurrentWeatherAsync(_settings.WeatherLatitude, _settings.WeatherLongitude);
    }

    // Saat yalnız HH:mm gösterir: bir sonraki dakika başına kurulan tek atımlık zamanlayıcı, saniyelik tik gerekmez.
    private void ScheduleNextClockTick()
    {
        var now = DateTime.Now;
        var untilNextMinute = TimeSpan.FromSeconds(60 - now.Second) - TimeSpan.FromMilliseconds(now.Millisecond) + TimeSpan.FromMilliseconds(50);

        _clockTimer?.Stop();
        _clockTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher) { Interval = untilNextMinute };
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        UpdateClock();
        if (_isActive)
            ScheduleNextClockTick();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var culture = new CultureInfo(_settings.Language == "en" ? "en-US" : "tr-TR");
        CurrentTime = now.ToString("HH:mm", culture);
        CurrentDate = now.ToString("d MMMM dddd", culture);
        ShortDate = now.ToString("d MMM ddd", culture);
        FullDate = now.ToString("d MMMM yyyy dddd", culture);
    }

    private void OnPowerChanged(object? sender, PowerChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            HasBattery = e.HasBattery;
            BatteryPercent = e.RemainingPercent;
            IsCharging = e.IsCharging;
            SetBatteryStatus(e.Status);
            BatteryText = IsCharging ? $"%{BatteryPercent} ({Strings.Charging})" : $"%{BatteryPercent}";
        });
    }

    private void UpdatePowerState()
    {
        HasBattery = _powerService.HasBattery;
        BatteryPercent = _powerService.RemainingPercent;
        IsCharging = _powerService.IsCharging;
        SetBatteryStatus(_powerService.Status);
        BatteryText = IsCharging ? $"%{BatteryPercent} ({Strings.Charging})" : $"%{BatteryPercent}";
    }

    private void SetBatteryStatus(BatteryStatus status)
    {
        var onBattery = BatterySaverPolicy.ShouldReduceMotion(true, HasBattery, status);
        if (onBattery == IsOnBattery)
            return;

        IsOnBattery = onBattery;
        OnPropertyChanged(nameof(IsOnBattery));
    }

    private void OnNetworkChanged(object? sender, NetworkStatus status)
    {
        // Olay Windows'un iş parçacığından gelir.
        _dispatcher.InvokeAsync(() =>
        {
            if (!_disposed)
                ApplyNetwork(status);
        });
    }

    private void ApplyNetwork(NetworkStatus status)
    {
        NetworkDisplay = NetworkSummary.Describe(status);
        NetworkIsConnected = NetworkSummary.IsConnected(status);
        NetworkText = NetworkDisplay switch
        {
            NetworkDisplay.WiFi => NetworkSummary.WiFiName(status) ?? Strings.Get("NetworkWiFi"),
            NetworkDisplay.Ethernet => Strings.Get("NetworkEthernetShort"),
            NetworkDisplay.Cellular => Strings.Get("NetworkCellularShort"),
            NetworkDisplay.NoInternet => Strings.Get("NetworkNoInternet"),
            _ => Strings.Get("NetworkDisconnected")
        };
    }

    private void OnWeatherChanged(object? sender, WeatherInfo? weather)
    {
        _dispatcher.InvokeAsync(() => UpdateWeatherState(weather));
    }

    private void UpdateWeatherState(WeatherInfo? weather)
    {
        if (weather is null)
        {
            HasWeather = false;
            TemperatureText = "--°C";
            WeatherDescription = Strings.Loading;
            WeatherIconKind = "Cloud";
            return;
        }

        HasWeather = true;
        TemperatureText = $"{Math.Round(weather.Temperature)}°C";
        WeatherDescription = weather.Description;
        WeatherIconKind = weather.IconKind;
    }

    [RelayCommand]
    private async Task RefreshWeatherAsync()
    {
        if (!HasWeatherLocation)
            return;

        await _weatherService.RefreshAsync(_settings.WeatherLatitude, _settings.WeatherLongitude);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        SetActive(false);
        _powerService.PowerChanged -= OnPowerChanged;
        _weatherService.WeatherChanged -= OnWeatherChanged;
        if (_networkService is not null)
            _networkService.Changed -= OnNetworkChanged;
    }
}
