namespace Koy.Core.Animation;

/// <summary>
/// Kütle-yay-sönüm modeli. Sabit alt adımlarla ilerlediği için kare hızından bağımsızdır.
/// Varsayılan değerler hafif bir sekme verir (kritik sönümün biraz altı).
/// </summary>
public sealed class Spring
{
    private const double MaxStep = 1.0 / 240;

    public Spring(double initial)
    {
        Value = initial;
        Target = initial;
    }

    public double Stiffness { get; init; } = 300;
    public double Damping { get; init; } = 26;
    public double Mass { get; init; } = 1;
    public double RestThreshold { get; init; } = 0.01;

    public double Value { get; private set; }
    public double Velocity { get; private set; }
    public double Target { get; set; }

    public bool IsAtRest =>
        Math.Abs(Target - Value) < RestThreshold && Math.Abs(Velocity) < RestThreshold;

    /// <summary>Durumu <paramref name="dt"/> saniye ilerletir. Hareket sürüyorsa true döner.</summary>
    public bool Step(double dt)
    {
        while (dt > 0)
        {
            var h = Math.Min(dt, MaxStep);
            var force = -Stiffness * (Value - Target) - Damping * Velocity;
            Velocity += force / Mass * h;
            Value += Velocity * h;
            dt -= h;
        }

        if (!IsAtRest)
            return true;

        Value = Target;
        Velocity = 0;
        return false;
    }

    /// <summary>Animasyonsuz olarak değere atlar.</summary>
    public void SnapTo(double value)
    {
        Value = value;
        Target = value;
        Velocity = 0;
    }
}
