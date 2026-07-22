using System;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Reads keyboard/mouse or gamepad for the player-controlled entity's movement
/// (mutually exclusive — <paramref name="useController"/>, tracked by MainGame
/// based on whichever device was used most recently, picks one and the other
/// is ignored). Facing: in controller mode the right stick aims independently
/// whenever it's pushed past its deadzone, falling back to the left stick's
/// direction otherwise; in keyboard/mouse mode facing just tracks WASD (no
/// separate aim input there). Thrust acceleration is facing-agnostic — same
/// magnitude regardless of which way the ship is pointed relative to its
/// velocity; SpeedCapSystem enforces a flat top speed on top of this.
/// </summary>
public static class ShipInputSystem
{
    private static readonly QueryDescription Query =
        new QueryDescription().WithAll<PhysicsBody, ShipMovement, PlayerControlled>();

    public static void Run(World world, KeyboardState keyboard, GamePadState gamePad, bool useController, float deltaSeconds)
    {
        world.Query(in Query, (ref PhysicsBody physicsBody, ref ShipMovement movement) =>
        {
            var bodyId = physicsBody.BodyId;

            var direction = Vector2.Zero;
            Vector2? facingDirection = null;

            if (useController)
            {
                // Thumbstick Y is up-positive; our world/screen convention is down-positive, so flip it.
                var leftStick = gamePad.ThumbSticks.Left;
                direction = new Vector2(leftStick.X, -leftStick.Y);
                if (direction.LengthSquared() > 1f) direction = Vector2.Normalize(direction);

                var rightStick = gamePad.ThumbSticks.Right;
                if (rightStick.LengthSquared() > 0f)
                    facingDirection = new Vector2(rightStick.X, -rightStick.Y);
                else if (direction != Vector2.Zero)
                    facingDirection = direction;
            }
            else
            {
                if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) direction += new Vector2(0, -1);
                if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) direction += new Vector2(0, 1);
                if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) direction += new Vector2(-1, 0);
                if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) direction += new Vector2(1, 0);
                if (direction != Vector2.Zero) direction = Vector2.Normalize(direction);

                if (direction != Vector2.Zero) facingDirection = direction;
            }

            if (direction != Vector2.Zero)
            {
                var mass = B2Api.b2Body_GetMass(bodyId);
                B2Api.b2Body_ApplyForceToCenter(bodyId, direction * (mass * movement.ThrustAcceleration), wake: true);
            }

            if (facingDirection.HasValue)
            {
                var targetAngle = MathF.Atan2(facingDirection.Value.Y, facingDirection.Value.X);
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
