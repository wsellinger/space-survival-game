using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>No drag means nothing else slows entities down, so this hard-caps speed instead. Run once per frame after the physics step.</summary>
public static class SpeedCapSystem
{
    private static readonly QueryDescription Query =
        new QueryDescription().WithAll<PhysicsBody, ShipMovement>();

    public static void Run(World world)
    {
        world.Query(in Query, (ref PhysicsBody physicsBody, ref ShipMovement movement) =>
        {
            var bodyId = physicsBody.BodyId;
            var velocity = B2Api.b2Body_GetLinearVelocity(bodyId);
            var speed = velocity.Length();
            if (speed > movement.MaxSpeedMetersPerSecond)
            {
                B2Api.b2Body_SetLinearVelocity(bodyId, velocity * (movement.MaxSpeedMetersPerSecond / speed));
            }
        });
    }
}
