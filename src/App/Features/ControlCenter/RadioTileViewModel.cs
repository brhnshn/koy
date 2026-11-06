using CommunityToolkit.Mvvm.ComponentModel;
using Koy.App.Resources;
using Koy.Core.Radio;

namespace Koy.App.Features.ControlCenter;

/// <summary>
/// Wi-Fi ya da Bluetooth döşemesinin görünen durumu. Karar <see cref="RadioTileResolver"/>'da verilir.
/// </summary>
public sealed partial class RadioTileViewModel : ObservableObject
{
    public RadioKind Kind { get; }

    public RadioTileState State { get; private set; }

    [ObservableProperty]
    private bool _isOn;

    [ObservableProperty]
    private bool _isUnavailable;

    /// <summary>Kapalı çentikteki durum simgesi gösterilsin mi (radyo var ve durumu biliniyor).</summary>
    [ObservableProperty]
    private bool _isPresent;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string? _toolTip;

    public RadioTileViewModel(RadioKind kind)
    {
        Kind = kind;
        State = RadioTileResolver.Resolve(RadioAccess.Unknown, RadioState.Unknown);
        Apply(State);
    }

    /// <summary>Tek yönlü bağlı anahtarı gerçek duruma döndürür (açma/kapama reddedilince).</summary>
    public void NotifyToggle() => OnPropertyChanged(nameof(IsOn));

    public void Apply(RadioTileState state)
    {
        State = state;
        IsOn = state.IsOn;
        IsUnavailable = state.IsUnavailable;
        IsPresent = state.IsPresent;
        StatusText = state.Status switch
        {
            RadioTileStatus.On => Strings.On,
            RadioTileStatus.Off => Strings.Off,
            RadioTileStatus.NotFound => Strings.RadioNotFound,
            RadioTileStatus.NoAccess => Strings.RadioNoAccess,
            RadioTileStatus.HardwareOff => Strings.RadioDisabled,
            RadioTileStatus.Failed => Strings.RadioToggleFailed,
            _ => Strings.Loading
        };
        ToolTip = state.Action switch
        {
            RadioTileAction.OpenSettings => Strings.RadioOpenSettingsHint,
            RadioTileAction.OpenPrivacySettings => Strings.RadioPrivacyHint,
            _ => null
        };
    }
}
