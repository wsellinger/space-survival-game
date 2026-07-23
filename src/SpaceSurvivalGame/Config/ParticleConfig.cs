using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Config;

/// <summary>
/// Tunable collision spark burst values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default one is
/// written out so there's always something to open and tweak.
/// </summary>
public class ParticleConfig
{
    public int SparkTextureSizePixels { get; set; } = 6;
    public int SparkCountMin { get; set; } = 8;
    public int SparkCountMax { get; set; } = 14;
    public float SparkSpeedMinMetersPerSecond { get; set; } = 1.5f;
    public float SparkSpeedMaxMetersPerSecond { get; set; } = 4f;
    public float SparkLifetimeMinSeconds { get; set; } = 0.2f;
    public float SparkLifetimeMaxSeconds { get; set; } = 0.45f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static ParticleConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<ParticleConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new ParticleConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
