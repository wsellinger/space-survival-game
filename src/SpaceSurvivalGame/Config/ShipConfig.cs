using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Config;

/// <summary>
/// Tunable ship movement values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class ShipConfig
{
    public float ThrustAcceleration { get; set; } = 8f;
    public float MaxSpeedMetersPerSecond { get; set; } = 4f;
    public float TurnSpeedRadiansPerSecond { get; set; } = 8f;
    public int SpriteSize { get; set; } = 24;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static ShipConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<ShipConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new ShipConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
