using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable collision spark burst values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default one is
/// written out so there's always something to open and tweak.
/// </summary>
public class ParticleConfig
{
    public int SparkTextureSizePixels { get; set; } = 6;
    public IntRange SparkCountRange { get; set; } = new(8, 14);
    public FloatRange SparkSpeedMetersPerSecondRange { get; set; } = new(1.5f, 4f);
    public FloatRange SparkLifetimeSecondsRange { get; set; } = new(0.2f, 0.45f);

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
