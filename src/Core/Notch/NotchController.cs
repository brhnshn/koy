namespace Koy.Core.Notch;

/// <summary>
/// Notch durumunu, panellerini ve zamanlayıcılarını yöneten saf iş mantığı denetleyicisi.
/// </summary>
public sealed class NotchController : IDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly List<NotchPanel> _enabledPanels = [NotchPanel.Music];
    private ITimer? _autoPeekTimer;
    private ITimer? _mouseLeaveTimer;
    private bool _disposed;

    public event EventHandler<NotchState>? StateChanged;
    public event EventHandler<NotchPanel>? PanelChanged;
    public event Action<int>? PanelTransitionTriggered;

    public NotchState State { get; private set; } = NotchState.Collapsed;
    public NotchPanel CurrentPanel { get; private set; } = NotchPanel.Music;
    public bool IsMouseOver { get; private set; }
    public IReadOnlyList<NotchPanel> EnabledPanels => _enabledPanels;

    public NotchController(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void SetEnabledPanels(IEnumerable<NotchPanel> panels)
    {
        _enabledPanels.Clear();
        _enabledPanels.AddRange(panels);
        if (_enabledPanels.Count == 0)
            _enabledPanels.Add(NotchPanel.Music);

        if (!_enabledPanels.Contains(CurrentPanel))
            SelectPanel(_enabledPanels[0]);
    }

    public void NextPanel()
    {
        if (_enabledPanels.Count <= 1)
            return;

        var currentIndex = _enabledPanels.IndexOf(CurrentPanel);
        var nextIndex = (currentIndex + 1) % _enabledPanels.Count;
        PanelTransitionTriggered?.Invoke(1);
        SelectPanel(_enabledPanels[nextIndex]);
    }

    public void PreviousPanel()
    {
        if (_enabledPanels.Count <= 1)
            return;

        var currentIndex = _enabledPanels.IndexOf(CurrentPanel);
        var prevIndex = (currentIndex - 1 + _enabledPanels.Count) % _enabledPanels.Count;
        PanelTransitionTriggered?.Invoke(-1);
        SelectPanel(_enabledPanels[prevIndex]);
    }

    public void SelectPanel(NotchPanel panel)
    {
        if (!_enabledPanels.Contains(panel) || CurrentPanel == panel)
            return;

        CurrentPanel = panel;
        PanelChanged?.Invoke(this, CurrentPanel);
    }

    /// <summary>
    /// Çentiği doğrudan Expanded durumuna geçirir ve isteğe bağlı olarak belirtilen paneli seçer.
    /// Sürükle-bırak veya doğrudan kullanıcı etkileşimi için kullanılır.
    /// </summary>
    public void Expand(NotchPanel? panel = null)
    {
        _autoPeekTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _mouseLeaveTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        SetState(NotchState.Expanded);
        if (panel.HasValue)
        {
            if (!_enabledPanels.Contains(panel.Value))
            {
                _enabledPanels.Add(panel.Value);
            }
            SelectPanel(panel.Value);
        }
    }

    public void OnMouseEnter()
    {
        IsMouseOver = true;
        _mouseLeaveTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        if (State == NotchState.Collapsed)
            SetState(NotchStateTransitions.Transition(State, NotchTrigger.MouseEnter));
    }

    public void OnMouseLeave()
    {
        IsMouseOver = false;

        if (State == NotchState.Peek)
        {
            _autoPeekTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            SetState(NotchStateTransitions.Transition(State, NotchTrigger.MouseLeave));
        }
        else if (State == NotchState.Expanded)
        {
            _mouseLeaveTimer?.Dispose();
            _mouseLeaveTimer = _timeProvider.CreateTimer(
                _ => OnMouseLeaveTimeout(),
                null,
                TimeSpan.FromMilliseconds(400),
                Timeout.InfiniteTimeSpan);
        }
    }

    public void OnClick()
    {
        _autoPeekTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _mouseLeaveTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        SetState(NotchStateTransitions.Transition(State, NotchTrigger.Click));
    }

    public void OnTrackChanged()
    {
        if (State == NotchState.Collapsed)
        {
            SetState(NotchStateTransitions.Transition(State, NotchTrigger.TrackChanged));
            ResetAutoPeekTimer();
        }
        else if (State == NotchState.Peek && !IsMouseOver)
        {
            ResetAutoPeekTimer();
        }
    }

    private void ResetAutoPeekTimer()
    {
        _autoPeekTimer?.Dispose();
        _autoPeekTimer = _timeProvider.CreateTimer(
            _ => OnAutoPeekTimeout(),
            null,
            TimeSpan.FromSeconds(3),
            Timeout.InfiniteTimeSpan);
    }

    public void OnAutoPeekTimeout()
    {
        if (State == NotchState.Peek && !IsMouseOver)
            SetState(NotchStateTransitions.Transition(State, NotchTrigger.AutoPeekTimeout));
    }

    public void OnMouseLeaveTimeout()
    {
        if (State == NotchState.Expanded && !IsMouseOver)
            SetState(NotchStateTransitions.Transition(State, NotchTrigger.MouseLeaveDelayTimeout));
    }

    private void SetState(NotchState newState)
    {
        if (State == newState)
            return;

        State = newState;
        StateChanged?.Invoke(this, State);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _autoPeekTimer?.Dispose();
        _mouseLeaveTimer?.Dispose();
        _autoPeekTimer = null;
        _mouseLeaveTimer = null;
    }
}
