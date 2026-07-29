namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Shared "small particle puff" cluster (see ParticleEffects.SpawnRotationJetPuff) — used by both
/// the ship's rotation-turn jets (RotationJetConfig) and the station core's drift-impulse puffs
/// (StationCoreDriftPuffConfig), which fire the exact same kind of effect from different mount
/// points.
/// </summary>
public class ParticlePuffConfig
{
    public IntRange ParticleCountPerFrame { get; set; } = new(1, 2);
    public FloatRange ParticleSpeedMetersPerSecondRange { get; set; } = new(0.5f, 1.2f);
    public FloatRange ParticleLifetimeSecondsRange { get; set; } = new(0.08f, 0.16f);
    public int ParticleSizePixels { get; set; } = 3;

    // Half-angle of random spread around each jet's own outward direction, in degrees.
    public float SpreadAngleDegrees { get; set; } = 20f;

    public string ColorHex { get; set; } = "#CFE8FFFF"; // pale blue-white, like a puff of gas
}
