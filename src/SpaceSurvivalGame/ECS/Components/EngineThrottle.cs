namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Current 0-1 engine throttle, driving the visual exhaust flame's size/intensity. Set directly from stick magnitude for controller input; eased toward 0/1 for keyboard's on-off input (see ShipInputSystem).</summary>
public struct EngineThrottle
{
    public float Current;
}
