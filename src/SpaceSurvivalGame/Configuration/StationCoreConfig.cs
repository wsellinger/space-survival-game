namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable station core values, loaded from a JSON file next to the executable so they can be
/// edited without recompiling. If the file is missing, a default one is written out so there's
/// always something to open and tweak.
/// </summary>
public class StationCoreConfig
{
    // How much Iron.Current the player must have accumulated before the core detaches from the
    // ship and becomes an independent object — see StationCoreSystem.
    public float IronAmountRequired { get; set; } = 200f;

    public int SpriteSizePixels { get; set; } = 28;

    // Parsed via SpaceSurvivalGame.Rendering.ColorHex — "#RRGGBB" or "#RRGGBBAA".
    public string CoreColorHex { get; set; } = "#FF1A1A"; // shiny red
    public string RingColorHex { get; set; } = "#999999"; // grey

    // Fraction of the sprite's own radius that's solid CoreColorHex before RingColorHex takes
    // over out to the full radius.
    public float InnerRadiusFraction { get; set; } = 0.55f;

    public static StationCoreConfig Load(string path) => ConfigLoader.Load<StationCoreConfig>(path);
}
