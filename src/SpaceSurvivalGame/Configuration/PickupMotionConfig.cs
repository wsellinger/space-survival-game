namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Physics/motion knobs shared identically by every pickup type (O2 crystals, iron ore chunks) —
/// how they're spawned as small drifting Box2D bodies. Plain composition rather than a common
/// base class since there's no per-type variation needed (unlike RichAsteroidConfig, whose
/// subclasses exist because some values genuinely differ per resource).
/// </summary>
public class PickupMotionConfig
{
    public float MaterialDensity { get; set; } = 0.3f;
    public float Restitution { get; set; } = 0.6f;
    public FloatRange SpeedMetersPerSecondRange { get; set; } = new(0.1f, 0.5f);

    // Initial spin, magnitude only — sign (direction) is randomized separately at spawn time.
    public FloatRange AngularVelocityRadiansPerSecondRange { get; set; } = new(0.1f, 1f);
}
