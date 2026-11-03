namespace Koy.Core.Power;

public enum BatteryStatus
{
    NotPresent,
    Discharging,
    Charging,
    Idle
}

/// <summary>
/// Pil ve güç yönetimi sözleşmesi.
/// </summary>
public interface IPowerService : IDisposable
{
    bool HasBattery { get; }
    int RemainingPercent { get; }
    bool IsCharging { get; }
    BatteryStatus Status { get; }

    event EventHandler<PowerChangedEventArgs>? PowerChanged;
}

public sealed record PowerChangedEventArgs(bool HasBattery, int RemainingPercent, bool IsCharging, BatteryStatus Status);
