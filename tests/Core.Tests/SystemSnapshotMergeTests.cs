using Koy.Core.SystemStats;
using Xunit;

namespace Koy.Core.Tests;

public class SystemSnapshotMergeTests
{
    private static SystemSnapshot Full(double cpu = 23) =>
        new(cpu, "Chrome", 11, 9L << 30, 16L << 30, 58, 2_000_000, 300_000, "Ev", 64L << 20);

    // Servis yeni kurulmuş: yalnız anlık değerler.
    private static SystemSnapshot Instant(long usedBytes = 10L << 30) =>
        new(0, null, 0, usedBytes, 16L << 30, 62.5, 0, 0, "Ev", 66L << 20, RatesReady: false);

    [Fact]
    public void Full_sample_is_returned_as_is()
    {
        var fresh = Full(40);

        Assert.Same(fresh, SystemSnapshotMerge.Merge(fresh, Full(10)));
    }

    [Fact]
    public void Instant_sample_takes_fresh_ram_but_keeps_last_cpu_and_rates()
    {
        var merged = SystemSnapshotMerge.Merge(Instant(), Full(23));

        Assert.Equal(10L << 30, merged.RamUsedBytes);
        Assert.Equal(62.5, merged.RamPercent);
        Assert.Equal(66L << 20, merged.KoyMemoryBytes);
        Assert.Equal(23, merged.CpuPercent);
        Assert.Equal("Chrome", merged.TopProcessName);
        Assert.Equal(2_000_000, merged.NetDownBytesPerSec);
        Assert.True(merged.RatesReady);
    }

    [Fact]
    public void Instant_sample_without_history_stays_unready()
    {
        var fresh = Instant();

        var merged = SystemSnapshotMerge.Merge(fresh, null);

        Assert.False(merged.RatesReady);
        Assert.Same(fresh, merged);
    }

    [Fact]
    public void Instant_sample_does_not_borrow_from_unready_history()
    {
        var merged = SystemSnapshotMerge.Merge(Instant(), SystemSnapshot.Empty);

        Assert.False(merged.RatesReady);
        Assert.Equal(0, merged.CpuPercent);
    }

    [Fact]
    public void Empty_has_no_rates_and_no_instant_data()
    {
        Assert.False(SystemSnapshot.Empty.RatesReady);
        Assert.False(SystemSnapshotMerge.HasInstantData(SystemSnapshot.Empty));
        Assert.True(SystemSnapshotMerge.HasInstantData(Instant()));
    }
}
