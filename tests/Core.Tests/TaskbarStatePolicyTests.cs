using Koy.Core.Shell;

namespace Koy.Core.Tests;

public class TaskbarStatePolicyTests
{
    [Fact]
    public void ChoosePreviousState_KayitYoksaAnlikDurumuKullanir()
    {
        Assert.Equal(TaskbarStatePolicy.AlwaysOnTop, TaskbarStatePolicy.ChoosePreviousState(null, TaskbarStatePolicy.AlwaysOnTop));
    }

    [Fact]
    public void ChoosePreviousState_ZatenGizliyseKayitliOncekiDurumuKorur()
    {
        // İkinci Hide(): anlık durum bizim koyduğumuz AUTOHIDE; kullanıcının gerçek durumu AlwaysOnTop kalmalı.
        var saved = new TaskbarSavedState(true, TaskbarStatePolicy.AlwaysOnTop);

        var chosen = TaskbarStatePolicy.ChoosePreviousState(saved, TaskbarStatePolicy.AutoHide);

        Assert.Equal(TaskbarStatePolicy.AlwaysOnTop, chosen);
    }

    [Fact]
    public void ChoosePreviousState_KayitGizliDegilseAnlikDurumuKullanir()
    {
        var saved = new TaskbarSavedState(false, TaskbarStatePolicy.AlwaysOnTop);

        Assert.Equal(TaskbarStatePolicy.AutoHide, TaskbarStatePolicy.ChoosePreviousState(saved, TaskbarStatePolicy.AutoHide));
    }

    [Fact]
    public void ChoosePreviousState_IkiKezHideDurumuEzmez()
    {
        int current = TaskbarStatePolicy.AlwaysOnTop;
        TaskbarSavedState? file = null;

        // 1. Hide
        var prev = TaskbarStatePolicy.ChoosePreviousState(file, current);
        file = new TaskbarSavedState(true, prev);
        current = TaskbarStatePolicy.AutoHide; // Hide AUTOHIDE koydu

        // 2. Hide
        prev = TaskbarStatePolicy.ChoosePreviousState(file, current);
        file = new TaskbarSavedState(true, prev);

        Assert.Equal(TaskbarStatePolicy.AlwaysOnTop, TaskbarStatePolicy.ResolveRestoreState(file));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResolveRestoreState_GecersizKayitAlwaysOnTopaDuser(int previous)
    {
        Assert.Equal(TaskbarStatePolicy.AlwaysOnTop, TaskbarStatePolicy.ResolveRestoreState(new TaskbarSavedState(true, previous)));
        Assert.Equal(TaskbarStatePolicy.AlwaysOnTop, TaskbarStatePolicy.ResolveRestoreState(null));
    }

    [Fact]
    public void ResolveRestoreState_KullanicininAutoHideTercihiniKorur()
    {
        Assert.Equal(TaskbarStatePolicy.AutoHide, TaskbarStatePolicy.ResolveRestoreState(new TaskbarSavedState(true, TaskbarStatePolicy.AutoHide)));
    }

    [Fact]
    public void SerializeParse_GidisDonus()
    {
        var json = TaskbarStatePolicy.Serialize(new TaskbarSavedState(true, 3), new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc));

        var parsed = TaskbarStatePolicy.Parse(json);

        Assert.Equal(new TaskbarSavedState(true, 3), parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bozuk")]
    [InlineData("{\"previousState\": abc}")]
    public void Parse_BozukIcerikNullDoner(string? content)
    {
        Assert.Null(TaskbarStatePolicy.Parse(content));
    }
}
