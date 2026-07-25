namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable collision/pickup spark burst values (shared by ParticleEffects.SpawnSparkBurst and
/// SpawnPickupBurst), loaded from a JSON file next to the executable so they can be edited
/// without recompiling. If the file is missing, a default one is written out so there's always
/// something to open and tweak.
/// </summary>
public class SparkConfig
{
    public int SparkTextureSizePixels { get; set; } = 6;
    public IntRange SparkCountRange { get; set; } = new(8, 14);
    public FloatRange SparkSpeedMetersPerSecondRange { get; set; } = new(1.5f, 4f);
    public FloatRange SparkLifetimeSecondsRange { get; set; } = new(0.2f, 0.45f);

    public static SparkConfig Load(string path) => ConfigLoader.Load<SparkConfig>(path);
}
