using System.Globalization;
using System.Resources;

namespace Koy.App.Resources;

/// <summary>
/// Yerelleştirilmiş metinlere XAML ve C# üzerinden güçlü tipli erişim sağlar.
/// XAML kullanımı: Text="{x:Static res:Strings.Key}"
/// </summary>
public static class Strings
{
    private static readonly ResourceManager ResourceManager = new("Koy.App.Resources.Strings", typeof(Strings).Assembly);

    public static string Get(string name) => ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    public static string AppTitle => Get(nameof(AppTitle));
    public static string Settings => Get(nameof(Settings));
    public static string Exit => Get(nameof(Exit));
    public static string Music => Get(nameof(Music));
    public static string ControlCenter => Get(nameof(ControlCenter));
    public static string Status => Get(nameof(Status));
    public static string Calendar => Get(nameof(Calendar));
    public static string Shelf => Get(nameof(Shelf));
    public static string NoMediaPlaying => Get(nameof(NoMediaPlaying));
    public static string ClearAll => Get(nameof(ClearAll));
    public static string DragDropPrompt => Get(nameof(DragDropPrompt));
    public static string Work => Get(nameof(Work));
    public static string ShortBreak => Get(nameof(ShortBreak));
    public static string LongBreak => Get(nameof(LongBreak));
    public static string Cancel => Get(nameof(Cancel));
    public static string Save => Get(nameof(Save));
    public static string General => Get(nameof(General));
    public static string Appearance => Get(nameof(Appearance));
    public static string About => Get(nameof(About));
    public static string Language => Get(nameof(Language));
    public static string LanguageDesc => Get(nameof(LanguageDesc));
    public static string RunAtStartup => Get(nameof(RunAtStartup));
    public static string RunAtStartupDesc => Get(nameof(RunAtStartupDesc));
    public static string NotchVisibility => Get(nameof(NotchVisibility));
    public static string NotchVisibilityDesc => Get(nameof(NotchVisibilityDesc));
    public static string EnabledPanels => Get(nameof(EnabledPanels));
    public static string AccentColor => Get(nameof(AccentColor));
    public static string AccentColorDesc => Get(nameof(AccentColorDesc));
    public static string AboutDesc => Get(nameof(AboutDesc));
    public static string CopyDiagnostics => Get(nameof(CopyDiagnostics));
    public static string WiFi => Get(nameof(WiFi));
    public static string Bluetooth => Get(nameof(Bluetooth));
    public static string DoNotDisturb => Get(nameof(DoNotDisturb));
    public static string On => Get(nameof(On));
    public static string Off => Get(nameof(Off));
    public static string Charging => Get(nameof(Charging));
    public static string Loading => Get(nameof(Loading));
    public static string Reset => Get(nameof(Reset));
    public static string Start => Get(nameof(Start));
    public static string Pause => Get(nameof(Pause));
    public static string NextPhase => Get(nameof(NextPhase));
    public static string Dock => Get(nameof(Dock));
    public static string DockEnabled => Get(nameof(DockEnabled));
    public static string DockEnabledDesc => Get(nameof(DockEnabledDesc));
    public static string DockHoverMagnify => Get(nameof(DockHoverMagnify));
    public static string DockHoverMagnifyDesc => Get(nameof(DockHoverMagnifyDesc));
    public static string LaunchNewInstance => Get(nameof(LaunchNewInstance));
    public static string Close => Get(nameof(Close));
    public static string EndTask => Get(nameof(EndTask));
    public static string StartMenu => Get(nameof(StartMenu));
    public static string SystemTray => Get(nameof(SystemTray));
    public static string QuickSettings => Get(nameof(QuickSettings));
    public static string NotificationCenter => Get(nameof(NotificationCenter));
    public static string VolumeLevel => Get(nameof(VolumeLevel));
    public static string VolumeMuted => Get(nameof(VolumeMuted));
    public static string BatteryLevel => Get(nameof(BatteryLevel));
    public static string BatteryCharging => Get(nameof(BatteryCharging));
    public static string NetworkWiFi => Get(nameof(NetworkWiFi));
    public static string NetworkEthernet => Get(nameof(NetworkEthernet));
    public static string NetworkCellular => Get(nameof(NetworkCellular));
    public static string NetworkNoInternet => Get(nameof(NetworkNoInternet));
    public static string NetworkDisconnected => Get(nameof(NetworkDisconnected));
    public static string TargetMonitor => Get(nameof(TargetMonitor));
    public static string TargetMonitorDesc => Get(nameof(TargetMonitorDesc));
}
