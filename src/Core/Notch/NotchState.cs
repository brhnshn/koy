namespace Koy.Core.Notch;

/// <summary>
/// Notch'ın görsel durumları.
/// </summary>
public enum NotchState
{
    Collapsed,
    Peek,
    Expanded
}

/// <summary>
/// Durum geçişini tetikleyen olaylar.
/// </summary>
public enum NotchTrigger
{
    MouseEnter,
    MouseLeave,
    Click,
    TrackChanged,
    AutoPeekTimeout,
    MouseLeaveDelayTimeout
}

/// <summary>
/// Saf durum geçiş kuralları (deterministik ve yan etkisiz).
/// </summary>
public static class NotchStateTransitions
{
    public static NotchState Transition(NotchState current, NotchTrigger trigger) =>
        (current, trigger) switch
        {
            (NotchState.Collapsed, NotchTrigger.MouseEnter) => NotchState.Peek,
            (NotchState.Collapsed, NotchTrigger.TrackChanged) => NotchState.Peek,
            (NotchState.Collapsed, NotchTrigger.Click) => NotchState.Expanded,

            (NotchState.Peek, NotchTrigger.Click) => NotchState.Expanded,
            (NotchState.Peek, NotchTrigger.MouseLeave) => NotchState.Collapsed,
            (NotchState.Peek, NotchTrigger.AutoPeekTimeout) => NotchState.Collapsed,

            (NotchState.Expanded, NotchTrigger.Click) => NotchState.Collapsed,
            (NotchState.Expanded, NotchTrigger.MouseLeaveDelayTimeout) => NotchState.Collapsed,

            _ => current
        };
}
