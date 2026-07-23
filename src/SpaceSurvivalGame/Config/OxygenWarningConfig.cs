using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Config;

/// <summary>
/// Tunable O2-bar low-oxygen warning values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default one is
/// written out so there's always something to open and tweak.
/// </summary>
public class OxygenWarningConfig
{
    // Below this fraction of MaxOxygen (and above 0), the bar flashes in a repeating
    // flash-flash-wait-wait-wait pattern (5 equal beats, first 2 lit).
    public float LowOxygenThresholdFraction { get; set; } = 0.1f;

    // Duration of one beat in the flash-flash-wait-wait-wait cycle.
    public float LowOxygenFlashBeatSeconds { get; set; } = 0.15f;

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
