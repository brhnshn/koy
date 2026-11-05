using Koy.Core.Display;
using Xunit;

namespace Koy.Core.Tests;

public class DisplayMonitorNumberTests
{
    [Theory]
    [InlineData(@"\\.\DISPLAY1", 1)]
    [InlineData(@"\\.\DISPLAY12", 12)]
    [InlineData("", 0)]
    [InlineData("kimlik", 0)]
    public void Number_is_the_trailing_digits_of_the_device_name(string deviceName, int expected)
    {
        var monitor = new DisplayMonitor("id", "ad", default, default, false, DeviceName: deviceName);
        Assert.Equal(expected, monitor.Number);
    }
}
