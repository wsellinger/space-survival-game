using System;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Reads keyboard/mouse for the player-controlled entity and drives its Box2D
/// body directly (WASD = absolute-screen-direction thrust, facing = mouse).
/// </summary>
public static class ShipInputSystem
{
    private static readonly QueryDescription Query =
        new QueryDescription().WithAll<PhysicsBody, ShipMovement, PlayerControlled>();

    public static void Run(World world, KeyboardState keyboard, Vector2 mousePositionPixels, float deltaSeconds)
    {
        world.Query(in Query, (ref PhysicsBody physicsBody, ref ShipMovement movement) =>
        {
            var bodyId = physicsBody.BodyId;

            var direction = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) direction += new Vector2(0, -1);
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) direction += new Vector2(0, 1);
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) direction += new Vector2(-1, 0);
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) direction += new Vector2(1, 0);

            if (direction != Vector2.Zero)
            {
                direction = Vector2.Normalize(direction);
                var mass = B2Api.b2Body_GetMass(bodyId);
                B2Api.b2Body_ApplyForceToCenter(bodyId, direction * (mass * movement.ThrustAcceleration), wake: true);
            }

            var positionPixels = PhysicsWorld.MetersToPixels(B2Api.b2Body_GetPosition(bodyId));
            var toMouse = mousePositionPixels - positionPixels;
            if (toMouse.LengthSquared() > 0.01f)
            {
                var targetAngle = MathF.Atan2(toMouse.Y, toMouse.X);
                TurnTowards(bodyId, targetAngle, movement.TurnSpeedRadiansPerSecond, deltaSeconds);
            }
            else
            {
                B2Api.b2Body_SetAngularVelocity(bodyId, 0f);
            }
        });
    }

    private static void TurnTowards(b2BodyId bodyId, float targetAngle, float turnSpeedRadiansPerSecond, float deltaSeconds)
    {
        var currentAngle = B2Api.b2Body_GetRotation(bodyId).GetAngle();
        var delta = WrapAngle(targetAngle - currentAngle);
        var maxStep = turnSpeedRadiansPerSecond * deltaSeconds;

        if (MathF.Abs(delta) <= maxStep)
        {
            // Close enough to reach this frame — snap exactly so we don't hunt around the target.
            B2Api.b2Body_SetAngularVelocity(bodyId, 0f);
            B2Api.b2Body_SetTransform(bodyId, B2Api.b2Body_GetPosition(bodyId), b2Rot.FromAngle(targetAngle));
        }
        else
        {
            B2Api.b2Body_SetAngularVelocity(bodyId, MathF.Sign(delta) * turnSpeedRadiansPerSecond);
        }
    }

    private static float WrapAngle(float angle)
    {
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        return angle;
    }
}
