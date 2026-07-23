using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame;

/// <summary>
/// Tunable O2 pickup values, loaded from a JSON file next to the executable so they can be
/// edited without recompiling. If the file is missing, a default one is written out so
/// there's always something to open and tweak.
/// </summary>
public class PickupConfig
{
    public int PickupCount { get; set; } = 5;
    public float OxygenAmount { get; set; } = 20f;
    public int SpriteSizePixels { get; set; } = 16;

    // How far the glow halo extends beyond the crystal's own edge, as a multiple of the
    // crystal's size — 1 = no glow, 1.5 = reaches half again as far out. Purely cosmetic:
    // never affects the crystal's actual on-screen size or its physics/collision shape.
    public float GlowRadius { get; set; } = 1.5f;

    public float MaterialDensity { get; set; } = 0.3f;
    public float Restitution { get; set; } = 0.6f;
    public float MinSpeedMetersPerSecond { get; set; } = 0.1f;
    public float MaxSpeedMetersPerSecond { get; set; } = 0.5f;

    // Initial spin, magnitude only — sign (direction) is randomized separately at spawn time.
    public float MinAngularVelocityRadiansPerSecond { get; set; } = 0.1f;
    public float MaxAngularVelocityRadiansPerSecond { get; set; } = 1f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static PickupConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<PickupConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new PickupConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
