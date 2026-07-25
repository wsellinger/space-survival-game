namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable hit-flash values, loaded from a JSON file next to the executable so they
/// can be edited without recompiling. If the file is missing, a default one is written
/// out so there's always something to open and tweak.
/// </summary>
public class HitFlashConfig
{
    public float FlashDurationSeconds { get; set; } = 0.25f;

    // How strongly the flash reads at the instant of impact; 1 = fully red, fading
    // linearly back to white over FlashDurationSeconds. Lower values blend less.
    public float FlashIntensity { get; set; } = 1f;

    public static HitFlashConfig Load(string path) => ConfigLoader.Load<HitFlashConfig>(path);
}
