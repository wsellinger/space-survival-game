using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

public class StarfieldLayerConfig
{
    public float HalfExtentMeters { get; set; }
    public int StarCount { get; set; }

    // How much camera movement affects this layer: 1 = moves fully with the camera
    // (nearest), smaller values lag behind more and read as farther away.
    public float Parallax { get; set; }

    // 0-1 brightness multiplier on top of plain white — distant layers dimmer helps sell depth.
    public float Brightness { get; set; } = 1f;
}

/// <summary>
/// Tunable background starfield layers, loaded from a JSON file next to the
/// executable so they can be edited without recompiling. If the file is
/// missing, a default one (5 layers) is written out so there's always
/// something to open and tweak.
/// </summary>
public class StarfieldConfig
{
    public List<StarfieldLayerConfig> Layers { get; set; } = new()
    {
        new StarfieldLayerConfig { HalfExtentMeters = 20f, StarCount = 400, Parallax = 1f, Brightness = 1f },
        new StarfieldLayerConfig { HalfExtentMeters = 30f, StarCount = 350, Parallax = 0.7f, Brightness = 0.85f },
        new StarfieldLayerConfig { HalfExtentMeters = 40f, StarCount = 300, Parallax = 0.5f, Brightness = 0.7f },
        new StarfieldLayerConfig { HalfExtentMeters = 60f, StarCount = 250, Parallax = 0.3f, Brightness = 0.55f },
        new StarfieldLayerConfig { HalfExtentMeters = 80f, StarCount = 200, Parallax = 0.15f, Brightness = 0.35f }
    };

    // Each star randomly dims one or two color channels by an amount in this range, giving
    // it a slight red/yellow/blue cast instead of plain white. 0 = no tint at all.
    public float MinTintStrength { get; set; } = 0.05f;
    public float MaxTintStrength { get; set; } = 0.15f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static StarfieldConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<StarfieldConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new StarfieldConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
