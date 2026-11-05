using Koy.Core.Power;

namespace Koy.Core.Tests;

public class BatterySaverPolicyTests
{
    [Fact]
    public void Reduces_motion_only_while_discharging_with_setting_on()
    {
        Assert.True(BatterySaverPolicy.ShouldReduceMotion(true, true, BatteryStatus.Discharging));
    }

    [Theory]
    [InlineData(BatteryStatus.Charging)]
    [InlineData(BatteryStatus.Idle)]
    [InlineData(BatteryStatus.NotPresent)]
    public void Keeps_full_motion_on_ac_power_or_without_battery(BatteryStatus status)
    {
        Assert.False(BatterySaverPolicy.ShouldReduceMotion(true, status != BatteryStatus.NotPresent, status));
    }

    [Fact]
    public void Keeps_full_motion_when_setting_is_off()
    {
        Assert.False(BatterySaverPolicy.ShouldReduceMotion(false, true, BatteryStatus.Discharging));
    }

    [Fact]
    public void Desktop_without_battery_never_reduces_motion()
    {
        Assert.False(BatterySaverPolicy.ShouldReduceMotion(true, false, BatteryStatus.Discharging));
    }
}
