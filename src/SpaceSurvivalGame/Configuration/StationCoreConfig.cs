namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable station core values, loaded from a JSON file next to the executable so they can be
/// edited without recompiling. If the file is missing, a default one is written out so there's
/// always something to open and tweak. Grouped into nested sections by subsystem — IronAmountRequired
/// is the only top-level scalar, since it's the one master gameplay trigger and doesn't belong to
/// any single subsystem below.
/// </summary>
public class StationCoreConfig
{
    // How much Iron.Current the player must have accumulated before the core detaches from the
    // ship and becomes an independent object — see StationCoreSystem.
    public float IronAmountRequired { get; set; } = 200f;

    public StationCoreDotConfig CoreDot { get; set; } = new();
    public StationCoreFlightConfig Flight { get; set; } = new();
    public StationCoreBuildConfig Build { get; set; } = new();
    public StationCoreCircuitConfig Circuit { get; set; } = new();
    public StationCoreShockwaveConfig Shockwave { get; set; } = new();
    public StationCorePhysicsConfig Physics { get; set; } = new();
    public StationCoreDriftConfig Drift { get; set; } = new();
    public StationCoreInitialSpeedConfig InitialSpeed { get; set; } = new();
    public AntiGravityFieldConfig AntiGravityField { get; set; } = new();

    public static StationCoreConfig Load(string path) => ConfigLoader.Load<StationCoreConfig>(path);
}

/// <summary>The small shiny dot itself — a solid-color center out to InnerRadiusFraction, then RingColorHex out to the sprite's own edge.</summary>
public class StationCoreDotConfig
{
    public int SpriteSizePixels { get; set; } = 28;

    // Parsed via SpaceSurvivalGame.Rendering.ColorHex — "#RRGGBB" or "#RRGGBBAA".
    public string ColorHex { get; set; } = "#FF1A1A"; // shiny red
    public string RingColorHex { get; set; } = "#999999"; // grey

    // Fraction of the sprite's own radius that's solid ColorHex before RingColorHex takes over
    // out to the full radius.
    public float InnerRadiusFraction { get; set; } = 0.55f;
}

/// <summary>The one-time flight from detach position to the chosen open spot, and the search that picks that spot.</summary>
public class StationCoreFlightConfig
{
    // How fast the core flies toward its chosen open spot after detaching.
    public float SpeedMetersPerSecond { get; set; } = 3f;

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
    // (fixed, distance/SpeedMetersPerSecond) duration: 1 = constant speed for that half (no
    // easing), higher = a more pronounced ease. EaseInExponent governs the slow-start half,
    // EaseOutExponent the slow-finish half — the two meet at the midpoint regardless of how
    // different they are, so there's no visible seam even with very different values.
    public float EaseInExponent { get; set; } = 2f;
    public float EaseOutExponent { get; set; } = 2f;
}

/// <summary>
/// The grey square that grows from nothing to full size and spins to a stop behind the core's own
/// dot (see StationCoreBuildEffectRenderSystem) — starts the instant the core detaches and
/// finishes exactly when it arrives, sharing the same eased flight progress so the reveal is tied
/// directly to the core's own movement. Stays fully grown afterward, and its footprint
/// (MaxSizePixels) is also what the landed core's actual Box2D collider is sized to.
/// </summary>
public class StationCoreBuildConfig
{
    public string ColorHex { get; set; } = "#666666";
    public int MaxSizePixels { get; set; } = 48;
    public float SpinRevolutions { get; set; } = 2f; // full turns completed over the course of growing — an integer lands the final rotation back at 0, reading as "settled" rather than stopped mid-spin
}

/// <summary>Circuit-board-style trace lines baked into the build square, symmetric across both axes (see ProceduralTextures.CreateCircuitSquare).</summary>
public class StationCoreCircuitConfig
{
    public string ColorHex { get; set; } = "#222222";
    public float ThicknessFraction { get; set; } = 0.08f; // fraction of the square's own half-size
}

