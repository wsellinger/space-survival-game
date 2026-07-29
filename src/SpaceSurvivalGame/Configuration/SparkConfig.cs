namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable collision/pickup spark burst values (shared by ParticleEffects.SpawnSparkBurst and
/// SpawnPickupBurst), loaded from a JSON file next to the executable so they can be edited
/// without recompiling. If the file is missing, a default one is written out so there's always
/// something to open and tweak.
/// </summary>
public class SparkConfig
{
    public SparkBurstConfig Burst { get; set; } = new();

    public static SparkConfig Load(string path) => ConfigLoader.Load<SparkConfig>(path);
}
