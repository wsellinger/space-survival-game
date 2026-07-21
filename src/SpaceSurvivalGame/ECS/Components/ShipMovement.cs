namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Per-entity movement tuning, populated from ShipConfig at creation time.</summary>
public struct ShipMovement
{
    public float ThrustAcceleration; // meters/sec^2; Force = mass * this
    public float MaxSpeedMetersPerSecond;
    public float TurnSpeedRadiansPerSecond;
}