/// <summary>
/// Fires once, when the flight's own (un-eased) progress first reaches TriggerProgress —
/// 1 = only once fully arrived, lower = fires a bit before the core actually stops (from
/// wherever it happens to be at that instant, not the final target). An outward Box2D impulse
/// on every nearby PhysicsBody (ship, asteroids, pickups alike), falling off linearly to 0 at
/// RadiusMeters, paired with a matching barely-visible expanding ring
/// (StationCoreShockwaveRenderSystem) that fades out over DurationSeconds — the same radius
/// drives both the physics push and how far the visual ring grows, so what you see lines up with
/// what's actually affected.
/// </summary>
public class StationCoreShockwaveConfig
{
    public float TriggerProgress { get; set; } = 0.9f;
    public string ColorHex { get; set; } = "#FFFFFF";
    public float MaxAlpha { get; set; } = 0.2f; // barely visible, per spec
    public float DurationSeconds { get; set; } = 0.5f;
    public float RadiusMeters { get; set; } = 8f;
    public float RingInnerRadiusFraction { get; set; } = 0.85f; // how thin the ring band is — closer to 1 = thinner
    public float ImpulseStrength { get; set; } = 5f; // impulse (Ns) at zero distance from the core, fading to 0 at RadiusMeters

    // Multiplies the impulse specifically for the player's own ship — 1 = same as everything
    // else, lower = the ship barely feels it while asteroids/pickups still get the full push.
    public float ShipImpulseMultiplier { get; set; } = 0.15f;

    // Same idea for O2/iron pickups (OxygenPickup/IronPickup) — they're light enough that the
    // full impulse sends them flying far more violently than asteroids do; tuned down separately
    // from the ship's own multiplier.
    public float PickupImpulseMultiplier { get; set; } = 0.25f;
}

/// <summary>
/// Once landed, the core becomes a dynamic Box2D body (see StationCoreSystem.CreatePhysicsBody)
/// instead of a fixed fixture, so it can actually be knocked around by later collisions rather
/// than acting as an immovable wall. MaterialDensity drives its mass (bigger footprint + higher
/// density = harder to push); Restitution is kept low since a heavy built structure should mostly
/// absorb impacts rather than bounce. LinearDamping/AngularDamping damp out drift velocity between
/// impulses so each nudge glides to a stop within a couple seconds instead of coasting freely
/// forever (Box2D bodies have zero damping by default, same as the ship).
/// </summary>
public class StationCorePhysicsConfig
{
    public float MaterialDensity { get; set; } = 1f;
    public float Restitution { get; set; } = 0.2f;

    // Scales collision damage the core deals to the ship on contact (see CollisionDamageSystem)
    // relative to an ordinary Damaging hit (asteroid) at the same speed — 1 would be the same as
    // an asteroid, lower means bumping the core hurts less.
    public float CollisionDamageMultiplier { get; set; } = 0.5f;

    public float LinearDamping { get; set; } = 0.6f;
    public float AngularDamping { get; set; } = 0.6f;
}

/// <summary>
/// Once landed, StationCoreSystem gives the core a small random linear + angular impulse every
/// ImpulseIntervalSecondsRange seconds (re-rolled after each one fires) so it wanders very gently
/// in place instead of sitting frozen. Each impulse is the random component below plus a
/// correction nudging it back toward HomePositionMeters/HomeRotationRadians (where it landed),
/// scaled by ReturnStrength/AngularReturnStrength — so it never drifts or spins up indefinitely,
/// just wanders and settles back. The angular values are tiny compared to the linear ones because
/// the core's own rotational inertia (a small ~0.35m square) is tiny too — don't scale these up
/// without checking the resulting rad/s, it gets fast very quickly.
/// </summary>
public class StationCoreDriftConfig
{
    public FloatRange ImpulseIntervalSecondsRange { get; set; } = new(4f, 9f);
    public FloatRange LinearImpulseStrengthRange { get; set; } = new(0.01f, 0.03f); // Ns, random direction each time
    public float ReturnStrength { get; set; } = 0.05f; // extra impulse (Ns) per meter of drift from HomePositionMeters
    public FloatRange AngularImpulseStrengthRange { get; set; } = new(0.0002f, 0.0006f); // random sign each time
    public float AngularReturnStrength { get; set; } = 0.0005f; // extra angular impulse per radian of drift from HomeRotationRadians

