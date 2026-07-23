using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Config;

/// <summary>
/// Tunable death-sequence values (explosion, then fade to black, then the game-over screen),
/// loaded from a JSON file next to the executable so they can be edited without recompiling.
/// If the file is missing, a default one is written out so there's always something to open
/// and tweak.
/// </summary>
public class DeathSequenceConfig
{
    // How many times ParticleEffects.SpawnSparkBurst fires at the ship's position (each burst
    // is ParticleConfig.SparkCountMin-Max particles) — repurposes the collision-spark effect
    // into a bigger one-off explosion.
    public int ExplosionBurstCount { get; set; } = 5;

    // How long the explosion plays before the screen starts fading to black.
    public float ExplosionDurationSeconds { get; set; } = 1.5f;

    // How long the fade to black takes once it starts.
    public float FadeDurationSeconds { get; set; } = 1.5f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static DeathSequenceConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<DeathSequenceConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new DeathSequenceConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
