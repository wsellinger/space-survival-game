namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable camera values, loaded from a JSON file next to the executable so
/// they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class CameraConfig
{
    // How far the camera pulls out ahead of the ship's facing when aiming with the right
    // stick, at full deflection. Scales linearly with how far the stick is held; 0 at rest.
    public float MaxDistanceMeters { get; set; } = 5f;

    // How quickly the camera eases toward its target position/look-ahead offset rather
    // than snapping instantly. Higher = snappier catch-up, lower = more lag/smoothing.
    public float TweenSpeed { get; set; } = 8f;

    // Mouse/keyboard mode only: the camera's focus point sits somewhere between the ship's
    // on-screen position and the cursor's. 0 = focus stays on the ship (cursor position has
    // no effect), 1 = focus sits exactly at the cursor, 0.5 = halfway between.
    public float MouseFocusRatio { get; set; } = 0.5f;

    public static CameraConfig Load(string path) => ConfigLoader.Load<CameraConfig>(path);
}
