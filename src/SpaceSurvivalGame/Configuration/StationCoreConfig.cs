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

    // Once landed, the core becomes a dynamic Box2D body (see StationCoreSystem.CreatePhysicsBody)
    // instead of a fixed fixture, so it can actually be knocked around by later collisions rather
    // than acting as an immovable wall. Density drives its mass (bigger footprint + higher density
    // = harder to push); restitution is kept low since a heavy built structure should mostly
    // absorb impacts rather than bounce.
    public float PhysicsMaterialDensity { get; set; } = 1f;
    public float PhysicsRestitution { get; set; } = 0.2f;

    // Damps out drift velocity between impulses (see below) so each nudge glides to a stop within
    // a couple seconds instead of coasting freely forever (Box2D bodies have zero damping by
    // default, same as the ship) — without this the core would just keep drifting in whatever
    // direction its last impulse pointed.
    public float PhysicsLinearDamping { get; set; } = 0.6f;
    public float PhysicsAngularDamping { get; set; } = 0.6f;

    // Once landed, StationCoreSystem gives the core a small random linear + angular impulse every
    // DriftImpulseIntervalSecondsRange seconds (re-rolled after each one fires) so it wanders very
    // gently in place instead of sitting frozen. Each impulse is the random component below plus a
    // correction nudging it back toward HomePositionMeters/HomeRotationRadians (where it landed),
    // scaled by DriftReturnStrength/DriftAngularReturnStrength — so it never drifts or spins up
    // indefinitely, just wanders and settles back. The angular values are tiny compared to the
    // linear ones because the core's own rotational inertia (a small ~0.35m square) is tiny too —
    // don't scale these up without checking the resulting rad/s, it gets fast very quickly.
    public FloatRange DriftImpulseIntervalSecondsRange { get; set; } = new(4f, 9f);
    public FloatRange DriftLinearImpulseStrengthRange { get; set; } = new(0.01f, 0.03f); // Ns, random direction each time
    public float DriftReturnStrength { get; set; } = 0.05f; // extra impulse (Ns) per meter of drift from HomePositionMeters
    public FloatRange DriftAngularImpulseStrengthRange { get; set; } = new(0.0002f, 0.0006f); // random sign each time
    public float DriftAngularReturnStrength { get; set; } = 0.0005f; // extra angular impulse per radian of drift from HomeRotationRadians

    // Given directly to the body at creation (bodyDef.linearVelocity/angularVelocity) so the core
    // is already gently moving the instant it lands, instead of sitting dead still until the first
    // drift impulse fires. Roughly the same speeds a single drift impulse would itself produce
    // (mass ~0.14kg, inertia ~0.0035 for the default BuildEffectMaxSizePixels square), just given
    // as a starting velocity instead of an impulse.
    public FloatRange InitialLinearSpeedMetersPerSecondRange { get; set; } = new(0.05f, 0.15f); // random direction
    public FloatRange InitialAngularSpeedRadiansPerSecondRange { get; set; } = new(0.03f, 0.1f); // random sign

    public static StationCoreConfig Load(string path) => ConfigLoader.Load<StationCoreConfig>(path);
}
