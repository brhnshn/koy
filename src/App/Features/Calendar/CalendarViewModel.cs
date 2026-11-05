using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Resources;
using Koy.Core.Calendar;
using Koy.Core.Settings;
using Koy.Core.Timers;

namespace Koy.App.Features.Calendar;

/// <summary>Sağ sütundaki araç: Pomodoro, serbest geri sayım zamanlayıcısı ya da kronometre.</summary>
public enum CalendarTool
{
    Pomodoro,
    Timer,
    Stopwatch
}

/// <summary>Hafta görünümünün bir satırı.</summary>
public sealed record WeekDayRow(DateOnly Date, string Label, bool IsToday, string Summary, bool HasEvents);

/// <summary>Gün görünümündeki bir etkinlik satırı.</summary>
public sealed record EventRow(string Id, string TimeText, string Title);

/// <summary>Zamanlayıcının hazır süre düğmesi ("5 dk").</summary>
public sealed record PresetChip(int Minutes, string Label);

/// <summary>Kronometre tur satırı.</summary>
public sealed record LapRow(string Label, string Total, string Split);

/// <summary>
/// Takvim paneli: ay, hafta ve gün görünümü (yerel etkinlikler), Pomodoro, serbest zamanlayıcı ve kronometre.
/// Zamanlayıcılar ve kronometre saf durum makineleridir (Core); bu sınıf yalnız görünür olduğunda bir
/// <see cref="DispatcherTimer"/> ile ekran metnini tazeler. Hiçbir şey görünmüyorsa tik yoktur, bitiş zamanı
/// <see cref="CountdownTimer"/> içinde hesaplanır.
/// </summary>
public sealed partial class CalendarViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan FastTick = TimeSpan.FromMilliseconds(100);

    private readonly Pomodoro _pomodoro;
    private readonly CountdownTimer _countdown;
    private readonly LapStopwatch _stopwatch = new();
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;
    private readonly SharedEvents _events;
    private readonly DispatcherTimer _displayTimer;
    private readonly CultureInfo _culture;
    private bool _calendarVisible;
    private bool _homeVisible;
    private bool _disposed;

    // --- Görünüm ve gezinme ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthView), nameof(IsWeekView), nameof(IsDayView))]
    private CalendarViewMode _viewMode = CalendarViewMode.Month;

    public bool IsMonthView => ViewMode == CalendarViewMode.Month;
    public bool IsWeekView => ViewMode == CalendarViewMode.Week;
    public bool IsDayView => ViewMode == CalendarViewMode.Day;

    /// <summary>Gezinmenin dayandığı gün: ay görünümünde ayı, hafta görünümünde haftayı, gün görünümünde günü belirler.</summary>
    [ObservableProperty]
    private DateOnly _anchor = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private string _headerTitle = "";

    [ObservableProperty]
    private IReadOnlyList<CalendarDay> _days = [];

    public IReadOnlyList<string> WeekdayHeaders { get; }

    [ObservableProperty]
    private IReadOnlyList<WeekDayRow> _weekDays = [];

    public ObservableCollection<EventRow> DayEvents { get; } = [];

    public bool HasDayEvents => DayEvents.Count > 0;

    [ObservableProperty]
    private string _newEventTitle = "";

    [ObservableProperty]
    private string _newEventTime = "09:00";

    /// <summary>Girilen saat geçersiz (alan kırmızı çizilir).</summary>
    [ObservableProperty]
    private bool _newEventTimeInvalid;

    // --- Araçlar ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPomodoroTool), nameof(IsTimerTool), nameof(IsStopwatchTool), nameof(ToolTitle))]
    private CalendarTool _tool = CalendarTool.Pomodoro;

    public bool IsPomodoroTool => Tool == CalendarTool.Pomodoro;
    public bool IsTimerTool => Tool == CalendarTool.Timer;
    public bool IsStopwatchTool => Tool == CalendarTool.Stopwatch;

    public string ToolTitle => Tool switch
    {
        CalendarTool.Timer => Strings.Get("CalToolTimer"),
        CalendarTool.Stopwatch => Strings.Get("CalToolStopwatch"),
        _ => Strings.Get("CalToolPomodoro")
    };

    // Pomodoro
    [ObservableProperty]
    private string _pomodoroTimeText = "25:00";

    [ObservableProperty]
    private string _pomodoroModeText = "Çalışma";

    [ObservableProperty]
    private bool _isPomodoroRunning;

    [ObservableProperty]
    private int _completedSessions;

    // Serbest zamanlayıcı
    [ObservableProperty]
    private string _countdownText = "";

    [ObservableProperty]
    private bool _isCountdownRunning;

    [ObservableProperty]
    private bool _isCountdownFinished;

    /// <summary>Süre yalnız çalışmıyorken değiştirilir.</summary>
    [ObservableProperty]
    private bool _canEditCountdown = true;

    public IReadOnlyList<PresetChip> CountdownPresetChips { get; } =
        CountdownPresets.Minutes.Select(m => new PresetChip(m, string.Format(Strings.Get("TimerPresetMinutes"), m))).ToList();

    // Kronometre
    [ObservableProperty]
    private string _stopwatchText = "00:00.0";

    [ObservableProperty]
    private bool _isStopwatchRunning;

    [ObservableProperty]
    private bool _canLap;

    public ObservableCollection<LapRow> Laps { get; } = [];

    public bool HasLaps => Laps.Count > 0;

    // --- Ana görünüm özeti ---
    /// <summary>Bugünün sıradaki etkinliği ("14:00 Toplantı"); yoksa boş.</summary>
    [ObservableProperty]
    private string _nextEventText = "";

    /// <summary>Çalışan zamanlayıcı özeti ("Zamanlayıcı 04:12"); hiçbiri çalışmıyorsa boş.</summary>
    [ObservableProperty]
    private string _activeTimerText = "";

    public bool HasNextEvent => NextEventText.Length > 0;
    public bool HasActiveTimer => ActiveTimerText.Length > 0;

    /// <summary>Zamanlayıcı bitti (Pomodoro oturumu ya da serbest geri sayım): çentik kısa süre Peek'e geçer ve titrer.</summary>
    public event Action? TimerFinished;

    public CalendarViewModel(AppSettings settings, SharedEvents? events = null, Dispatcher? dispatcher = null, TimeProvider? timeProvider = null)
    {
        _settings = settings;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
        _events = events ?? new SharedEvents();
        _culture = new CultureInfo(settings.Language == "en" ? "en-US" : "tr-TR");
        WeekdayHeaders = BuildWeekdayHeaders(_culture);

        _pomodoro = new Pomodoro(PomodoroConfig.FromMinutes(
            settings.PomodoroWorkMinutes, settings.PomodoroShortBreakMinutes, settings.PomodoroLongBreakMinutes), timeProvider);
        _pomodoro.Ticked += OnPomodoroTicked;
        _pomodoro.StateChanged += OnPomodoroStateChanged;
        _pomodoro.ModeChanged += OnPomodoroModeChanged;
        _pomodoro.SessionCompleted += OnPomodoroSessionCompleted;

        _countdown = new CountdownTimer(TimeSpan.FromMinutes(CountdownPresets.DefaultMinutes), timeProvider);
        _countdown.StateChanged += OnCountdownStateChanged;
        _countdown.Finished += OnCountdownFinished;

        _stopwatch.StateChanged += OnStopwatchStateChanged;
        _stopwatch.LapsChanged += OnStopwatchLapsChanged;

        _displayTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher);
        _displayTimer.Tick += OnDisplayTick;

        // Etkinlik defteri bu noktada okunmaz (disk işi görünür olunca yapılır); yalnız ekran metni kurulur.
        RefreshNavigation();
        UpdatePomodoroUI();
        UpdateCountdownUI();
        UpdateStopwatchUI();
    }

    // ---------------------------------------------------------------------------------------------
    // Görünürlük: tazeleme zamanlayıcısı yalnız ilgili panel açıkken ve gerçekten bir şey değişiyorsa çalışır.
    // ---------------------------------------------------------------------------------------------

    /// <param name="calendarVisible">Takvim sekmesi açık.</param>
    /// <param name="homeVisible">Ana görünüm açık (özet satırlarını gösterir).</param>
    public void SetVisible(bool calendarVisible, bool homeVisible)
    {
        var wasVisible = _calendarVisible || _homeVisible;
        _calendarVisible = calendarVisible;
        _homeVisible = homeVisible;

        if (_calendarVisible || _homeVisible)
        {
            if (!wasVisible)
            {
                _events.Changed += OnEventsChanged;
                // Gün değişmiş olabilir (uygulama gece boyunca açık kaldı).
                Anchor = DateOnly.FromDateTime(DateTime.Today);
            }

            RefreshNavigation();
            UpdateDisplay();
        }
        else if (wasVisible)
        {
            _events.Changed -= OnEventsChanged;
        }

        RestartDisplayTimer();
    }

    private void OnEventsChanged(object? sender, EventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            RefreshNavigation();
            UpdateAgenda();
        });
    }

    private void OnDisplayTick(object? sender, EventArgs e)
    {
        UpdateDisplay();
        RestartDisplayTimer();
    }

    // Çalışan bir şey yoksa ve ana görünüm de kapalıysa tik yok. Kronometre yalnız kendi aracı görünürken
    // saniyenin onda biriyle çizilir; diğer durumlarda saniye başına bir kez ("Zamanlayıcı 04:12" özeti için).
    private void RestartDisplayTimer()
    {
        if (_disposed || !(_calendarVisible || _homeVisible))
        {
            _displayTimer.Stop();
            return;
        }

        var stopwatchRunning = _stopwatch.State == StopwatchState.Running;
        var countdownRunning = _countdown.State == CountdownState.Running;

        TimeSpan? interval = null;
        if (stopwatchRunning && _calendarVisible && Tool == CalendarTool.Stopwatch)
        {
            interval = FastTick;
        }
        else if (stopwatchRunning || countdownRunning)
        {
            var now = DateTime.Now;
            interval = TimeSpan.FromMilliseconds(1000 - now.Millisecond + 20);
        }
        else if (_homeVisible)
        {
            // Yalnız "sıradaki etkinlik" satırı: bir sonraki dakika başında tazelenir.
            var now = DateTime.Now;
            interval = TimeSpan.FromSeconds(60 - now.Second) + TimeSpan.FromMilliseconds(100);
        }

        if (interval is null)
        {
            _displayTimer.Stop();
            return;
        }

        _displayTimer.Stop();
        _displayTimer.Interval = interval.Value;
        _displayTimer.Start();
    }

    private void UpdateDisplay()
    {
        UpdateCountdownUI();
        UpdateStopwatchUI();
        UpdateAgenda();
    }

    // ---------------------------------------------------------------------------------------------
    // Gezinme: ay, hafta, gün
    // ---------------------------------------------------------------------------------------------

    private void RefreshNavigation()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        switch (ViewMode)
        {
            case CalendarViewMode.Month:
                HeaderTitle = Anchor.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", _culture);
                Days = MonthGrid.Build(Anchor.Year, Anchor.Month, today);
                break;

            case CalendarViewMode.Week:
            {
                var week = EventAgenda.ForWeek(_calendarVisible || _homeVisible ? _events.Book.Events : [], Anchor, today);
                var first = week[0].Date.ToDateTime(TimeOnly.MinValue);
                var last = week[6].Date.ToDateTime(TimeOnly.MinValue);
                HeaderTitle = first.Month == last.Month
                    ? $"{first.Day} – {last.ToString("d MMM", _culture)}"
                    : $"{first.ToString("d MMM", _culture)} – {last.ToString("d MMM", _culture)}";
                WeekDays = week.Select(BuildWeekRow).ToList();
                break;
            }

            default:
            {
                HeaderTitle = Anchor.ToDateTime(TimeOnly.MinValue).ToString("d MMMM dddd", _culture);
                var events = _calendarVisible || _homeVisible ? EventAgenda.ForDay(_events.Book.Events, Anchor) : [];
                DayEvents.Clear();
                foreach (var e in events)
                    DayEvents.Add(new EventRow(e.Id, e.Start.ToString("HH:mm", _culture), e.Title));
                OnPropertyChanged(nameof(HasDayEvents));
                break;
            }
        }
    }

    private WeekDayRow BuildWeekRow(DayAgenda day)
    {
        var date = day.Date.ToDateTime(TimeOnly.MinValue);
        var label = date.ToString("ddd d", _culture);
        string summary;
        if (day.Events.Count == 0)
        {
            summary = "";
        }
        else
        {
            var first = day.Events[0];
            summary = $"{first.Start.ToString("HH:mm", _culture)} {first.Title}";
            if (day.Events.Count > 1)
                summary += $"  +{day.Events.Count - 1}";
        }

        return new WeekDayRow(day.Date, label, day.IsToday, summary, day.Events.Count > 0);
    }

    private static IReadOnlyList<string> BuildWeekdayHeaders(CultureInfo culture)
    {
        // Pazartesi'den başlar (MonthGrid ile aynı sıra). Kültürün "en kısa" adları Türkçede tek harf (P, S, Ç, P…)
        // olup belirsiz kaldığı için iki harfli adlar sabit tutulur.
        return culture.Name.StartsWith("tr", StringComparison.OrdinalIgnoreCase)
            ? ["Pt", "Sa", "Ça", "Pe", "Cu", "Ct", "Pz"]
            : ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];
    }

    partial void OnViewModeChanged(CalendarViewMode value) => RefreshNavigation();

    [RelayCommand]
    private void SetView(CalendarViewMode mode) => ViewMode = mode;

    [RelayCommand]
    private void Previous() => Move(-1);

    [RelayCommand]
    private void Next() => Move(1);

    private void Move(int direction)
    {
        Anchor = ViewMode switch
        {
            CalendarViewMode.Month => new DateOnly(Anchor.Year, Anchor.Month, 1).AddMonths(direction),
            CalendarViewMode.Week => Anchor.AddDays(7 * direction),
            _ => Anchor.AddDays(direction)
        };
        RefreshNavigation();
    }

    [RelayCommand]
    private void GoToday()
    {
        Anchor = DateOnly.FromDateTime(DateTime.Today);
        RefreshNavigation();
    }

    /// <summary>Ay ızgarasındaki ya da hafta satırındaki güne tıklanınca o günün görünümü açılır.</summary>
    [RelayCommand]
    private void OpenDay(DateOnly date)
    {
        Anchor = date;
        ViewMode = CalendarViewMode.Day;
    }

    // ---------------------------------------------------------------------------------------------
    // Etkinlik ekleme / silme (yalnız yerel)
    // ---------------------------------------------------------------------------------------------

    [RelayCommand]
    private void AddEvent()
    {
        NewEventTimeInvalid = !EventAgenda.TryParseTime(NewEventTime, out var time);
        if (NewEventTimeInvalid)
            return;

        if (_events.Book.Add(NewEventTitle, Anchor, time) is null)
            return;

        NewEventTitle = "";
    }

    [RelayCommand]
    private void DeleteEvent(string id) => _events.Book.Remove(id);

    partial void OnNewEventTimeChanged(string value) => NewEventTimeInvalid = false;

    private void UpdateAgenda()
    {
        var events = _calendarVisible || _homeVisible ? _events.Book.Events : [];
        var next = EventAgenda.NextToday(events, DateTime.Now);
        NextEventText = next is null ? "" : $"{next.Start.ToString("HH:mm", _culture)}  {next.Title}";

        ActiveTimerText = _countdown.State == CountdownState.Running
            ? string.Format(Strings.Get("HomeTimerLabel"), DurationFormat.Countdown(_countdown.Remaining))
            : _stopwatch.State == StopwatchState.Running
                ? string.Format(Strings.Get("HomeStopwatchLabel"), DurationFormat.Stopwatch(_stopwatch.Elapsed)[..^2])
                : _pomodoro.State == PomodoroState.Running
                    ? string.Format(Strings.Get("HomePomodoroLabel"), PomodoroTimeText)
                    : "";

        OnPropertyChanged(nameof(HasNextEvent));
        OnPropertyChanged(nameof(HasActiveTimer));
    }

    // ---------------------------------------------------------------------------------------------
    // Araç seçimi
    // ---------------------------------------------------------------------------------------------

    [RelayCommand]
    private void NextTool() => SetTool(((int)Tool + 1) % 3);

    [RelayCommand]
    private void PreviousTool() => SetTool(((int)Tool + 2) % 3);

    private void SetTool(int index)
    {
        Tool = (CalendarTool)index;
        UpdateDisplay();
        RestartDisplayTimer();
    }

    // ---------------------------------------------------------------------------------------------
    // Pomodoro (Core/Timers/Pomodoro)
    // ---------------------------------------------------------------------------------------------

    private void UpdatePomodoroUI()
    {
        PomodoroTimeText = DurationFormat.Countdown(_pomodoro.Remaining);
        PomodoroModeText = _pomodoro.Mode switch
        {
            PomodoroMode.Work => Strings.Work,
            PomodoroMode.ShortBreak => Strings.ShortBreak,
            PomodoroMode.LongBreak => Strings.LongBreak,
            _ => "Pomodoro"
        };
        IsPomodoroRunning = _pomodoro.State == PomodoroState.Running;
        CompletedSessions = _pomodoro.CompletedWorkSessions;
        UpdateAgenda();
    }

    private void OnPomodoroTicked(object? sender, TimeSpan remaining) => _dispatcher.InvokeAsync(UpdatePomodoroUI);

    private void OnPomodoroStateChanged(object? sender, PomodoroState state) => _dispatcher.InvokeAsync(UpdatePomodoroUI);

    private void OnPomodoroModeChanged(object? sender, PomodoroMode mode) => _dispatcher.InvokeAsync(UpdatePomodoroUI);

    private void OnPomodoroSessionCompleted(object? sender, PomodoroMode mode)
    {
        _dispatcher.InvokeAsync(() =>
        {
            UpdatePomodoroUI();
            TimerFinished?.Invoke();
        });
    }

    [RelayCommand]
    private void TogglePomodoro()
    {
        if (_pomodoro.State == PomodoroState.Running)
            _pomodoro.Pause();
        else
            _pomodoro.Start();
    }

    [RelayCommand]
    private void ResetPomodoro() => _pomodoro.Reset();

    [RelayCommand]
    private void SkipPomodoro() => _pomodoro.Skip();

    // ---------------------------------------------------------------------------------------------
    // Serbest geri sayım zamanlayıcısı (Core/Timers/CountdownTimer)
    // ---------------------------------------------------------------------------------------------

    private void UpdateCountdownUI()
    {
        CountdownText = DurationFormat.Countdown(_countdown.Remaining);
        IsCountdownRunning = _countdown.State == CountdownState.Running;
        IsCountdownFinished = _countdown.State == CountdownState.Finished;
        CanEditCountdown = _countdown.State != CountdownState.Running;
    }

    private void OnCountdownStateChanged(object? sender, CountdownState state)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            UpdateCountdownUI();
            UpdateAgenda();
            RestartDisplayTimer();
        });
    }

    private void OnCountdownFinished(object? sender, EventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (!_disposed)
                TimerFinished?.Invoke();
        });
    }

    [RelayCommand]
    private void ToggleCountdown()
    {
        if (_countdown.State == CountdownState.Running)
            _countdown.Pause();
        else
            _countdown.Start();
    }

    [RelayCommand]
    private void ResetCountdown() => _countdown.Reset();

    [RelayCommand]
    private void IncreaseCountdown() => _countdown.SetDuration(CountdownPresets.Step(_countdown.Duration, true));

    [RelayCommand]
    private void DecreaseCountdown() => _countdown.SetDuration(CountdownPresets.Step(_countdown.Duration, false));

    [RelayCommand]
    private void SetCountdownMinutes(int minutes) => _countdown.SetDuration(TimeSpan.FromMinutes(minutes));

    // ---------------------------------------------------------------------------------------------
    // Kronometre (Core/Timers/LapStopwatch)
    // ---------------------------------------------------------------------------------------------

    private void UpdateStopwatchUI()
    {
        StopwatchText = DurationFormat.Stopwatch(_stopwatch.Elapsed);
        IsStopwatchRunning = _stopwatch.State == StopwatchState.Running;
        CanLap = IsStopwatchRunning;
    }

    private void OnStopwatchStateChanged(object? sender, StopwatchState state)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            UpdateStopwatchUI();
            UpdateAgenda();
            RestartDisplayTimer();
        });
    }

    private void OnStopwatchLapsChanged(object? sender, EventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            Laps.Clear();
            foreach (var lap in _stopwatch.Laps.Take(20))
                Laps.Add(new LapRow(
                    string.Format(Strings.Get("TimerLapRow"), lap.Number),
                    DurationFormat.Stopwatch(lap.Total),
                    "+" + DurationFormat.Stopwatch(lap.Split)));
            OnPropertyChanged(nameof(HasLaps));
        });
    }

    [RelayCommand]
    private void ToggleStopwatch()
    {
        if (_stopwatch.State == StopwatchState.Running)
            _stopwatch.Pause();
        else
            _stopwatch.Start();
    }

    [RelayCommand]
    private void LapStopwatch() => _stopwatch.Lap();

    [RelayCommand]
    private void ResetStopwatch() => _stopwatch.Reset();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _displayTimer.Stop();
        _displayTimer.Tick -= OnDisplayTick;
        _events.Changed -= OnEventsChanged;

        _pomodoro.Ticked -= OnPomodoroTicked;
        _pomodoro.StateChanged -= OnPomodoroStateChanged;
        _pomodoro.ModeChanged -= OnPomodoroModeChanged;
        _pomodoro.SessionCompleted -= OnPomodoroSessionCompleted;
        _pomodoro.Dispose();

        _countdown.StateChanged -= OnCountdownStateChanged;
        _countdown.Finished -= OnCountdownFinished;
        _countdown.Dispose();

        _stopwatch.StateChanged -= OnStopwatchStateChanged;
        _stopwatch.LapsChanged -= OnStopwatchLapsChanged;
    }
}
