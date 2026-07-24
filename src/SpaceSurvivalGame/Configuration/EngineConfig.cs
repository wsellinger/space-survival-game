using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>A silhouette (outer) + core (inner) layer pair, sharing one size/color shape.</summary>
public class FlameLayerConfig
{
    public FloatRange LengthPixelsRange { get; set; } = new(6f, 26f);
    public FloatRange WidthPixelsRange { get; set; } = new(4f, 14f);
    public string ColorHex { get; set; } = "#FFAA28CC";
}

/// <summary>Like FlameLayerConfig, but colorless — the strafe jets reuse MainJet's colors (see EngineJetRenderer).</summary>
public class StrafeFlameLayerConfig
{
    public FloatRange LengthPixelsRange { get; set; } = new(3f, 8f);
    public FloatRange WidthPixelsRange { get; set; } = new(1f, 4f);
}

/// <summary>
/// A sum of two out-of-sync sine waves (not random) so the flame stays reproducible
/// frame-to-frame; Intensity is the max fraction the size/brightness varies by (0.15 = up to
/// +/-15%).
/// </summary>
public class FlickerConfig
{
    public float SpeedHz { get; set; } = 14f;
    public float Intensity { get; set; } = 0.15f;
}

public class MainJetConfig
{
    public FlameLayerConfig Outer { get; set; } = new();

    public FlameLayerConfig Inner { get; set; } = new()
    {
        LengthPixelsRange = new FloatRange(3f, 14f),
        WidthPixelsRange = new FloatRange(2f, 7f),
        ColorHex = "#FFFFDCCC"
    };
}

/// <summary>
/// 2 smaller jets flush-mounted on the ship's own side edges (nose to wing corner), firing
/// tangent to that edge but mirrored to point forward-and-outward rather than backward-and-inward
/// (like an RCS thruster venting away from the hull) — see EngineJetRenderer. EdgeMountFraction
/// picks where along that edge they sit (0 = at the wing corner, 1 = at the nose).
/// </summary>
public class StrafeJetsConfig
{
    public float EdgeMountFraction { get; set; } = 0.55f;
    public StrafeFlameLayerConfig Outer { get; set; } = new();

    public StrafeFlameLayerConfig Inner { get; set; } = new()
    {
        LengthPixelsRange = new FloatRange(1.5f, 4f),
        WidthPixelsRange = new FloatRange(1f, 2.5f)
    };
}

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

    public FlickerConfig Flicker { get; set; } = new();
    public MainJetConfig MainJet { get; set; } = new();
    public StrafeJetsConfig StrafeJets { get; set; } = new();

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
