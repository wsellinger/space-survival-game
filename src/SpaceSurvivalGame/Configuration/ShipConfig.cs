using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

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

    // Thrust cuts out entirely once the ship's actual facing strays this many degrees from
    // the requested WASD/left-stick direction — has to turn back within the cone before it
    // fires again, rather than always burning out of whatever way it currently happens to face.
    public float ThrustAngleThresholdDegrees { get; set; } = 90f;

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
