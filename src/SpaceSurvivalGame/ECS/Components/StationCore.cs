namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// A station core — spawns already riding at the ship's exact center (Attached = true; its own
/// Transform is copied from the ship's every frame by StationCoreSystem) until the player's
/// Iron.Current first reaches StationCoreConfig.IronAmountRequired, at which point it detaches
/// (Attached flips false, that amount is spent) and becomes an independent, stationary world
/// object wherever the ship happened to be at that instant. No physics/collision yet.
/// </summary>
public struct StationCore
{
    public bool Attached;
}
