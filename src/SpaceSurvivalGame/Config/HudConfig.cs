using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Config;

/// <summary>
/// Tunable HUD bar layout values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class HudConfig
{
    public int BarLengthPixels { get; set; } = 200;
    public int BarThicknessPixels { get; set; } = 20;
    public int BarOutlineThicknessPixels { get; set; } = 2;
    public int MarginPixels { get; set; } = 16;
    public int BarSpacingPixels { get; set; } = 6;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static HudConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<HudConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new HudConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
