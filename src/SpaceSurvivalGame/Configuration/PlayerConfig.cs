using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Collision damage is a linear map from approach speed to HP lost, clamped at both ends:
/// speeds at/below SpeedMetersPerSecondRange.Min deal DamageRange.Min, speeds at/above
/// SpeedMetersPerSecondRange.Max deal DamageRange.Max, everything between is interpolated.
/// </summary>
public class CollisionDamageConfig
{
    public FloatRange DamageRange { get; set; } = new(1f, 100f);
    public FloatRange SpeedMetersPerSecondRange { get; set; } = new(0.1f, 4.5f);
}

/// <summary>
/// Tunable player vitals values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class PlayerConfig
{
    public float MaxHealth { get; set; } = 100f;
    public float MaxOxygen { get; set; } = 100f;
    public float OxygenDrainPerSecond { get; set; } = 1f;
    public CollisionDamageConfig Collision { get; set; } = new();

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static PlayerConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<PlayerConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new PlayerConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
