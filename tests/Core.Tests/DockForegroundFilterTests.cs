using Koy.Core.Dock;
using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class DockForegroundFilterTests
{
    private const nint Primary = 101;
    private const nint Secondary = 202;

    // Ana ekranda (0–1920) büyütülmüş pencere: Windows kenarlardan 8 px taşırır.
    private static readonly WindowRect MaximizedOnPrimary = new(-8, -8, 1928, 1048);

    [Fact]
    public void Yan_ekrandaki_pencere_bu_dock_icin_bos_sayilir()
    {
        var bounds = DockForegroundFilter.BoundsForDock(MaximizedOnPrimary, Primary, Secondary);
        Assert.True(bounds.IsEmpty);
        Assert.False(DockForegroundFilter.AppliesToDock(Primary, Secondary));
    }

    [Fact]
    public void Kendi_ekranindaki_pencere_oldugu_gibi_verilir()
    {
        Assert.Equal(MaximizedOnPrimary, DockForegroundFilter.BoundsForDock(MaximizedOnPrimary, Primary, Primary));
        Assert.True(DockForegroundFilter.AppliesToDock(Primary, Primary));
    }

    [Fact]
    public void Ekrani_bilinmeyen_pencere_her_dock_a_verilir()
    {
        Assert.Equal(MaximizedOnPrimary, DockForegroundFilter.BoundsForDock(MaximizedOnPrimary, 0, Secondary));
        Assert.True(DockForegroundFilter.AppliesToDock(0, Secondary));
    }

    [Fact]
    public void Tasma_yan_ekrana_bos_sinir_verilince_dock_cakisma_gormez()
    {
        // Yan ekran 1920'den başlıyor: filtre olmadan 1928'lik sağ kenar bu ekranın alt şeridiyle kesişirdi.
        var secondary = new Koy.Core.Display.DisplayMonitor(
            Id: "MONITOR_2",
            Name: "Yan ekran",
            Bounds: new WindowRect(1920, 0, 3456, 864),
            WorkArea: new WindowRect(1920, 0, 3456, 864),
            IsPrimary: false,
            Handle: Secondary);
        var bounds = DockForegroundFilter.BoundsForDock(MaximizedOnPrimary, Primary, Secondary);

        var state = DockPositionResolver.Resolve(
            secondary, DockStyleMode.FloatingCapsule, DockProximityMode.Dodge, 400, bounds);

        Assert.False(state.IsHidden);
        Assert.False(state.IsSnapped);
    }
}
