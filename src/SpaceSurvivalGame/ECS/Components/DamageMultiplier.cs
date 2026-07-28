namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Optional companion to Damaging — scales how much collision damage that specific entity deals (see CollisionDamageSystem), 1 being the same as an entity with no multiplier at all. E.g. the station core deals reduced damage compared to an ordinary asteroid hit.</summary>
public struct DamageMultiplier
{
    public float Value;
}
