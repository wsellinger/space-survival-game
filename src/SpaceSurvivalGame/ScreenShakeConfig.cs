using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame;

/// <summary>
/// Tunable screen shake values, loaded from a JSON file next to the executable so they
/// can be edited without recompiling. If the file is missing, a default one is written
/// out so there's always something to open and tweak.
/// </summary>
public class ScreenShakeConfig
{
    // Shake magnitude is a linear map from collision damage to pixels of jitter,
    // clamped between these two — a trivial tap barely shakes, a max-damage hit shakes hard.
    public float MinShakeMagnitudePixels { get; set; } = 2f;
    public float MaxShakeMagnitudePixels { get; set; } = 20f;

    // Exponential decay rate — higher settles back to still faster, same convention as
    // CameraConfig.TweenSpeed.
    public float ShakeDecaySpeed { get; set; } = 6f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static ScreenShakeConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<ScreenShakeConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new ScreenShakeConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
