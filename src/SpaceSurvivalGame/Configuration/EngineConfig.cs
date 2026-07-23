using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable engine exhaust flame values, loaded from a JSON file next to the executable so
/// they can be edited without recompiling. If the file is missing, a default one is written
/// out so there's always something to open and tweak.
/// </summary>
public class EngineConfig
{
    // How fast keyboard's on/off throttle eases toward 0 or 1 (units/second) — controller
    // input instead maps directly to how far the left stick is pushed, no easing needed.
    public float KeyboardThrottleEaseSpeed { get; set; } = 4f;

    public int FlameTextureSizePixels { get; set; } = 16;

    // Outer flame — the full silhouette.
    public float MinFlameLengthPixels { get; set; } = 6f;
    public float MaxFlameLengthPixels { get; set; } = 26f;
    public float MinFlameWidthPixels { get; set; } = 4f;
    public float MaxFlameWidthPixels { get; set; } = 14f;
    public int ColorR { get; set; } = 255;
    public int ColorG { get; set; } = 170;
    public int ColorB { get; set; } = 40;

    // Inner flame — a smaller, independently colored core drawn on top of the outer one
    // (both anchored/rotated the same way).
    public float MinInnerFlameLengthPixels { get; set; } = 3f;
    public float MaxInnerFlameLengthPixels { get; set; } = 14f;
    public float MinInnerFlameWidthPixels { get; set; } = 2f;
    public float MaxInnerFlameWidthPixels { get; set; } = 7f;
    public int InnerColorR { get; set; } = 255;
    public int InnerColorG { get; set; } = 255;
    public int InnerColorB { get; set; } = 220;

    // Flicker is a sum of two out-of-sync sine waves (not random) so the flame stays
    // reproducible frame-to-frame; FlickerIntensity is the max fraction the size/brightness
    // varies by (0.15 = up to +/-15%).
    public float FlickerSpeedHz { get; set; } = 14f;
    public float FlickerIntensity { get; set; } = 0.15f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static EngineConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<EngineConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new EngineConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
