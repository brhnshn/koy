using Koy.Core.Timers;

namespace Koy.Core.Tests;

public class CountdownTimerTests
{
    private static CountdownTimer Create(FakeClock clock, int seconds = 60) => new(TimeSpan.FromSeconds(seconds), clock);

    [Fact]
    public void New_timer_is_idle_with_the_full_duration()
    {
        var clock = new FakeClock();
        using var timer = Create(clock, 90);

        Assert.Equal(CountdownState.Idle, timer.State);
        Assert.Equal(TimeSpan.FromSeconds(90), timer.Remaining);
        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public void Running_timer_counts_down_from_the_clock_without_ticking()
    {
        var clock = new FakeClock();
        using var timer = Create(clock);

        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(20));

        Assert.Equal(CountdownState.Running, timer.State);
        Assert.Equal(TimeSpan.FromSeconds(40), timer.Remaining);
        // Saniyelik tik yok: çalışırken tek bir (bitiş için) zamanlayıcı var.
        Assert.Equal(1, clock.ActiveTimers);
    }

    [Fact]
    public void Timer_finishes_once_at_the_end_and_raises_the_event()
    {
        var clock = new FakeClock();
        using var timer = Create(clock, 30);
        var finished = 0;
        timer.Finished += (_, _) => finished++;

        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(29));
        Assert.Equal(0, finished);

        clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(1, finished);
        Assert.Equal(CountdownState.Finished, timer.State);
        Assert.Equal(TimeSpan.Zero, timer.Remaining);
        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public void Pause_freezes_the_remaining_time_and_stops_the_background_timer()
    {
        var clock = new FakeClock();
        using var timer = Create(clock);
        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(15));

        timer.Pause();
        clock.Advance(TimeSpan.FromMinutes(10));

        Assert.Equal(CountdownState.Paused, timer.State);
        Assert.Equal(TimeSpan.FromSeconds(45), timer.Remaining);
        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public void Resume_after_pause_finishes_after_the_remaining_time()
    {
        var clock = new FakeClock();
        using var timer = Create(clock, 60);
        var finished = 0;
        timer.Finished += (_, _) => finished++;

        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(50));
        timer.Pause();
        clock.Advance(TimeSpan.FromHours(1));
        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(9));
        Assert.Equal(0, finished);
        clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(1, finished);
        Assert.Equal(CountdownState.Finished, timer.State);
    }

    [Fact]
    public void Reset_returns_to_the_full_duration()
    {
        var clock = new FakeClock();
        using var timer = Create(clock, 60);
        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(10));

        timer.Reset();

        Assert.Equal(CountdownState.Idle, timer.State);
        Assert.Equal(TimeSpan.FromSeconds(60), timer.Remaining);
        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public void Reset_while_running_prevents_a_late_finish()
    {
        var clock = new FakeClock();
        using var timer = Create(clock, 10);
        var finished = 0;
        timer.Finished += (_, _) => finished++;
        timer.Start();

        timer.Reset();
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(0, finished);
    }

    [Fact]
    public void Start_after_finish_restarts_from_the_full_duration()
    {
        var clock = new FakeClock();
        using var timer = Create(clock, 5);
        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(6));

        timer.Start();

        Assert.Equal(CountdownState.Running, timer.State);
        Assert.Equal(TimeSpan.FromSeconds(5), timer.Remaining);
    }

    [Fact]
    public void Duration_can_only_change_while_not_running()
    {
        var clock = new FakeClock();
        using var timer = Create(clock, 60);

        Assert.True(timer.SetDuration(TimeSpan.FromMinutes(5)));
        Assert.Equal(TimeSpan.FromMinutes(5), timer.Remaining);

        timer.Start();
        Assert.False(timer.SetDuration(TimeSpan.FromMinutes(1)));
        Assert.Equal(TimeSpan.FromMinutes(5), timer.Duration);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-30, 1)]
    [InlineData(10, 10)]
    public void Duration_is_clamped_to_the_allowed_range(int seconds, int expectedSeconds)
    {
        var clock = new FakeClock();
        using var timer = new CountdownTimer(TimeSpan.FromSeconds(seconds), clock);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), timer.Duration);
    }

    [Fact]
    public void Duration_has_an_upper_limit()
    {
        var clock = new FakeClock();
        using var timer = new CountdownTimer(TimeSpan.FromDays(30), clock);

        Assert.Equal(CountdownTimer.MaxDuration, timer.Duration);
    }

    [Fact]
    public void Dispose_cancels_the_background_timer()
    {
        var clock = new FakeClock();
        var timer = Create(clock, 10);
        var finished = 0;
        timer.Finished += (_, _) => finished++;
        timer.Start();

        timer.Dispose();
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(0, finished);
        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public void State_changes_are_reported()
    {
        var clock = new FakeClock();
        using var timer = Create(clock, 3);
        var states = new List<CountdownState>();
        timer.StateChanged += (_, s) => states.Add(s);

        timer.Start();
        timer.Pause();
        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(4));
        timer.Reset();

        Assert.Equal(
            [CountdownState.Running, CountdownState.Paused, CountdownState.Running, CountdownState.Finished, CountdownState.Idle],
            states);
    }
}

public class LapStopwatchTests
{
    [Fact]
    public void New_stopwatch_is_idle_at_zero()
    {
        var watch = new LapStopwatch(new FakeClock());

        Assert.Equal(StopwatchState.Idle, watch.State);
        Assert.Equal(TimeSpan.Zero, watch.Elapsed);
        Assert.Empty(watch.Laps);
    }

