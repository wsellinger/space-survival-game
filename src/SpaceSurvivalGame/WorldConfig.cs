using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame;

/// <summary>
/// Tunable asteroid field values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class WorldConfig
{
    public float FieldHalfExtentMeters { get; set; } = 250f;

    // How many asteroids per square meter to generate, not a raw count — so shrinking
    // FieldHalfExtentMeters (e.g. to cut down on total asteroid/physics load) keeps the
    // same spawn density instead of requiring AsteroidCount to be manually recalculated.
    public float AsteroidSpawnDensityPerSquareMeter { get; set; } = 0.0012f;

    public float AsteroidMinRadiusMeters { get; set; } = 0.5f;
    public float AsteroidMaxRadiusMeters { get; set; } = 2f;
    public float AsteroidMinSpeedMetersPerSecond { get; set; } = 0.1f;
    public float AsteroidMaxSpeedMetersPerSecond { get; set; } = 0.5f;
    public float AsteroidRestitution { get; set; } = 0.6f;

    // Same material density for every asteroid, so mass just scales with area (bigger =
    // proportionally heavier) — not tied to the ship at runtime. This value is a fixed,
    // pragmatic starting point: at the default AsteroidMinRadiusMeters (0.5), it happens to
    // put the smallest asteroid's mass in the same ballpark as the ship's mass at its
    // default SpriteSize. (Not to be confused with AsteroidSpawnDensityPerSquareMeter above,
    // which is about how many asteroids to generate, not any single asteroid's mass.)
    public float AsteroidMaterialDensity { get; set; } = 0.073f;
    public int WorldSeed { get; set; } = 12345;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static WorldConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<WorldConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new WorldConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
