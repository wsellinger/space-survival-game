namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Tag component: marks an entity as one CollisionDamageSystem should treat as capable of dealing/receiving collision damage (e.g. the ship, asteroids). Entities without this tag — pickups, etc. — can still physically collide via Box2D but never trigger damage.</summary>
public struct Damaging
{
}
