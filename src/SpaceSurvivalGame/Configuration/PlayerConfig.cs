using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

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

    // Collision damage is a linear map from approach speed to HP lost, clamped
    // at both ends: speeds at/below MinCollisionSpeedMetersPerSecond deal
    // MinCollisionDamage, speeds at/above MaxCollisionSpeedMetersPerSecond deal
    // MaxCollisionDamage, everything between is interpolated.
    public float MinCollisionDamage { get; set; } = 1f;
    public float MaxCollisionDamage { get; set; } = 100f;
    public float MinCollisionSpeedMetersPerSecond { get; set; } = 0.1f;
    public float MaxCollisionSpeedMetersPerSecond { get; set; } = 4.5f;

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
