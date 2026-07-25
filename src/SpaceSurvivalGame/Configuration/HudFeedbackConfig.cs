namespace SpaceSurvivalGame.Configuration;

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
    public FloatRange ShakeMagnitudePixelsRange { get; set; } = new(1f, 6f);
    public float ShakeDecaySpeed { get; set; } = 10f;

    public static HudFeedbackConfig Load(string path) => ConfigLoader.Load<HudFeedbackConfig>(path);
}
