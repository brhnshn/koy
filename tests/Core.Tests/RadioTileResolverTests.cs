using Koy.Core.Radio;
using Xunit;

namespace Koy.Core.Tests;

public class RadioTileResolverTests
{
    [Theory]
    [InlineData(RadioState.On, true, RadioTileStatus.On)]
    [InlineData(RadioState.Off, false, RadioTileStatus.Off)]
    public void Resolve_ControllableRadio_TogglesAndShowsState(RadioState state, bool isOn, RadioTileStatus status)
    {
        var tile = RadioTileResolver.Resolve(RadioAccess.Allowed, state);

        Assert.Equal(status, tile.Status);
        Assert.Equal(isOn, tile.IsOn);
        Assert.Equal(RadioTileAction.Toggle, tile.Action);
        Assert.False(tile.IsUnavailable);
    }

    [Theory]
    [InlineData(RadioAccess.Unknown)]
    [InlineData(RadioAccess.Allowed)]
    public void Resolve_NotScannedYet_IsLoadingAndIgnoresClicks(RadioAccess access)
    {
        var tile = RadioTileResolver.Resolve(access, RadioState.Unknown);

        Assert.Equal(RadioTileStatus.Loading, tile.Status);
        Assert.Equal(RadioTileAction.None, tile.Action);
        Assert.False(tile.IsOn);
        Assert.False(tile.IsUnavailable);
    }

    [Theory]
    [InlineData(RadioAccess.DeniedByUser, RadioState.NotPresent)]
    [InlineData(RadioAccess.DeniedBySystem, RadioState.NotPresent)]
    [InlineData(RadioAccess.DeniedByUser, RadioState.On)]
    public void Resolve_AccessDenied_OpensPrivacySettingsInsteadOfFakeToggle(RadioAccess access, RadioState state)
    {
        var tile = RadioTileResolver.Resolve(access, state);

        Assert.Equal(RadioTileStatus.NoAccess, tile.Status);
        Assert.Equal(RadioTileAction.OpenPrivacySettings, tile.Action);
        Assert.False(tile.IsOn);
        Assert.True(tile.IsUnavailable);
    }

    [Fact]
    public void Resolve_NoAdapter_SaysNotFoundAndOpensSettings()
    {
        var tile = RadioTileResolver.Resolve(RadioAccess.Allowed, RadioState.NotPresent);

        Assert.Equal(RadioTileStatus.NotFound, tile.Status);
        Assert.Equal(RadioTileAction.OpenSettings, tile.Action);
        Assert.True(tile.IsUnavailable);
    }

    [Fact]
    public void Resolve_ScanFailedWithUnknownAccess_StillShowsNotFound()
    {
        // Radyo yığını hata verirse izin bilinmez ama tür "yok" sayılır; döşeme sonsuza dek yüklenmez.
        var tile = RadioTileResolver.Resolve(RadioAccess.Unknown, RadioState.NotPresent);

        Assert.Equal(RadioTileStatus.NotFound, tile.Status);
        Assert.Equal(RadioTileAction.OpenSettings, tile.Action);
    }

    [Fact]
    public void Resolve_HardwareSwitchOff_SaysDisabledAndOpensSettings()
    {
        var tile = RadioTileResolver.Resolve(RadioAccess.Allowed, RadioState.Disabled);

        Assert.Equal(RadioTileStatus.HardwareOff, tile.Status);
        Assert.Equal(RadioTileAction.OpenSettings, tile.Action);
        Assert.False(tile.IsOn);
        Assert.True(tile.IsUnavailable);
    }

    [Theory]
    [InlineData(RadioState.On, true)]
    [InlineData(RadioState.Off, false)]
    public void Resolve_LastToggleFailed_ShowsFailureButKeepsRealStateAndRetry(RadioState state, bool isOn)
    {
        var tile = RadioTileResolver.Resolve(RadioAccess.Allowed, state, lastToggleFailed: true);

        Assert.Equal(RadioTileStatus.Failed, tile.Status);
        Assert.Equal(isOn, tile.IsOn);
        Assert.Equal(RadioTileAction.Toggle, tile.Action);
    }

    [Fact]
    public void Resolve_FailureFlagIgnoredWhenRadioUnavailable()
    {
        var tile = RadioTileResolver.Resolve(RadioAccess.Allowed, RadioState.NotPresent, lastToggleFailed: true);

        Assert.Equal(RadioTileStatus.NotFound, tile.Status);
    }

    [Theory]
    [InlineData(RadioAccess.Allowed, RadioState.On, true)]
    [InlineData(RadioAccess.Allowed, RadioState.Off, true)]
    [InlineData(RadioAccess.Allowed, RadioState.Disabled, true)]
    [InlineData(RadioAccess.Allowed, RadioState.NotPresent, false)]
    [InlineData(RadioAccess.DeniedByUser, RadioState.On, false)]
    [InlineData(RadioAccess.Unknown, RadioState.Unknown, false)]
    public void IsPresent_ShowsNotchIconOnlyForKnownExistingRadio(RadioAccess access, RadioState state, bool expected)
    {
        // Çentikte simge yalnız radyo varsa çıkar; Bluetooth'suz kasada boş simge gösterilmez.
        Assert.Equal(expected, RadioTileResolver.Resolve(access, state).IsPresent);
    }

    [Fact]
    public void IsPresent_StaysTrueWhenToggleFailed()
    {
        Assert.True(RadioTileResolver.Resolve(RadioAccess.Allowed, RadioState.On, lastToggleFailed: true).IsPresent);
    }
}
