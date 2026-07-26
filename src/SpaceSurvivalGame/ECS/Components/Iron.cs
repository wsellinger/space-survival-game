namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// Raw iron ore collected by the ship (see IronPickupSystem) — destined to be converted into
/// aerospace aluminum once a refining system exists. No cap, no UI, and nothing consumes it yet;
/// this just accumulates a running total for whenever that lands.
/// </summary>
public struct Iron
{
    public float Current;
}
