using Koy.Core.Notch;

namespace Koy.Core.Tests;

public sealed class GreetingTests
{
    [Theory]
    [InlineData(5, DayPart.Morning)]
    [InlineData(11, DayPart.Morning)]
    [InlineData(12, DayPart.Afternoon)]
    [InlineData(17, DayPart.Afternoon)]
    [InlineData(18, DayPart.Evening)]
    [InlineData(21, DayPart.Evening)]
    [InlineData(22, DayPart.Night)]
    [InlineData(0, DayPart.Night)]
    [InlineData(4, DayPart.Night)]
    public void ForHour_PicksTheDayPart(int hour, DayPart expected) =>
        Assert.Equal(expected, Greeting.ForHour(hour));
}
