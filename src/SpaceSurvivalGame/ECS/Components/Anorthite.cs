namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// Raw anorthite ore collected by the ship (see AnorthitePickupSystem) — destined to be
/// converted into aerospace aluminum once a refining system exists. No cap, no UI, and nothing
/// consumes it yet; this just accumulates a running total for whenever that lands.
/// </summary>
public struct Anorthite
{
    public float Current;
}
