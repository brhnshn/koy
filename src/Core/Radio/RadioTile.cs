namespace Koy.Core.Radio;

/// <summary>Kontrol Merkezi'ndeki radyo döşemesinin alt satırında ne yazacağı.</summary>
public enum RadioTileStatus
{
    Loading,
    On,
    Off,
    NotFound,
    NoAccess,
    HardwareOff,
    Failed
}

/// <summary>Döşemeye tıklanınca ne olacağı.</summary>
public enum RadioTileAction
{
    None,
    Toggle,
    OpenSettings,
    OpenPrivacySettings
}

public sealed record RadioTileState(RadioTileStatus Status, bool IsOn, RadioTileAction Action)
{
    /// <summary>Radyo yazılımdan değiştirilemiyor; döşeme soluk gösterilir ve ayarlara yönlendirir.</summary>
    public bool IsUnavailable => Status is RadioTileStatus.NotFound or RadioTileStatus.NoAccess or RadioTileStatus.HardwareOff;
}

/// <summary>
/// Radyo izni ve durumundan döşemenin görünümünü ve tıklama davranışını belirler.
/// Değiştirilemeyen radyo için sahte bir anahtar göstermek yerine nedenini yazar ve ilgili Windows ayarını açar.
/// </summary>
public static class RadioTileResolver
{
    public static RadioTileState Resolve(RadioAccess access, RadioState state, bool lastToggleFailed = false)
    {
        if (access is RadioAccess.DeniedByUser or RadioAccess.DeniedBySystem)
            return new RadioTileState(RadioTileStatus.NoAccess, false, RadioTileAction.OpenPrivacySettings);

        return state switch
        {
            RadioState.On => new RadioTileState(lastToggleFailed ? RadioTileStatus.Failed : RadioTileStatus.On, true, RadioTileAction.Toggle),
            RadioState.Off => new RadioTileState(lastToggleFailed ? RadioTileStatus.Failed : RadioTileStatus.Off, false, RadioTileAction.Toggle),
            RadioState.Disabled => new RadioTileState(RadioTileStatus.HardwareOff, false, RadioTileAction.OpenSettings),
            RadioState.NotPresent => new RadioTileState(RadioTileStatus.NotFound, false, RadioTileAction.OpenSettings),
            _ => new RadioTileState(RadioTileStatus.Loading, false, RadioTileAction.None)
        };
    }
}
