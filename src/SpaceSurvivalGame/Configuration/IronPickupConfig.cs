namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable iron ore pickup values, loaded from a JSON file next to the executable so they can
/// be edited without recompiling. If the file is missing, a default one is written out so
/// there's always something to open and tweak.
/// </summary>
public class IronPickupConfig
{
    public int PickupCount { get; set; } = 5;
    public float IronAmount { get; set; } = 20f;
    public int SpriteSizePixels { get; set; } = 16;

    // Parsed via SpaceSurvivalGame.Rendering.ColorHex — "#RRGGBB" or "#RRGGBBAA".
    public string ColorHex { get; set; } = "#69768A";

    public float MaterialDensity { get; set; } = 0.3f;
    public float Restitution { get; set; } = 0.6f;
    public FloatRange SpeedMetersPerSecondRange { get; set; } = new(0.1f, 0.5f);

    // Initial spin, magnitude only — sign (direction) is randomized separately at spawn time.
    public FloatRange AngularVelocityRadiansPerSecondRange { get; set; } = new(0.1f, 1f);

    public static IronPickupConfig Load(string path) => ConfigLoader.Load<IronPickupConfig>(path);
}
