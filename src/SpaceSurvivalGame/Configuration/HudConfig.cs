namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable HUD layout values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class HudConfig
{
    public HudBarConfig Bars { get; set; } = new();
    public HudIronCounterConfig IronCounter { get; set; } = new();

    public static HudConfig Load(string path) => ConfigLoader.Load<HudConfig>(path);
}

/// <summary>The Health/Oxygen bars — same layout for both, drawn one above the other.</summary>
public class HudBarConfig
{
    public int LengthPixels { get; set; } = 200;
    public int ThicknessPixels { get; set; } = 20;
    public int OutlineThicknessPixels { get; set; } = 2;
    public int MarginPixels { get; set; } = 16; // bottom offset — horizontally the bars are centered on the viewport instead of margin-based
    public int SpacingPixels { get; set; } = 6;

    // Shared by both bars' low-resource blink (flash-off-flash-off-off-off-off) so they stay
    // in sync rather than drifting apart with independently-tuned cadences.
    public float WarningFlashBeatSeconds { get; set; } = 0.15f;
}

/// <summary>
/// Iron has no Max (an uncapped resource, unlike Health/Oxygen), so it's a plain text counter
/// rather than a bar. Position is the screen-space pixel coordinate of the text's own top-left
/// corner (raw, not margin-relative) so it can be placed by hand.
/// </summary>
public class HudIronCounterConfig
{
    public float PositionX { get; set; } = 16f;
    public float PositionY { get; set; } = 1020f;
    public float TextScale { get; set; } = 1f;
}
