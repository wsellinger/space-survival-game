namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable iron ore pickup values, loaded from a JSON file next to the executable so they can
/// be edited without recompiling. If the file is missing, a default one is written out so
/// there's always something to open and tweak.
/// </summary>
public class IronPickupConfig
{
    public int PickupCount { get; set; } = 5;
    public float IronAmount { get; set; } = 20f;
    public int SpriteSizePixels { get; set; } = 16;

    // Parsed via SpaceSurvivalGame.Rendering.ColorHex — "#RRGGBB" or "#RRGGBBAA".
    public string ColorHex { get; set; } = "#69768A";

    // Color of the "+N Iron" popup (see ParticleEffects.SpawnFloatingText/IronPickupSystem) —
    // independent of ColorHex above, since the ore's own sprite color can be too dark/subtle to
    // read well as legible on-screen text.
    public string FloatingTextColorHex { get; set; } = "#69768A";

    public IronSparkleConfig Sparkle { get; set; } = new();
    public IronRustSpotConfig RustSpots { get; set; } = new();
    public PickupMotionConfig Motion { get; set; } = new();

    public static IronPickupConfig Load(string path) => ConfigLoader.Load<IronPickupConfig>(path);
}

/// <summary>Iron doesn't glow like the O2 crystals, so instead each chunk carries a small animated glint (see MetallicSparkle/MetallicSparkleRenderSystem) that briefly flares in and out as if catching the light, rather than a static highlight.</summary>
public class IronSparkleConfig
{
    public string ColorHex { get; set; } = "#FFFFFF";
    public int Count { get; set; } = 6; // how many independent glint points each chunk carries
    public int SizePixels { get; set; } = 3;
    public float FrequencyHz { get; set; } = 0.5f; // flare cycles per second, before the per-instance phase offset
    public float Sharpness { get; set; } = 8f; // higher = briefer, punchier flares (raises the sine hump to this power)
    public float MaxAlpha { get; set; } = 0.85f; // opacity at the very peak of a flare
}

/// <summary>
/// Small jagged rust patches baked into each shape variant's own texture (see
/// IronPickupField.Create/ProceduralTextures.CreateSpottedPolygon) — static, unlike the sparkle,
/// since they're a fixed surface stain rather than something catching the light. ColorHex's own
/// alpha is the blend strength against the base ore color, not baked-in transparency, so keep it
/// well under FF for a subtle stain rather than a solid patch.
/// </summary>
public class IronRustSpotConfig
{
    public string ColorHex { get; set; } = "#8B451399";
    public IntRange CountRange { get; set; } = new(2, 4);
    public FloatRange SizeUnitRange { get; set; } = new(0.15f, 0.35f); // fraction of the chunk's own local unit radius
}
