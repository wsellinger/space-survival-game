namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Per-entity movement tuning, populated from ShipConfig at creation time.</summary>
public struct ShipMovement
{
    public float ThrustAcceleration; // meters/sec^2; Force = mass * this
    public float MaxSpeedMetersPerSecond;
    public float StrafeMaxSpeedMetersPerSecond;
    public float StrafeSpeedCapAngleThresholdRadians; // beyond this angle off facing, thrust is weak enough to use StrafeMaxSpeedMetersPerSecond instead
    public float TurnSpeedRadiansPerSecond;
    public float ThrustAngleThresholdRadians; // thrust cuts out once facing strays further than this from the requested input direction

    // Runtime state, not static config: set by ShipInputSystem each frame so SpeedCapSystem
    // (which runs later the same frame, after the physics step) knows which speed cap to enforce.
    public bool UseStrafeSpeedCap;

    // Runtime state, not static config: whether the independent-aim input (right stick/RMB) is
    // currently held, regardless of whether thrust actually fired this frame — drives
    // StrafeModeIndicatorRenderer's on-screen "STRAFE MODE" indicator.
    public bool IsStrafing;
}
