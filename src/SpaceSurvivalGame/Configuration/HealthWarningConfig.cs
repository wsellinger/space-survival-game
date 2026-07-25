namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable health-bar low-health warning values, loaded from a JSON file next to the
/// executable so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class HealthWarningConfig
{
    // Below this fraction of MaxHealth (and above 0), the bar blinks in a repeating
    // flash-off-flash-off-off-off-off pattern (7 equal beats, lit on beats 0 and 2), same as
    // OxygenWarningConfig's low-oxygen warning. Cadence is HudConfig.WarningFlashBeatSeconds,
    // shared with the O2 bar so both blink in sync.
    public float LowHealthThresholdFraction { get; set; } = 0.3f;

    public static HealthWarningConfig Load(string path) => ConfigLoader.Load<HealthWarningConfig>(path);
}
