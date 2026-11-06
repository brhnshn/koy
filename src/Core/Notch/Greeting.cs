namespace Koy.Core.Notch;

/// <summary>Günün hangi diliminde olduğumuz; Ana'daki selamlama (müzik çalmıyorken) için.</summary>
public enum DayPart
{
    Morning,
    Afternoon,
    Evening,
    Night
}

public static class Greeting
{
    /// <summary>05-11 sabah, 12-17 gün, 18-21 akşam, 22-04 gece.</summary>
    public static DayPart ForHour(int hour) => hour switch
    {
        >= 5 and < 12 => DayPart.Morning,
        >= 12 and < 18 => DayPart.Afternoon,
        >= 18 and < 22 => DayPart.Evening,
        _ => DayPart.Night
    };
}
