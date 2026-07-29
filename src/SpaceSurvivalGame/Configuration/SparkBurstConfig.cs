namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Shared spark-burst cluster (see ParticleEffects.SpawnSparkBurst/SpawnPickupBurst/
/// SpawnExplosionBurst) — used by both ordinary collision taps (SparkConfig) and the bigger
/// death-explosion burst (DeathSequenceConfig.ExplosionConfig), independently tunable via each
/// one's own Burst section.
/// </summary>
public class SparkBurstConfig
{
    public int SizePixels { get; set; } = 6;
    public IntRange CountRange { get; set; } = new(8, 14);
    public FloatRange SpeedMetersPerSecondRange { get; set; } = new(1.5f, 4f);
    public FloatRange LifetimeSecondsRange { get; set; } = new(0.2f, 0.45f);
}
