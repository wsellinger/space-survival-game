using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable screen-edge warning values (the pulsing outline + vignette shown for low health/O2
/// and on taking damage), loaded from a JSON file next to the executable so they can be
/// edited without recompiling. If the file is missing, a default one is written out so
/// there's always something to open and tweak.
/// </summary>
public class ScreenWarningConfig
{
    public int OutlineThicknessPixels { get; set; } = 8;
    public int VignetteDepthPixels { get; set; } = 220;

    // Full pulse cycles per second for the ongoing low-health/low-O2 warning.
    public float PulseFrequencyHz { get; set; } = 1.5f;

    // How long each color shows before switching, when both health and O2 are low at once.
    public float AlternateSeconds { get; set; } = 0.6f;

    // Caps how opaque the outline/vignette get at full pulse, so they never fully obscure
    // the screen.
    public float MaxIntensity { get; set; } = 0.6f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static ScreenWarningConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<ScreenWarningConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new ScreenWarningConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
