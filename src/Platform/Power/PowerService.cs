using Koy.Core.Power;
using WinPower = Windows.System.Power.PowerManager;
using WinBatteryStatus = Windows.System.Power.BatteryStatus;
using WinPowerSupply = Windows.System.Power.PowerSupplyStatus;

namespace Koy.Platform.Power;

public sealed class PowerService : IPowerService
{
    private bool _disposed;

    public bool HasBattery { get; private set; }
    public int RemainingPercent { get; private set; } = 100;
    public bool IsCharging { get; private set; }
    public BatteryStatus Status { get; private set; } = BatteryStatus.NotPresent;

    public event EventHandler<PowerChangedEventArgs>? PowerChanged;

    public PowerService()
    {
        Refresh();

        WinPower.BatteryStatusChanged += OnBatteryStatusChanged;
        WinPower.RemainingChargePercentChanged += OnRemainingChargePercentChanged;
        WinPower.PowerSupplyStatusChanged += OnPowerSupplyStatusChanged;
    }

    private void Refresh()
    {
        try
        {
            var winStatus = WinPower.BatteryStatus;
            Status = winStatus switch
            {
                WinBatteryStatus.Charging => BatteryStatus.Charging,
                WinBatteryStatus.Discharging => BatteryStatus.Discharging,
                WinBatteryStatus.Idle => BatteryStatus.Idle,
                _ => BatteryStatus.NotPresent
            };

            HasBattery = Status != BatteryStatus.NotPresent;
            RemainingPercent = WinPower.RemainingChargePercent;
            IsCharging = Status == BatteryStatus.Charging || (HasBattery && WinPower.PowerSupplyStatus == WinPowerSupply.Adequate && RemainingPercent < 100);

            PowerChanged?.Invoke(this, new PowerChangedEventArgs(HasBattery, RemainingPercent, IsCharging, Status));
        }
        catch
        {
            HasBattery = false;
            Status = BatteryStatus.NotPresent;
        }
    }

    private void OnBatteryStatusChanged(object? sender, object e) => Refresh();
    private void OnRemainingChargePercentChanged(object? sender, object e) => Refresh();
    private void OnPowerSupplyStatusChanged(object? sender, object e) => Refresh();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        WinPower.BatteryStatusChanged -= OnBatteryStatusChanged;
        WinPower.RemainingChargePercentChanged -= OnRemainingChargePercentChanged;
        WinPower.PowerSupplyStatusChanged -= OnPowerSupplyStatusChanged;
    }
}
