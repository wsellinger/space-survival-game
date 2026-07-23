using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Config;

/// <summary>
/// Tunable health-bar flash/shake values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default one is
/// written out so there's always something to open and tweak.
/// </summary>
public class HudFeedbackConfig
{
    public float FlashDurationSeconds { get; set; } = 0.25f;

    // Shake magnitude is a linear map from collision damage to pixels of jitter on the
    // health bar itself, clamped between these two, same convention as ScreenShakeConfig.
    public float MinShakeMagnitudePixels { get; set; } = 1f;
    public float MaxShakeMagnitudePixels { get; set; } = 6f;
    public float ShakeDecaySpeed { get; set; } = 10f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static HudFeedbackConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<HudFeedbackConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new HudFeedbackConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