    public StationCoreDriftPuffConfig Puffs { get; set; } = new();
}

/// <summary>
/// Given directly to the body at creation (bodyDef.linearVelocity/angularVelocity) so the core is
/// already gently moving the instant it lands, instead of sitting dead still until the first
/// drift impulse fires. Roughly the same speeds a single drift impulse would itself produce (mass
/// ~0.14kg, inertia ~0.0035 for the default StationCoreBuildConfig.MaxSizePixels square), just
/// given as a starting velocity instead of an impulse.
/// </summary>
public class StationCoreInitialSpeedConfig
{
    public FloatRange LinearMetersPerSecondRange { get; set; } = new(0.05f, 0.15f); // random direction
    public FloatRange AngularRadiansPerSecondRange { get; set; } = new(0.03f, 0.1f); // random sign
}

/// <summary>
/// A continuous outward force field around the landed core (see StationCoreSystem.
/// ApplyAntiGravityField) — every frame, every asteroid/O2-pickup/iron-pickup within
/// RadiusMeters gets pushed directly away from the core's center, force scaling from
/// ForceStrength at zero distance down to 0 at RadiusMeters (same linear falloff shape as the
/// one-time arrival shockwave, just applied continuously via b2Body_ApplyForceToCenter instead of
/// a single impulse), keeping the area right around the built station naturally clear. The ship
/// is deliberately left alone — this keeps debris out of the way, it isn't meant to be a hazard.
/// </summary>
public class AntiGravityFieldConfig
{
    public float RadiusMeters { get; set; } = 6f;
    public float ForceStrength { get; set; } = 2f; // outward force (N) at zero distance from the core, falling off linearly to 0 at RadiusMeters
}

/// <summary>
/// Small particle puffs (see ParticleEffects.SpawnRotationJetPuff, reused here — same idea as
/// RotationJetSystem's ship-turning puffs) that fire whenever a drift impulse fires (see
/// StationCoreSystem.ApplyDriftImpulse), angled to visually match that impulse: one puff opposite
/// the linear push direction, plus a diagonal pair of corner puffs for the angular one — same
/// thruster-reaction convention (exhaust fires opposite the push it causes) as the ship's own
/// rotation jets. The three don't fire together — see StaggerSeconds — and each one keeps
/// re-emitting for its own DurationSeconds once it starts, rather than firing just a single frame.
/// </summary>
public class StationCoreDriftPuffConfig
{
    // Delay between each of the three puffs in the sequence STARTING (linear, then the two
    // angular corner puffs), so they read as three quick puffs one after another instead of one
    // simultaneous cluster. 0 starts all three on the same frame.
    public float StaggerSeconds { get; set; } = 0.03f;

    // How long each individual puff, once started, keeps spawning fresh particles for — 0 fires
    // just a single instant burst; higher sustains it like a brief continuous hiss (still made up
    // of short-lived individual particles, per Puff.ParticleLifetimeSecondsRange). Independent of
    // StaggerSeconds — a puff's own duration can outlast the delay before the next one starts.
    public float DurationSeconds { get; set; } = 0.1f;

    public ParticlePuffConfig Puff { get; set; } = new(){ ParticleSpeedMetersPerSecondRange = new(0.3f, 0.7f), ParticleLifetimeSecondsRange = new(0.1f, 0.2f) };
}
