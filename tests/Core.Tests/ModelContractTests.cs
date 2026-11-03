using Koy.Core.Audio;
using Koy.Core.Display;
using Koy.Core.Notch;
using Koy.Core.Power;
using Koy.Core.Radio;
using Koy.Core.Weather;
using Xunit;

namespace Koy.Core.Tests;

public sealed class ModelContractTests
{
    [Fact]
    public void AudioStateChangedEventArgs_InitializesProperly()
    {
        var args = new AudioStateChangedEventArgs(0.75f, true);
        Assert.Equal(0.75f, args.Volume);
        Assert.True(args.IsMuted);
    }

    [Fact]
    public void RadioStateChangedEventArgs_InitializesProperly()
    {
        var args = new RadioStateChangedEventArgs(RadioKind.WiFi, RadioState.On);
        Assert.Equal(RadioKind.WiFi, args.Kind);
        Assert.Equal(RadioState.On, args.State);
    }

    [Fact]
    public void PowerChangedEventArgs_InitializesProperly()
    {
        var args = new PowerChangedEventArgs(true, 85, true, BatteryStatus.Charging);
        Assert.True(args.HasBattery);
        Assert.Equal(85, args.RemainingPercent);
        Assert.True(args.IsCharging);
        Assert.Equal(BatteryStatus.Charging, args.Status);
    }

    [Fact]
    public void WeatherInfo_InitializesProperly()
    {
        var now = DateTimeOffset.UtcNow;
        var info = new WeatherInfo(21.5, 2, "Parçalı Bulutlu", "PartlyCloudy", now);
        Assert.Equal(21.5, info.Temperature);
        Assert.Equal(2, info.WeatherCode);
        Assert.Equal("Parçalı Bulutlu", info.Description);
        Assert.Equal("PartlyCloudy", info.IconKind);
        Assert.Equal(now, info.FetchedAt);
    }

    [Fact]
    public void NotchController_CyclesThroughAllThreePanels()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Music, NotchPanel.ControlCenter, NotchPanel.Status]);

        Assert.Equal(NotchPanel.Music, controller.CurrentPanel);

        var transitions = new List<int>();
        controller.PanelTransitionTriggered += dir => transitions.Add(dir);

        controller.NextPanel();
        Assert.Equal(NotchPanel.ControlCenter, controller.CurrentPanel);

        controller.NextPanel();
        Assert.Equal(NotchPanel.Status, controller.CurrentPanel);

        controller.NextPanel();
        Assert.Equal(NotchPanel.Music, controller.CurrentPanel);

        controller.PreviousPanel();
        Assert.Equal(NotchPanel.Status, controller.CurrentPanel);

        Assert.Equal([1, 1, 1, -1], transitions);
    }
}
