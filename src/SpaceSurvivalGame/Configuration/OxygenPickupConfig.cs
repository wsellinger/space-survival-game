namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable O2 pickup values, loaded from a JSON file next to the executable so they can be
/// edited without recompiling. If the file is missing, a default one is written out so
/// there's always something to open and tweak.
/// </summary>
public class OxygenPickupConfig
{
    public int PickupCount { get; set; } = 5;
    public float OxygenAmount { get; set; } = 20f;
    public int SpriteSizePixels { get; set; } = 16;

    // Parsed via SpaceSurvivalGame.Rendering.ColorHex — "#RRGGBB" or "#RRGGBBAA".
    public string ColorHex { get; set; } = "#6495ED"; // CornflowerBlue's own hex value

    // How far the glow halo extends beyond the crystal's own edge, as a multiple of the
    // crystal's size — 1 = no glow, 1.5 = reaches half again as far out. Purely cosmetic:
    // never affects the crystal's actual on-screen size or its physics/collision shape.
    public float GlowRadius { get; set; } = 1.5f;

    public float MaterialDensity { get; set; } = 0.3f;
    public float Restitution { get; set; } = 0.6f;
    public FloatRange SpeedMetersPerSecondRange { get; set; } = new(0.1f, 0.5f);

    // Initial spin, magnitude only — sign (direction) is randomized separately at spawn time.
    public FloatRange AngularVelocityRadiansPerSecondRange { get; set; } = new(0.1f, 1f);

    public static OxygenPickupConfig Load(string path) => ConfigLoader.Load<OxygenPickupConfig>(path);
}
