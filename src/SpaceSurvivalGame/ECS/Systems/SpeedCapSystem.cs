using System;
using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// No drag means nothing else slows entities down, so this caps speed instead. Run once per
/// frame after the physics step. Ships get a separate, lower cap while
/// ShipMovement.UseStrafeSpeedCap is set — meaning this frame's actual thrust was angled more
/// than StrafeSpeedCapAngleThresholdRadians off facing, i.e. meaningfully using the weaker
/// side/reverse jets rather than the main one. That flag is set fresh by ShipInputSystem
/// earlier the same frame, before the step this cap is enforcing against. Whenever current
/// speed exceeds whichever cap applies, speed eases back down toward it at easeSpeed (same
/// TweenSpeed-style exponential decay as Camera.MoveTowardTarget) rather than clamping
/// instantly — most noticeable the moment strafe mode's lower cap first engages.
/// </summary>
public static class SpeedCapSystem
{
    private static readonly QueryDescription Query =
        new QueryDescription().WithAll<PhysicsBody, ShipMovement>();

    public static void Run(World world, float deltaSeconds, float easeSpeed)
    {
        world.Query(in Query, (ref PhysicsBody physicsBody, ref ShipMovement movement) =>
        {
            var bodyId = physicsBody.BodyId;
            var velocity = B2Api.b2Body_GetLinearVelocity(bodyId);
            var speed = velocity.Length();
            var maxSpeed = movement.UseStrafeSpeedCap ? movement.StrafeMaxSpeedMetersPerSecond : movement.MaxSpeedMetersPerSecond;
            if (speed <= maxSpeed) return;

            if (easeSpeed <= 0f)
            {
                B2Api.b2Body_SetLinearVelocity(bodyId, velocity * (maxSpeed / speed));
                return;
            }

            var t = 1f - MathF.Exp(-easeSpeed * deltaSeconds);
            var newSpeed = speed + (maxSpeed - speed) * t;
            B2Api.b2Body_SetLinearVelocity(bodyId, velocity * (newSpeed / speed));
        });
    }
}
