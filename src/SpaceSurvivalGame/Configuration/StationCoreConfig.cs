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

    // How fast the core flies toward its chosen open spot after detaching.
    public float FlightSpeedMetersPerSecond { get; set; } = 3f;

    // Samples per axis when searching the current on-screen area (at the moment of detaching)
    // for the point farthest from any asteroid's edge — higher = a better-found spot at the
    // cost of a (one-time, not per-frame) more expensive search.
    public int OpenSpotSearchResolution { get; set; } = 24;

    // Only candidates within this many meters of the detach position are considered — bounds
    // how far the core can end up flying from where it was built, even if a clearer spot exists
    // further away toward the edge of the screen. Candidates outside this range are treated as
    // invalid, not just deprioritized, so if none qualify the core simply stays put.
    public float MaxSearchRangeMeters { get; set; } = 15f;

    // Independently shape the first and second half of the flight's speed curve over its
    // (fixed, distance/FlightSpeedMetersPerSecond) duration: 1 = constant speed for that half (no
    // easing), higher = a more pronounced ease. FlightEaseInExponent governs the slow-start half,
    // FlightEaseOutExponent the slow-finish half — the two meet at the midpoint regardless of how
    // different they are, so there's no visible seam even with very different values.
    public float FlightEaseInExponent { get; set; } = 2f;
    public float FlightEaseOutExponent { get; set; } = 2f;

    public static StationCoreConfig Load(string path) => ConfigLoader.Load<StationCoreConfig>(path);
}
