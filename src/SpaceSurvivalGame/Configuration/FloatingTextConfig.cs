namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable values for the "+N Resource" popup that appears above the ship when a pickup is
/// collected (see ParticleEffects.SpawnFloatingText / FloatingTextSystem / FloatingTextRenderSystem),
/// fading out while rising. Pure screen space, not world space — the rise rate is a constant
/// pixel speed independent of the camera's own position/zoom. Loaded from a JSON file next to the
/// executable so it can be edited without recompiling.
/// </summary>
public class FloatingTextConfig
{
    public float DurationSeconds { get; set; } = 1.2f;
    public float RiseSpeedPixelsPerSecond { get; set; } = 150f;

    // How far above the ship's own center the popup spawns, in pixels — screen-space "up", not
    // relative to the ship's facing.
    public float SpawnHeightAbovePixels { get; set; } = 20f;

    public float TextScale { get; set; } = 1f;

    public static FloatingTextConfig Load(string path) => ConfigLoader.Load<FloatingTextConfig>(path);
}
