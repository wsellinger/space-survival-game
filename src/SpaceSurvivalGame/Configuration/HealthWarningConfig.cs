using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable health-bar low-health warning values, loaded from a JSON file next to the
/// executable so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class HealthWarningConfig
{
    // Below this fraction of MaxHealth (and above 0), the bar blinks in a repeating
    // flash-off-flash-off-off-off-off pattern (7 equal beats, lit on beats 0 and 2), same as
    // OxygenWarningConfig's low-oxygen warning. Cadence is HudConfig.WarningFlashBeatSeconds,
    // shared with the O2 bar so both blink in sync.
    public float LowHealthThresholdFraction { get; set; } = 0.3f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static HealthWarningConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<HealthWarningConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new HealthWarningConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
