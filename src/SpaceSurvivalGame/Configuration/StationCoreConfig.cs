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

    // Candidates closer than this to the ship's own position at the moment of detaching are
    // excluded too — a failsafe so the core (and its arrival shockwave) never ends up parked
    // right on top of the ship, however good that spot's asteroid clearance looks otherwise.
    public float MinDistanceFromShipMeters { get; set; } = 2f;

    // Independently shape the first and second half of the flight's speed curve over its
    // (fixed, distance/FlightSpeedMetersPerSecond) duration: 1 = constant speed for that half (no
    // easing), higher = a more pronounced ease. FlightEaseInExponent governs the slow-start half,
    // FlightEaseOutExponent the slow-finish half — the two meet at the midpoint regardless of how
    // different they are, so there's no visible seam even with very different values.
    public float FlightEaseInExponent { get; set; } = 2f;
    public float FlightEaseOutExponent { get; set; } = 2f;

    // A grey square that grows from nothing to full size and spins to a stop behind the core's
    // own dot (see StationCoreBuildEffectRenderSystem) — starts the instant the core detaches and
    // finishes exactly when it arrives, sharing the same eased flight progress so the reveal is
    // tied directly to the core's own movement. Stays fully grown afterward.
    public string BuildEffectColorHex { get; set; } = "#666666";
    public int BuildEffectMaxSizePixels { get; set; } = 48;
    public float BuildEffectSpinRevolutions { get; set; } = 2f; // full turns completed over the course of growing — an integer lands the final rotation back at 0, reading as "settled" rather than stopped mid-spin

    // Circuit-board-style trace lines baked into the square, symmetric across both axes (see
    // ProceduralTextures.CreateCircuitSquare).
    public string CircuitLineColorHex { get; set; } = "#222222";
    public float CircuitLineThicknessFraction { get; set; } = 0.08f; // fraction of the square's own half-size

    // Fires once, when the flight's own (un-eased) progress first reaches this fraction —
    // 1 = only once fully arrived, lower = fires a bit before the core actually stops (from
    // wherever it happens to be at that instant, not the final target). An outward Box2D impulse
    // on every nearby PhysicsBody (ship, asteroids, pickups alike), falling off linearly to 0 at
    // ShockwaveRadiusMeters, paired with a matching barely-visible expanding ring
    // (StationCoreShockwaveRenderSystem) that fades out over ShockwaveDurationSeconds — the same
    // radius drives both the physics push and how far the visual ring grows, so what you see
    // lines up with what's actually affected.
    public float ShockwaveTriggerProgress { get; set; } = 0.9f;
    public string ShockwaveColorHex { get; set; } = "#FFFFFF";
    public float ShockwaveMaxAlpha { get; set; } = 0.2f; // barely visible, per spec
    public float ShockwaveDurationSeconds { get; set; } = 0.5f;
    public float ShockwaveRadiusMeters { get; set; } = 8f;
    public float ShockwaveRingInnerRadiusFraction { get; set; } = 0.85f; // how thin the ring band is — closer to 1 = thinner
    public float ShockwaveImpulseStrength { get; set; } = 5f; // impulse (Ns) at zero distance from the core, fading to 0 at ShockwaveRadiusMeters

    // Multiplies the impulse specifically for the player's own ship — 1 = same as everything
    // else, lower = the ship barely feels it while asteroids/pickups still get the full push.
    public float ShockwaveShipImpulseMultiplier { get; set; } = 0.15f;

    // Same idea for O2/iron pickups (OxygenPickup/IronPickup) — they're light enough that the
    // full impulse sends them flying far more violently than asteroids do; tuned down separately
    // from the ship's own multiplier.
    public float ShockwavePickupImpulseMultiplier { get; set; } = 0.25f;

    public static StationCoreConfig Load(string path) => ConfigLoader.Load<StationCoreConfig>(path);
}
