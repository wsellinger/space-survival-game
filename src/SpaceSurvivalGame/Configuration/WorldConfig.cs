using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

public class AsteroidConfig
{
    // How many asteroids per square meter to generate, not a raw count — so shrinking
    // WorldConfig.FieldHalfExtentMeters (e.g. to cut down on total asteroid/physics load) keeps
    // the same spawn density instead of requiring a raw count to be manually recalculated.
    public float SpawnDensityPerSquareMeter { get; set; } = 0.0012f;

    public FloatRange RadiusMetersRange { get; set; } = new(0.5f, 2f);
    public FloatRange SpeedMetersPerSecondRange { get; set; } = new(0.1f, 0.5f);

    // Initial spin, magnitude only — sign (direction) is randomized separately at spawn time.
    public FloatRange AngularVelocityRadiansPerSecondRange { get; set; } = new(0.1f, 1f);

    public float Restitution { get; set; } = 0.6f;

    // Same material density for every asteroid, so mass just scales with area (bigger =
    // proportionally heavier) — not tied to the ship at runtime. This value is a fixed,
    // pragmatic starting point: at the default RadiusMetersRange.Min (0.5), it happens to put
    // the smallest asteroid's mass in the same ballpark as the ship's mass at its default
    // SpriteSize. (Not to be confused with SpawnDensityPerSquareMeter above, which is about
    // how many asteroids to generate, not any single asteroid's mass.)
    public float MaterialDensity { get; set; } = 0.073f;
}

/// <summary>
/// Tunable asteroid field values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class WorldConfig
{
    public float FieldHalfExtentMeters { get; set; } = 250f;

    // Asteroids never spawn within this many pixels of the ship's spawn point, so the ship
    // doesn't start out already touching (or embedded in) one.
    public float ShipSpawnClearRadiusPixels { get; set; } = 50f;

    public int WorldSeed { get; set; } = 12345;
    public AsteroidConfig Asteroid { get; set; } = new();

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
