using Koy.Core.Notch;
using Koy.Core.Settings;

namespace Koy.Core.Tests;

public sealed class NotchPanelsTests
{
    [Fact]
    public void DefaultSettings_EnableAllPanels_InTabOrder()
    {
        Assert.Equal(NotchPanels.Order, NotchPanels.ParseEnabled(new AppSettings().EnabledPanels));
    }

    [Fact]
    public void ParseEnabled_AlwaysIncludesHome()
    {
        Assert.Equal([NotchPanel.Home], NotchPanels.ParseEnabled(null));
        Assert.Equal([NotchPanel.Home], NotchPanels.ParseEnabled([]));
        Assert.Equal([NotchPanel.Home, NotchPanel.Shelf], NotchPanels.ParseEnabled(["shelf"]));
    }

    [Fact]
    public void ParseEnabled_IgnoresOrderInSettings()
    {
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.Focus, NotchPanel.Shelf, NotchPanel.ControlCenter],
            NotchPanels.ParseEnabled(["controlcenter", "shelf", "focus", "home"]));
    }

    [Fact]
    public void ParseEnabled_MigratesOldKeys()
    {
        // Eski varsayılan: music, controlcenter, status, calendar, shelf.
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.Focus, NotchPanel.Calendar, NotchPanel.Shelf, NotchPanel.ControlCenter],
            NotchPanels.ParseEnabled(["music", "controlcenter", "status", "calendar", "shelf"]));
    }

    [Fact]
    public void ParseEnabled_CalendarKey_GoesToCalendar_AndOnlyLegacyFilesAlsoGetFocus()
    {
        // H dosyası: "calendar" yalnız Takvim sekmesini açar, Odak kendiliğinden gelmez.
        Assert.Equal([NotchPanel.Home, NotchPanel.Calendar], NotchPanels.ParseEnabled(["home", "calendar"]));
        // H öncesi dosya ("music"/"status" var): eski takvim paneli Pomodoro da taşıdığı için Odak da açılır.
        Assert.Equal([NotchPanel.Home, NotchPanel.Focus, NotchPanel.Calendar], NotchPanels.ParseEnabled(["music", "calendar"]));
        Assert.Equal([NotchPanel.Home], NotchPanels.ParseEnabled(["music", "status"]));
    }

    [Fact]
    public void ParseEnabled_OldSettingsFiles_DoNotGetNewTabs()
    {
        // H öncesi varsayılan: Pano ve Sistem anahtarı yok, sekmeler kendiliğinden açılmaz.
        var panels = NotchPanels.ParseEnabled(["home", "focus", "shelf", "controlcenter"]);
        Assert.DoesNotContain(NotchPanel.Clipboard, panels);
        Assert.DoesNotContain(NotchPanel.System, panels);
    }

    [Fact]
    public void ParseEnabled_ReadsClipboardAndSystemKeys_InTabOrder()
    {
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.Clipboard, NotchPanel.System, NotchPanel.ControlCenter],
            NotchPanels.ParseEnabled(["controlcenter", "system", "clipboard"]));
    }

    [Fact]
    public void ParseEnabled_ClipboardHistoryOff_HidesClipboardTabOnly()
    {
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.System],
            NotchPanels.ParseEnabled(["clipboard", "system"], clipboardHistoryEnabled: false));
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.Clipboard, NotchPanel.System],
            NotchPanels.ParseEnabled(["clipboard", "system"], clipboardHistoryEnabled: true));
    }

    [Fact]
    public void DefaultSettings_IncludeClipboardAndSystem()
    {
        var keys = new AppSettings().EnabledPanels;
        Assert.Contains("clipboard", keys);
        Assert.Contains("system", keys);
        Assert.True(new AppSettings().ClipboardHistoryEnabled);
    }

    [Fact]
    public void SettingsFile_WithoutNewPanelKeys_LoadsAndKeepsItsList()
    {
        var dir = Path.Combine(Path.GetTempPath(), "koy-panels-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "settings.json");
            File.WriteAllText(path, """{ "EnabledPanels": ["home", "focus", "shelf", "controlcenter"] }""");

            var loaded = new SettingsStore(path).Load();

            Assert.Equal(["home", "focus", "shelf", "controlcenter"], loaded.EnabledPanels);
            Assert.True(loaded.ClipboardHistoryEnabled);
            Assert.Equal(
                [NotchPanel.Home, NotchPanel.Focus, NotchPanel.Shelf, NotchPanel.ControlCenter],
                NotchPanels.ParseEnabled(loaded.EnabledPanels, loaded.ClipboardHistoryEnabled));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void ParseEnabled_SkipsUnknownKeysAndIgnoresCase()
    {
        Assert.Equal(
            [NotchPanel.Home, NotchPanel.ControlCenter],
            NotchPanels.ParseEnabled(["status", "pano", "", "ControlCenter"]));
    }

    [Fact]
    public void ToKey_RoundTrips()
    {
        foreach (var panel in NotchPanels.Order)
        {
            Assert.True(NotchPanels.TryParse(NotchPanels.ToKey(panel), out var parsed));
            Assert.Equal(panel, parsed);
        }
    }

    [Fact]
    public void Expand_ToDisabledPanel_KeepsTabOrder()
    {
        using var controller = new NotchController();
        controller.SetEnabledPanels([NotchPanel.Home, NotchPanel.ControlCenter]);

        controller.Expand(NotchPanel.Shelf);

        Assert.Equal([NotchPanel.Home, NotchPanel.Shelf, NotchPanel.ControlCenter], controller.EnabledPanels);
    }
}

public sealed class NotchPinTests
{
    [Fact]
    public void Pinned_Expanded_DoesNotCollapseOnMouseLeave()
    {
        var time = new StepTimeProvider();
        using var controller = new NotchController(time);
        controller.Expand();
        controller.OnMouseEnter();
        controller.SetPinned(true);

        controller.OnMouseLeave();
        time.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(NotchState.Expanded, controller.State);
        Assert.True(controller.IsPinned);
    }

    [Fact]
    public void Unpinning_WithMouseOutside_CollapsesAfterTheUsualDelay()
    {
        var time = new StepTimeProvider();
        using var controller = new NotchController(time);
        controller.Expand();
        controller.SetPinned(true);
        controller.OnMouseLeave();

        controller.SetPinned(false);
        time.Advance(TimeSpan.FromMilliseconds(500));

        Assert.Equal(NotchState.Collapsed, controller.State);
    }

    [Fact]
    public void Collapsing_ClearsPin()
    {
        using var controller = new NotchController();
        controller.Expand();
        controller.SetPinned(true);

        controller.OnClick(); // Expanded -> Collapsed

        Assert.Equal(NotchState.Collapsed, controller.State);
        Assert.False(controller.IsPinned);
    }

    [Fact]
    public void CannotPin_WhenNotExpanded()
    {
        using var controller = new NotchController();

        controller.TogglePinned();

        Assert.False(controller.IsPinned);
    }
}
