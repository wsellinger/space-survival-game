using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame;

/// <summary>
/// Tunable suffocation post-process values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default one is
/// written out so there's always something to open and tweak.
/// </summary>
public class SuffocationEffectConfig
{
    // How long after Oxygen hits 0 before the effect reaches full intensity (grayscale
    // fully desaturated, pixelation at its max block size, vignette fully closed to black).
    public float EffectDurationSeconds { get; set; } = 5f;

    // Pixelation block size in screen pixels at full intensity; 0 at the start. Left in but
    // disabled by default in favor of the noise effect below — flip PixelationEnabled back
    // on to bring it back.
    public bool PixelationEnabled { get; set; } = false;
    public float MaxPixelationBlockSizePixels { get; set; } = 24f;

    // Size of one noise cell in screen pixels — 1 is per-pixel-sharp static; higher values
    // give chunkier grain.
    public float NoiseGrainSizePixels { get; set; } = 1f;

    // Noise blend strength at full effect intensity; 0 disables it entirely.
    public float NoiseMaxIntensity { get; set; } = 0.35f;

    // true = additive: pixels blend toward random gray/white static (classic TV-static look).
    // false = darkening grain: pixels are randomly and only ever darkened (film-grain look).
    public bool NoiseAdditiveBlend { get; set; } = true;

    // Reshapes how grayscale intensity ramps over the effect's progress (0..1) via
    // intensity = progress ^ this. 1 = linear; less than 1 rises faster early and flattens
    // out approaching full intensity; greater than 1 starts slower and ramps up late.
    public float GrayscaleEaseExponent { get; set; } = 0.4f;

    // Vignette radius at the start, in aspect-corrected normalized screen units (screen
    // edges are roughly at 0.5-0.9 depending on aspect ratio) — bigger than that means no
    // visible darkening at all until the effect starts closing in. Shrinks linearly to 0
    // (fully black) by EffectDurationSeconds.
    public float VignetteStartRadius { get; set; } = 1.5f;

    // Softness of the vignette's edge, in the same normalized units.
    public float VignetteFeatherRadius { get; set; } = 0.2f;

    // Reshapes how the vignette closes in over the effect's progress (0..1) via
    // t = progress ^ this. 1 = linear; greater than 1 starts slow and accelerates toward
    // the end; less than 1 starts fast and eases off.
    public float VignetteEaseExponent { get; set; } = 2.5f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static SuffocationEffectConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<SuffocationEffectConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new SuffocationEffectConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
