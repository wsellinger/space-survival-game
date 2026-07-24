using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable "STRAFE MODE" on-screen indicator values (corner brackets + text, shown while
/// ShipMovement.IsStrafing is set), loaded from a JSON file next to the executable so they
/// can be edited without recompiling. If the file is missing, a default one is written out
/// so there's always something to open and tweak.
/// </summary>
public class StrafeModeIndicatorConfig
{
    public float CornerRadiusPixels { get; set; } = 12f;
    public float OutlineThicknessPixels { get; set; } = 4f;
    public float ArmLengthPixels { get; set; } = 120f;

    // How far the brackets are pulled in from the true screen edge — also how far the text is
    // offset from the corner on top of TextMarginPixels, so it stays anchored to the (now inset)
    // bracket rather than the raw screen corner.
    public float InsetPixels { get; set; } = 24f;

    public string ColorHex { get; set; } = "#FFFFFF";

    public float TextScale { get; set; } = 2f;
    public float TextMarginPixels { get; set; } = 28f;

    // On/off toggles per second (a hard blink, not a fade).
    public float BlinkFrequencyHz { get; set; } = 1f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static StrafeModeIndicatorConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<StrafeModeIndicatorConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new StrafeModeIndicatorConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
