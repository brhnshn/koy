using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Power;
using Koy.Core.Settings;
using Koy.Core.Weather;

namespace Koy.App.Features.Status;

public sealed partial class StatusViewModel : ObservableObject, IDisposable
{
    private readonly IPowerService _powerService;
    private readonly IWeatherService _weatherService;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _clockTimer;
    private bool _isActive;
    private bool _disposed;

    [ObservableProperty]
    private string _currentTime = "";

    [ObservableProperty]
    private string _currentDate = "";

    [ObservableProperty]
    private bool _hasBattery;

    [ObservableProperty]
    private int _batteryPercent = 100;

    [ObservableProperty]
    private bool _isCharging;

    [ObservableProperty]
    private string _batteryText = "";

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

    public StatusViewModel(
        IPowerService powerService,
        IWeatherService weatherService,
        AppSettings settings,
        Dispatcher? dispatcher = null)
    {
        _powerService = powerService;
        _weatherService = weatherService;
        _settings = settings;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;

        City = settings.WeatherCity;

        UpdateClock();
        UpdatePowerState();
        UpdateWeatherState(_weatherService.CurrentWeather);

        _powerService.PowerChanged += OnPowerChanged;
        _weatherService.WeatherChanged += OnWeatherChanged;

        // İlk açılışta arka planda hava durumu isteği yap
        _ = _weatherService.GetCurrentWeatherAsync(_settings.WeatherLatitude, _settings.WeatherLongitude);
    }

    public void SetActive(bool active)
    {
        if (_isActive == active)
            return;

        _isActive = active;

        if (_isActive)
        {
            UpdateClock();
            _clockTimer ??= new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();

            // Panel açıldığında hava durumunu tazele (önbellek kontrolü servis içindedir)
            _ = _weatherService.GetCurrentWeatherAsync(_settings.WeatherLatitude, _settings.WeatherLongitude);
        }
        else
        {
            if (_clockTimer is not null)
            {
                _clockTimer.Tick -= OnClockTick;
                _clockTimer.Stop();
                _clockTimer = null;
            }
        }
    }

    private void OnClockTick(object? sender, EventArgs e) => UpdateClock();

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var culture = new CultureInfo(_settings.Language == "en" ? "en-US" : "tr-TR");
        CurrentTime = now.ToString("HH:mm", culture);
        CurrentDate = now.ToString("d MMMM dddd", culture);
    }

    private void OnPowerChanged(object? sender, PowerChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            HasBattery = e.HasBattery;
            BatteryPercent = e.RemainingPercent;
            IsCharging = e.IsCharging;
            BatteryText = IsCharging ? $"%{BatteryPercent} ({Resources.Strings.Charging})" : $"%{BatteryPercent}";
        });
    }

    private void UpdatePowerState()
    {
        HasBattery = _powerService.HasBattery;
        BatteryPercent = _powerService.RemainingPercent;
        IsCharging = _powerService.IsCharging;
        BatteryText = IsCharging ? $"%{BatteryPercent} ({Resources.Strings.Charging})" : $"%{BatteryPercent}";
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
            WeatherDescription = Resources.Strings.Loading;
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
    }
}
