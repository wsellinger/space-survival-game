using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// No drag means nothing else slows entities down, so this hard-caps speed instead. Run once
/// per frame after the physics step. Ships get a separate, lower cap while
/// ShipMovement.UseStrafeSpeedCap is set — meaning this frame's actual thrust was angled more
/// than StrafeSpeedCapAngleThresholdRadians off facing, i.e. meaningfully using the weaker
/// side/reverse jets rather than the main one. That flag is set fresh by ShipInputSystem
/// earlier the same frame, before the step this cap is enforcing against.
/// </summary>
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
            var maxSpeed = movement.UseStrafeSpeedCap ? movement.StrafeMaxSpeedMetersPerSecond : movement.MaxSpeedMetersPerSecond;
            if (speed > maxSpeed)
            {
                B2Api.b2Body_SetLinearVelocity(bodyId, velocity * (maxSpeed / speed));
            }
        });
    }
}
