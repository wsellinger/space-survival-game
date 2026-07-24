namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Per-entity movement tuning, populated from ShipConfig at creation time.</summary>
public struct ShipMovement
{
    public float ThrustAcceleration; // meters/sec^2; Force = mass * this
    public float MaxSpeedMetersPerSecond;
    public float TurnSpeedRadiansPerSecond;
    public float ThrustAngleThresholdRadians; // thrust cuts out once facing strays further than this from the requested input direction
}
