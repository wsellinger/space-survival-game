using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable mouse-aim crosshair values, loaded from a JSON file next to the executable so
/// they can be edited without recompiling. If the file is missing, a default one is written
/// out so there's always something to open and tweak.
/// </summary>
public class CrosshairConfig
{
    public int SizePixels { get; set; } = 20;
    public float GapRadiusPixels { get; set; } = 4f;
    public float TickLengthPixels { get; set; } = 5f;
    public float ThicknessPixels { get; set; } = 2f;
    public int ColorR { get; set; } = 255;
    public int ColorG { get; set; } = 255;
    public int ColorB { get; set; } = 255;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static CrosshairConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<CrosshairConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new CrosshairConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
