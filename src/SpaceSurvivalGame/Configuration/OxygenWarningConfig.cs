using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable O2-bar low-oxygen warning values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default one is
/// written out so there's always something to open and tweak.
/// </summary>
public class OxygenWarningConfig
{
    // Below this fraction of MaxOxygen (and above 0), the bar blinks in a repeating
    // flash-off-flash-off-off-off-off pattern (7 equal beats, lit on beats 0 and 2). Cadence
    // is HudConfig.WarningFlashBeatSeconds, shared with the health bar so both blink in sync.
    public float LowOxygenThresholdFraction { get; set; } = 0.1f;

    // Once Oxygen hits exactly 0, the bar instead pulses smoothly toward red and grows/
    // shrinks, this many full cycles per second.
    public float EmptyOxygenPulseFrequencyHz { get; set; } = 2f;

    // How much larger the bar gets at the peak of the empty-oxygen pulse, as a fraction of
    // its normal size (0.15 = up to 15% bigger).
    public float EmptyOxygenPulseScaleAmount { get; set; } = 0.15f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static OxygenWarningConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<OxygenWarningConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new OxygenWarningConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
