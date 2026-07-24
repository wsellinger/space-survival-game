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
/// direction otherwise; in keyboard/mouse mode, holding the right mouse button
/// aims the same way (<paramref name="mouseFacingDirection"/>, precomputed by
/// MainGame as the cursor's direction from the ship), falling back to WASD
/// direction otherwise.
///
/// Whenever that independent aim input is actually held (right stick pushed, or
/// RMB down) — "strafe mode" — WASD/left-stick pushes the ship directly in the
/// input's own direction, same as a raw twin-stick shooter, letting you aim one
/// way and fly another. The rest of the time, WASD/left-stick only triggers
/// thrust and steers where the ship turns to face; the actual thrust force
/// points along the ship's current facing (its real body rotation, not the aim
/// target it's turning towards) and cuts out entirely once facing has drifted
/// more than ThrustAngleThresholdRadians from the requested direction, instead
/// of firing off-target while turning to catch up. SpeedCapSystem enforces a
/// top speed on top of either mode — normally ShipMovement.MaxSpeedMetersPerSecond,
/// but StrafeMaxSpeedMetersPerSecond instead whenever the actual thrust direction
/// is more than StrafeSpeedCapAngleThresholdRadians off facing (representing the
/// side/reverse jets being weaker than the main one), regardless of which mode
/// produced that thrust — thrust is always facing-aligned outside strafe mode, so
/// this only ever engages while actually strafing hard.
///
/// Also maintains EngineThrottle by decomposing whatever thrust actually fired
/// onto the ship's own forward/right axes: Current (0-1, forward) drives the
/// main tail jet. LeftStrafe/RightStrafe drive the two strafe jets near the
/// nose — a sideways push lights up just the one opposite the push direction
/// (reaction thrust), while a backward push (no rear jet exists) lights up
/// both, so reversing still visibly fires the engines; diagonal thrust blends
/// the two. Outside strafe mode thrust is always exactly forward-aligned, so
/// both strafe jets naturally stay zero and only the tail jet ever lights up —
/// the two modes share one code path rather than branching the rendering
/// separately.
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
            var strafeMode = false;

            if (useController)
            {
                // Thumbstick Y is up-positive; our world/screen convention is down-positive, so flip it.
                var leftStick = gamePad.ThumbSticks.Left;
                direction = new Vector2(leftStick.X, -leftStick.Y);
                if (direction.LengthSquared() > 1f) direction = Vector2.Normalize(direction);

                var rightStick = gamePad.ThumbSticks.Right;
                strafeMode = rightStick.LengthSquared() > 0f;
                if (strafeMode)
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

                strafeMode = mouseFacingDirection.HasValue;
                if (strafeMode)
                    facingDirection = mouseFacingDirection.Value;
                else if (direction != Vector2.Zero)
                    facingDirection = direction;
            }

            movement.IsStrafing = strafeMode;

            var currentAngle = B2Api.b2Body_GetRotation(bodyId).GetAngle();
            var facingVector = new Vector2(MathF.Cos(currentAngle), MathF.Sin(currentAngle));
            var rightVector = new Vector2(-facingVector.Y, facingVector.X);

            var thrustFiring = false;
            var thrustDirection = Vector2.Zero;
            var thrustMagnitude = 0f;

            if (direction != Vector2.Zero)
            {
                if (strafeMode)
                {
                    // Strafe mode: thrust matches the raw input direction directly, no facing
                    // lock and no angle gating — free twin-stick-style movement while aiming.
                    thrustDirection = Vector2.Normalize(direction);
                    thrustMagnitude = direction.Length();
                    thrustFiring = true;
                }
                else
                {
                    // Normal mode: thrust only fires out of the ship's actual nose, and only
                    // within the angle cone around the requested direction.
                    var inputDirection = Vector2.Normalize(direction);
                    var angleFromInput = MathF.Acos(Math.Clamp(Vector2.Dot(facingVector, inputDirection), -1f, 1f));
                    if (angleFromInput <= movement.ThrustAngleThresholdRadians)
                    {
                        thrustDirection = facingVector;
                        thrustMagnitude = direction.Length();
                        thrustFiring = true;
                    }
                }
            }

            if (thrustFiring)
            {
                var mass = B2Api.b2Body_GetMass(bodyId);
                B2Api.b2Body_ApplyForceToCenter(bodyId, thrustDirection * (mass * movement.ThrustAcceleration * thrustMagnitude), wake: true);
            }

            // Decompose the actual applied thrust onto the ship's own axes for the jet visuals.
            // Forward-Backward and Left-Right are the (orthogonal, so their squares sum to 1
            // when thrust is firing) components of thrustDirection along facing/right. There's
            // no rear thruster modeled, so a backward component (BackwardComponent) instead
            // lights up BOTH strafe jets equally, on top of whatever sideways lean they already
            // have from LeftRightComponent — reversing still reads as the engines firing.
            var forwardComponent = thrustFiring ? Vector2.Dot(thrustDirection, facingVector) : 0f;
            var leftRightComponent = thrustFiring ? Vector2.Dot(thrustDirection, rightVector) : 0f;
            var backwardComponent = MathF.Max(0f, -forwardComponent);

            // The weaker strafe cap only kicks in once actual thrust is angled far enough off
            // facing that it's meaningfully using the side/reverse jets rather than the main one.
            var angleFromFacing = thrustFiring ? MathF.Acos(Math.Clamp(forwardComponent, -1f, 1f)) : 0f;
            movement.UseStrafeSpeedCap = thrustFiring && angleFromFacing > movement.StrafeSpeedCapAngleThresholdRadians;

            var targetForward = MathF.Max(0f, forwardComponent) * thrustMagnitude;
            var targetLeftStrafe = MathF.Min(1f, MathF.Max(0f, leftRightComponent) + backwardComponent) * thrustMagnitude;
            var targetRightStrafe = MathF.Min(1f, MathF.Max(0f, -leftRightComponent) + backwardComponent) * thrustMagnitude;

            if (useController)
            {
                throttle.Current = targetForward;
                throttle.LeftStrafe = targetLeftStrafe;
                throttle.RightStrafe = targetRightStrafe;
            }
            else
            {
                var maxStep = engineConfig.KeyboardThrottleEaseSpeed * deltaSeconds;
                throttle.Current += Math.Clamp(targetForward - throttle.Current, -maxStep, maxStep);
                throttle.LeftStrafe += Math.Clamp(targetLeftStrafe - throttle.LeftStrafe, -maxStep, maxStep);
                throttle.RightStrafe += Math.Clamp(targetRightStrafe - throttle.RightStrafe, -maxStep, maxStep);
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