    [Fact]
    public void Elapsed_follows_the_clock_only_while_running()
    {
        var clock = new FakeClock();
        var watch = new LapStopwatch(clock);

        watch.Start();
        clock.Advance(TimeSpan.FromSeconds(12));
        watch.Pause();
        clock.Advance(TimeSpan.FromSeconds(100));

        Assert.Equal(TimeSpan.FromSeconds(12), watch.Elapsed);

        watch.Start();
        clock.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(TimeSpan.FromSeconds(15), watch.Elapsed);
    }

    [Fact]
    public void Stopwatch_never_creates_a_timer()
    {
        var clock = new FakeClock();
        var watch = new LapStopwatch(clock);

        watch.Start();
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public void Laps_record_total_and_split_newest_first()
    {
        var clock = new FakeClock();
        var watch = new LapStopwatch(clock);
        watch.Start();

        clock.Advance(TimeSpan.FromSeconds(10));
        watch.Lap();
        clock.Advance(TimeSpan.FromSeconds(7));
        watch.Lap();

        var laps = watch.Laps;
        Assert.Equal(2, laps.Count);
        Assert.Equal(2, laps[0].Number);
        Assert.Equal(TimeSpan.FromSeconds(17), laps[0].Total);
        Assert.Equal(TimeSpan.FromSeconds(7), laps[0].Split);
        Assert.Equal(1, laps[1].Number);
        Assert.Equal(TimeSpan.FromSeconds(10), laps[1].Split);
    }

    [Fact]
    public void Lap_is_ignored_unless_running()
    {
        var clock = new FakeClock();
        var watch = new LapStopwatch(clock);

        Assert.Null(watch.Lap());

        watch.Start();
        clock.Advance(TimeSpan.FromSeconds(2));
        watch.Pause();

        Assert.Null(watch.Lap());
        Assert.Empty(watch.Laps);
    }

    [Fact]
    public void Reset_clears_time_and_laps()
    {
        var clock = new FakeClock();
        var watch = new LapStopwatch(clock);
        watch.Start();
        clock.Advance(TimeSpan.FromSeconds(5));
        watch.Lap();

        watch.Reset();

        Assert.Equal(StopwatchState.Idle, watch.State);
        Assert.Equal(TimeSpan.Zero, watch.Elapsed);
        Assert.Empty(watch.Laps);

        watch.Start();
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, watch.Lap()!.Number);
    }

    [Fact]
    public void Lap_list_is_capped()
    {
        var clock = new FakeClock();
        var watch = new LapStopwatch(clock);
        watch.Start();

        for (var i = 0; i < LapStopwatch.MaxLaps + 5; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            watch.Lap();
        }

        Assert.Equal(LapStopwatch.MaxLaps, watch.Laps.Count);
        Assert.Equal(LapStopwatch.MaxLaps + 5, watch.Laps[0].Number);
    }
}

public class CountdownPresetsTests
{
    [Theory]
    [InlineData(5, true, 6)]
    [InlineData(9, true, 10)]
    [InlineData(10, true, 15)]
    [InlineData(12, true, 15)]
    [InlineData(59, true, 60)]
    [InlineData(60, true, 75)]
    [InlineData(1, false, 1)]
    [InlineData(7, false, 6)]
    [InlineData(10, false, 9)]
    [InlineData(12, false, 10)]
    [InlineData(15, false, 10)]
    [InlineData(60, false, 55)]
    [InlineData(70, false, 60)]
    [InlineData(75, false, 60)]
    public void Step_moves_by_a_size_that_fits_the_current_duration(int from, bool increase, int expected)
    {
        Assert.Equal(TimeSpan.FromMinutes(expected), CountdownPresets.Step(TimeSpan.FromMinutes(from), increase));
    }

    [Fact]
    public void Step_stays_inside_the_allowed_range()
    {
        Assert.Equal(CountdownTimer.MaxDuration, CountdownPresets.Step(CountdownTimer.MaxDuration, true));
        Assert.Equal(TimeSpan.FromMinutes(1), CountdownPresets.Step(TimeSpan.FromSeconds(10), false));
    }

    [Fact]
    public void Default_and_presets_are_valid_durations()
    {
        Assert.All(CountdownPresets.Minutes, m => Assert.InRange(TimeSpan.FromMinutes(m), CountdownTimer.MinDuration, CountdownTimer.MaxDuration));
        Assert.Contains(CountdownPresets.DefaultMinutes, CountdownPresets.Minutes);
    }
}

public class DurationFormatTests
{
    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(-5, "00:00")]
    [InlineData(1, "00:01")]
    [InlineData(300, "05:00")]
    [InlineData(3599, "59:59")]
    [InlineData(3600, "1:00:00")]
    [InlineData(3725, "1:02:05")]
    public void Countdown_formats_whole_seconds(int seconds, string expected)
    {
        Assert.Equal(expected, DurationFormat.Countdown(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Countdown_rounds_a_fraction_of_a_second_up()
    {
        Assert.Equal("00:01", DurationFormat.Countdown(TimeSpan.FromMilliseconds(400)));
        Assert.Equal("04:60".Length, DurationFormat.Countdown(TimeSpan.FromMilliseconds(299_100)).Length);
        Assert.Equal("05:00", DurationFormat.Countdown(TimeSpan.FromMilliseconds(299_100)));
    }

    [Theory]
    [InlineData(0, "00:00.0")]
    [InlineData(100, "00:00.1")]
    [InlineData(12_340, "00:12.3")]
    [InlineData(59_999, "00:59.9")]
    [InlineData(60_000, "01:00.0")]
    [InlineData(3_600_000, "1:00:00.0")]
    public void Stopwatch_formats_tenths_and_truncates(int milliseconds, string expected)
    {
        Assert.Equal(expected, DurationFormat.Stopwatch(TimeSpan.FromMilliseconds(milliseconds)));
    }
}
