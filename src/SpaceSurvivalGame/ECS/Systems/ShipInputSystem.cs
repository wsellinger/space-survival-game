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
/// is ignored).
///
/// "Strafe mode" engages whenever independent aim input is actually held (right
/// stick pushed, or RMB down), OR a dedicated hold — Space (keyboard) or the
/// right trigger (controller) — is active, regardless of aim. Whenever it's
/// engaged, WASD/left-stick pushes the ship directly in the input's own
/// direction, same as a raw twin-stick shooter, instead of only steering where
/// it turns to face — see ResolveThrust, which branches on strafe mode alone
/// and doesn't care which of those triggered it. Facing while strafing: the
/// right stick/mouse cursor aims independently whenever actually pushed/aimed
/// at; the rest of the time (including the whole time when strafing was
/// triggered by Space/the right trigger rather than aiming) facing is simply
/// left alone — no target angle is set at all, so the ship just keeps
/// whatever facing it had, until the player provides a real aim input. Outside
/// strafe mode, WASD/left-stick both fires thrust and steers where the ship
/// turns to face — the actual thrust force
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

    private const float RightTriggerStrafeHoldThreshold = 0.1f; // matches MainGame's own controller-active-input threshold

    public static void Run(World world, KeyboardState keyboard, GamePadState gamePad, bool useController, Vector2? mouseFacingDirection, float deltaSeconds, EngineConfig engineConfig)
    {
        world.Query(in Query, (ref PhysicsBody physicsBody, ref ShipMovement movement, ref EngineThrottle throttle) =>
        {
            var bodyId = physicsBody.BodyId;

            ReadInput(keyboard, gamePad, useController, mouseFacingDirection, out var direction, out var facingDirection, out var strafeMode);
            movement.IsStrafing = strafeMode;

            ResolveThrust(bodyId, movement, direction, strafeMode, out var thrustFiring, out var thrustDirection,
                out var thrustMagnitude, out var facingVector, out var rightVector);

            ResolveThrottle(ref movement, ref throttle, useController, deltaSeconds, engineConfig, thrustFiring,
                thrustDirection, thrustMagnitude, facingVector, rightVector);

            UpdateFacing(bodyId, facingDirection, movement.TurnSpeedRadiansPerSecond, deltaSeconds);
        });
    }

    /// <summary>
    /// Thumbstick Y is up-positive; our world/screen convention is down-positive, so it's
    /// flipped below. See the class doc comment for how strafe mode/facing fall out of this.
    /// </summary>
    private static void ReadInput(KeyboardState keyboard, GamePadState gamePad, bool useController, Vector2? mouseFacingDirection,
        out Vector2 direction, out Vector2? facingDirection, out bool strafeMode)
    {
        direction = Vector2.Zero;
        facingDirection = null;

        if (useController)
        {
            var leftStick = gamePad.ThumbSticks.Left;
            direction = new Vector2(leftStick.X, -leftStick.Y);
            if (direction.LengthSquared() > 1f) direction = Vector2.Normalize(direction);

            var rightStick = gamePad.ThumbSticks.Right;
            var aiming = rightStick.LengthSquared() > 0f;
            strafeMode = aiming || gamePad.Triggers.Right > RightTriggerStrafeHoldThreshold;
            if (aiming)
                facingDirection = new Vector2(rightStick.X, -rightStick.Y);
            else if (!strafeMode && direction != Vector2.Zero)
                facingDirection = direction;
        }
        else
        {
            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) direction += new Vector2(0, -1);
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) direction += new Vector2(0, 1);
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) direction += new Vector2(-1, 0);
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) direction += new Vector2(1, 0);
            if (direction != Vector2.Zero) direction = Vector2.Normalize(direction);

            strafeMode = mouseFacingDirection.HasValue || keyboard.IsKeyDown(Keys.Space);
            if (mouseFacingDirection.HasValue)
                facingDirection = mouseFacingDirection.Value;
            else if (!strafeMode && direction != Vector2.Zero)
                facingDirection = direction;
        }
    }

    /// <summary>
    /// Strafe mode: thrust matches the raw input direction directly, no facing lock and no
    /// angle gating — free twin-stick-style movement while aiming. Normal mode: thrust only
    /// fires out of the ship's actual nose, and only within the angle cone around the
    /// requested direction. Applies the resulting force to the body directly.
    /// </summary>
    private static void ResolveThrust(b2BodyId bodyId, ShipMovement movement, Vector2 direction, bool strafeMode,
        out bool thrustFiring, out Vector2 thrustDirection, out float thrustMagnitude, out Vector2 facingVector, out Vector2 rightVector)
    {
        var currentAngle = B2Api.b2Body_GetRotation(bodyId).GetAngle();
        facingVector = new Vector2(MathF.Cos(currentAngle), MathF.Sin(currentAngle));
        rightVector = new Vector2(-facingVector.Y, facingVector.X);

        thrustFiring = false;
        thrustDirection = Vector2.Zero;
        thrustMagnitude = 0f;

        if (direction != Vector2.Zero)
        {
            if (strafeMode)
            {
                thrustDirection = Vector2.Normalize(direction);
                thrustMagnitude = direction.Length();
                thrustFiring = true;
            }
            else
            {
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
    }

    /// <summary>
    /// Decomposes the actual applied thrust onto the ship's own axes for the jet visuals.
    /// Forward-Backward and Left-Right are the (orthogonal, so their squares sum to 1 when
    /// thrust is firing) components of thrustDirection along facing/right. There's no rear
    /// thruster modeled, so a backward component (BackwardComponent) instead lights up BOTH
    /// strafe jets equally, on top of whatever sideways lean they already have from
    /// LeftRightComponent — reversing still reads as the engines firing. Also sets
    /// movement.UseStrafeSpeedCap, and either snaps (controller) or eases (keyboard) the
    /// throttle values toward their targets.
    /// </summary>
    private static void ResolveThrottle(ref ShipMovement movement, ref EngineThrottle throttle, bool useController, float deltaSeconds,
        EngineConfig engineConfig, bool thrustFiring, Vector2 thrustDirection, float thrustMagnitude, Vector2 facingVector, Vector2 rightVector)
    {
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
    }

    private static void UpdateFacing(b2BodyId bodyId, Vector2? facingDirection, float turnSpeedRadiansPerSecond, float deltaSeconds)
    {
        if (facingDirection.HasValue)
        {
            var targetAngle = MathF.Atan2(facingDirection.Value.Y, facingDirection.Value.X);
            TurnTowards(bodyId, targetAngle, turnSpeedRadiansPerSecond, deltaSeconds);
        }
        else
        {
            B2Api.b2Body_SetAngularVelocity(bodyId, 0f);
        }
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
