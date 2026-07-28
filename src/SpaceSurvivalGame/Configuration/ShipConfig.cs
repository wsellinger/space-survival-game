namespace SpaceSurvivalGame.Configuration;

public class ThrustConfig
{
    public float Acceleration { get; set; } = 8f;

    // Thrust cuts out entirely once the ship's actual facing strays this many degrees from
    // the requested WASD/left-stick direction — has to turn back within the cone before it
    // fires again, rather than always burning out of whatever way it currently happens to face.
    public float AngleThresholdDegrees { get; set; } = 90f;
}

/// <summary>
/// Separate top speed enforced while thrust in strafe mode is pointed more than
/// SpeedCapAngleThresholdDegrees away from the ship's actual facing — representing the
/// side/reverse thrusters being weaker than the main tail jet, so strafing hard sideways or
/// backward tops out lower than burning straight out of the nose (even while aiming
/// independently). Thrust close enough to dead-ahead still gets the normal top speed.
/// </summary>
public class StrafeConfig
{
    public float MaxSpeedMetersPerSecond { get; set; } = 4f;
    public float SpeedCapAngleThresholdDegrees { get; set; } = 30f;
}

/// <summary>
/// Tunable ship movement values, loaded from a JSON file next to the executable
/// so they can be edited without recompiling. If the file is missing, a default
/// one is written out so there's always something to open and tweak.
/// </summary>
public class ShipConfig
{
    public float MaxSpeedMetersPerSecond { get; set; } = 4f;
    public float TurnSpeedRadiansPerSecond { get; set; } = 8f;
    public int SpriteSize { get; set; } = 24;

    // How quickly SpeedCapSystem eases speed down toward whichever cap currently applies
    // (MaxSpeedMetersPerSecond, or StrafeMaxSpeedMetersPerSecond while UseStrafeSpeedCap is
    // set) once it's exceeded, rather than clamping instantly — same TweenSpeed-style
    // exponential-decay convention as CameraConfig.TweenSpeed. Most noticeable the moment
    // strafe mode's lower cap first engages while already going faster than it. <= 0 disables
    // easing entirely and snaps straight to the cap, like before this existed.
    public float SpeedCapEaseSpeed { get; set; } = 8f;

    // How far forward the hull's back notch is pulled, as a fraction of the distance from the
    // wing corners to the nose (0 = flat back/plain triangle, higher = deeper concave notch).
    // Visual only — the physics collider stays the simpler flat-back triangle. The main engine
    // jet (EngineJetRenderer) mounts at this notch point instead of the flat back-center.
    public float NotchDepthFraction { get; set; } = 0.35f;

    // Same flat-panel look as the station core's build effect (see StationCoreBuildConfig/
    // StationCoreCircuitConfig) — a solid hull color plus accent trim lines along the hull's
    // side/back edges, deliberately simpler than the core's own busier circuit-board trace
    // pattern. Lighter than the core's own colors so the ship reads as its own thing rather
    // than a literal copy.
    public string ColorHex { get; set; } = "#888888";
    public string AccentColorHex { get; set; } = "#444444";

    // Color of the "mounting socket" circle baked at the ship's own center, sized to match the
    // station core's own dot (see ShipEntity.Create) — hidden underneath the core's own sprite
    // while it's still riding attached, revealed once it detaches, marking where it used to sit.
    public string SocketColorHex { get; set; } = "#222222";

    public ThrustConfig Thrust { get; set; } = new();
    public StrafeConfig Strafe { get; set; } = new();

    public static ShipConfig Load(string path) => ConfigLoader.Load<ShipConfig>(path);
}
