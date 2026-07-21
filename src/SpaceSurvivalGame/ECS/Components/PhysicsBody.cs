using Box2dNet.Interop;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Links an entity to its Box2D body, which stays the source of truth for movement.</summary>
public struct PhysicsBody
{
    public b2BodyId BodyId;
}
