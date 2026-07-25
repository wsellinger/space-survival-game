namespace SpaceSurvivalGame.Configuration;

public class OxygenRichAsteroidConfig
{
    // Fraction of generated asteroids that roll this type instead of an ordinary rock.
    public float SpawnChanceFraction { get; set; } = 0.15f;

    public IntRange CrystalCountRange { get; set; } = new(3, 6);

    // Each speckle's own radius, in true rock-unit space (1.0 = the rock's own nominal outer
    // radius — the same unit space the rock's own unit vertices live in).
    public FloatRange CrystalSizeUnitRange { get; set; } = new(0.10f, 0.18f);

    // How far each speckle's own center sits from the rock's ACTUAL edge in the direction that
    // speckle rolled (not a plain circle — the rock's real boundary varies with angle, dipping
    // well below its own nominal radius of 1 at concave points between vertices), in true
    // rock-unit space: negative = pulled inward (embedded), positive = pushed outward
    // (protruding). The max end must stay below CrystalSizeUnitRange.Min so even the smallest
    // crystal at the largest offset still overlaps the rock's edge by a hair — otherwise it'd
    // render as a fully detached blob floating just past the silhouette.
    public FloatRange CrystalEdgeOffsetRange { get; set; } = new(-0.20f, 0.08f);

    // How far each speckle's glow extends beyond its own edge, as a multiple of that speckle's
    // own radius — same convention as PickupConfig.GlowRadius.
    public float CrystalGlowRadiusMultiplier { get; set; } = 1.4f;
}

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

    public OxygenRichAsteroidConfig OxygenRich { get; set; } = new();
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

    public static WorldConfig Load(string path) => ConfigLoader.Load<WorldConfig>(path);
}
