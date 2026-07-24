using System;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Reads keyboard/mouse or gamepad for the player-controlled entity's movement
/// (mutually exclusive — <paramref name="useController"/>, tracked by MainGame
/// based on whichever device was used most recently, picks one and the other
/// is ignored). Facing: in controller mode the right stick aims independently
/// whenever it's pushed past its deadzone, falling back to the left stick's
/// direction otherwise; in keyboard/mouse mode, holding the left mouse button
/// aims the same way (<paramref name="mouseFacingDirection"/>, precomputed by
/// MainGame as the cursor's direction from the ship), falling back to WASD
/// direction otherwise. WASD/left-stick input doesn't push the ship directly —
/// it only triggers thrust (any nonzero input) and, absent an independent aim
/// input, sets where the ship turns to face. The actual thrust force always
/// points along the ship's current facing (its real body rotation, not the aim
/// target it's turning towards), scaled by how far the input is pushed, so the
/// ship always visibly accelerates out of its own nose; SpeedCapSystem enforces
/// a flat top speed on top of this.
/// Also maintains EngineThrottle (0-1, drives the exhaust flame drawn by
/// EngineJetRenderer): the raw left-stick magnitude for controller input, or an
/// eased ramp toward 0/1 for keyboard's inherently on-off input — zero either
/// way whenever thrust itself is cut out by the angle threshold, so the flame
/// never shows while the engine isn't actually firing.
/// </summary>
public static class ShipInputSystem
{
    private static readonly QueryDescription Query =
        new QueryDescription().WithAll<PhysicsBody, ShipMovement, EngineThrottle, PlayerControlled>();

    public static void Run(World world, KeyboardState keyboard, GamePadState gamePad, bool useController, Vector2? mouseFacingDirection, float deltaSeconds, EngineConfig engineConfig)
    {
        world.Query(in Query, (ref PhysicsBody physicsBody, ref ShipMovement movement, ref EngineThrottle throttle) =>
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

                if (mouseFacingDirection.HasValue)
                    facingDirection = mouseFacingDirection.Value;
                else if (direction != Vector2.Zero)
                    facingDirection = direction;
            }

            // Thrust always fires out of the ship's actual current nose direction, not the raw
            // input vector — input only triggers thrust and (elsewhere) steers where it turns to.
            // But it cuts out entirely (force AND flame) once facing has drifted more than
            // ThrustAngleThresholdRadians from the requested direction, instead of firing wildly
            // off-target while turning to catch up.
            var currentAngle = B2Api.b2Body_GetRotation(bodyId).GetAngle();
            var thrustDirection = new Vector2(MathF.Cos(currentAngle), MathF.Sin(currentAngle));
            var thrustAllowed = false;

            if (direction != Vector2.Zero)
            {
                var inputDirection = Vector2.Normalize(direction);
                var angleFromInput = MathF.Acos(Math.Clamp(Vector2.Dot(thrustDirection, inputDirection), -1f, 1f));
                thrustAllowed = angleFromInput <= movement.ThrustAngleThresholdRadians;
            }

            if (thrustAllowed)
            {
                var mass = B2Api.b2Body_GetMass(bodyId);
                B2Api.b2Body_ApplyForceToCenter(bodyId, thrustDirection * (mass * movement.ThrustAcceleration * direction.Length()), wake: true);
            }

            if (useController)
            {
                throttle.Current = thrustAllowed ? direction.Length() : 0f;
            }
            else
            {
                var targetThrottle = thrustAllowed ? 1f : 0f;
                var maxStep = engineConfig.KeyboardThrottleEaseSpeed * deltaSeconds;
                throttle.Current += Math.Clamp(targetThrottle - throttle.Current, -maxStep, maxStep);
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
