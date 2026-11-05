using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Calendar;
using Koy.Core.Settings;
using Koy.Core.Timers;

namespace Koy.App.Features.Calendar;

public sealed partial class CalendarViewModel : ObservableObject, IDisposable
{
    private readonly Pomodoro _pomodoro;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;
    private bool _disposed;

    [ObservableProperty]
    private int _year;

    [ObservableProperty]
    private int _month;

    [ObservableProperty]
    private string _monthTitle = "";

    [ObservableProperty]
    private IReadOnlyList<CalendarDay> _days = [];

    [ObservableProperty]
    private string _pomodoroTimeText = "25:00";

    [ObservableProperty]
    private string _pomodoroModeText = "Çalışma";

    [ObservableProperty]
    private bool _isPomodoroRunning;

    [ObservableProperty]
    private int _completedSessions;

    public event Action? TimerFinished;

    public CalendarViewModel(AppSettings settings, Dispatcher? dispatcher = null)
    {
        _settings = settings;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;

        var now = DateTime.Today;
        _year = now.Year;
        _month = now.Month;

        _pomodoro = new Pomodoro(PomodoroConfig.FromMinutes(
            settings.PomodoroWorkMinutes, settings.PomodoroShortBreakMinutes, settings.PomodoroLongBreakMinutes));
        _pomodoro.Ticked += OnPomodoroTicked;
        _pomodoro.StateChanged += OnPomodoroStateChanged;
        _pomodoro.ModeChanged += OnPomodoroModeChanged;
        _pomodoro.SessionCompleted += OnPomodoroSessionCompleted;

        UpdateCalendarGrid();
        UpdatePomodoroUI();
    }

    private void UpdateCalendarGrid()
    {
        var culture = new CultureInfo(_settings.Language == "en" ? "en-US" : "tr-TR");
        var date = new DateTime(Year, Month, 1);
        MonthTitle = date.ToString("MMMM yyyy", culture);
        Days = MonthGrid.Build(Year, Month);
    }

    private void UpdatePomodoroUI()
    {
        PomodoroTimeText = $"{(int)_pomodoro.Remaining.TotalMinutes:D2}:{_pomodoro.Remaining.Seconds:D2}";
        PomodoroModeText = _pomodoro.Mode switch
        {
            PomodoroMode.Work => Resources.Strings.Work,
            PomodoroMode.ShortBreak => Resources.Strings.ShortBreak,
            PomodoroMode.LongBreak => Resources.Strings.LongBreak,
            _ => "Pomodoro"
        };
        IsPomodoroRunning = _pomodoro.State == PomodoroState.Running;
        CompletedSessions = _pomodoro.CompletedWorkSessions;
    }

    private void OnPomodoroTicked(object? sender, TimeSpan remaining)
    {
        _dispatcher.InvokeAsync(UpdatePomodoroUI);
    }

    private void OnPomodoroStateChanged(object? sender, PomodoroState state)
    {
        _dispatcher.InvokeAsync(UpdatePomodoroUI);
    }

    private void OnPomodoroModeChanged(object? sender, PomodoroMode mode)
    {
        _dispatcher.InvokeAsync(UpdatePomodoroUI);
    }

    private void OnPomodoroSessionCompleted(object? sender, PomodoroMode mode)
    {
        _dispatcher.InvokeAsync(() =>
        {
            UpdatePomodoroUI();
            TimerFinished?.Invoke();
        });
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        if (Month == 1)
        {
            Month = 12;
            Year--;
        }
        else
        {
            Month--;
        }
        UpdateCalendarGrid();
    }

    [RelayCommand]
    private void NextMonth()
    {
        if (Month == 12)
        {
            Month = 1;
            Year++;
        }
        else
        {
            Month++;
        }
        UpdateCalendarGrid();
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
    private void ResetPomodoro()
    {
        _pomodoro.Reset();
    }

    [RelayCommand]
    private void SkipPomodoro()
    {
        _pomodoro.Skip();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _pomodoro.Ticked -= OnPomodoroTicked;
        _pomodoro.StateChanged -= OnPomodoroStateChanged;
        _pomodoro.ModeChanged -= OnPomodoroModeChanged;
        _pomodoro.SessionCompleted -= OnPomodoroSessionCompleted;
        _pomodoro.Dispose();
    }
}
