using System;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS;

/// <summary>Creates the player ship entity and handles the one-off respawn action.</summary>
public static class ShipEntity
{
    public static Entity Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 startPositionMeters, ShipConfig config, PlayerConfig playerConfig,
        int coreSocketDiameterPixels)
    {
        // No damping: momentum carries the ship, like real space.
        var bodyId = PhysicsBodyFactory.CreateDynamicBody(physicsWorld, startPositionMeters, rotationRadians: 0f,
            linearVelocityMetersPerSecond: Vector2.Zero, angularVelocityRadiansPerSecond: 0f);

        // Matches ProceduralTextures.CreateRightFacingTriangle's vertex layout (tip at
        // (size-1, size/2), tail corners at (0,0)/(0,size-1)) relative to the sprite's
        // center — same center RenderSystem uses as the rotation origin — so the collider
        // actually matches the visible triangle instead of a bounding box around it.
        var halfSize = config.SpriteSize / 2f;
        var trianglePointsMeters = new[]
        {
            PhysicsWorld.PixelsToMeters(new Vector2(halfSize - 1f, 0f)),
            PhysicsWorld.PixelsToMeters(new Vector2(-halfSize, -halfSize)),
            PhysicsWorld.PixelsToMeters(new Vector2(-halfSize, halfSize - 1f))
        };
        // enableHitEvents: only one shape in a collision needs this set for CollisionDamageSystem to see it.
        // categoryBits: lets OxygenPickupField/IronPickupField exclude just the ship from their collision masks.
        PhysicsBodyFactory.CreateHullPolygonShape(bodyId, trianglePointsMeters, density: 1f, restitution: 0f,
            enableHitEvents: true, categoryBits: CollisionCategories.Ship);

        // Visual only — the physics collider above stays the simpler flat-back triangle.
        var texture = ProceduralTextures.CreateConcaveArrowShip(graphicsDevice, config.SpriteSize, config.NotchDepthFraction,
            ColorHex.Parse(config.ColorHex), ColorHex.Parse(config.AccentColorHex), coreSocketDiameterPixels, ColorHex.Parse(config.SocketColorHex));

        return world.Create(
            new PhysicsBody { BodyId = bodyId },
            new Transform { PositionMeters = startPositionMeters, RotationRadians = 0f },
            new Velocity(),
            // Nudged slightly behind the documented frontmost value (0) so the station core —
            // which rides at the ship's own center at the default 0 while still Attached (see
            // StationCoreSystem) — reliably draws on top of the ship instead of an undefined
            // same-depth tie under SpriteSortMode.BackToFront.
            new Sprite { Texture = texture, Color = Microsoft.Xna.Framework.Color.White, Size = config.SpriteSize, Scale = 1f, LayerDepth = 0.01f, Parallax = 1f },
            new ShipMovement
            {
                ThrustAcceleration = config.Thrust.Acceleration,
                MaxSpeedMetersPerSecond = config.MaxSpeedMetersPerSecond,
                StrafeMaxSpeedMetersPerSecond = config.Strafe.MaxSpeedMetersPerSecond,
                StrafeSpeedCapAngleThresholdRadians = config.Strafe.SpeedCapAngleThresholdDegrees * MathF.PI / 180f,
                TurnSpeedRadiansPerSecond = config.TurnSpeedRadiansPerSecond,
                ThrustAngleThresholdRadians = config.Thrust.AngleThresholdDegrees * MathF.PI / 180f
            },
            new Health { Current = playerConfig.MaxHealth, Max = playerConfig.MaxHealth },
            new Oxygen { Current = playerConfig.MaxOxygen, Max = playerConfig.MaxOxygen },
            new Iron { Current = 0f },
            new HitFlash { RemainingSeconds = 0f },
            new Invulnerability { RemainingSeconds = 0f },
            new HealthBarFeedback(),
            new Suffocation { ElapsedSeconds = 0f },
            new Damaging(),
            new EngineThrottle { Current = 0f, LeftStrafe = 0f, RightStrafe = 0f },
            new PlayerControlled());
    }

    private static readonly QueryDescription RespawnQuery =
        new QueryDescription().WithAll<PhysicsBody, PlayerControlled, Health, Oxygen, Iron, HitFlash, Invulnerability, HealthBarFeedback, Suffocation, Sprite, EngineThrottle, ShipMovement>();

    public static void Respawn(World world, Vector2 positionMeters)
    {
        world.Query(in RespawnQuery, (ref PhysicsBody physicsBody, ref Health health, ref Oxygen oxygen, ref Iron iron, ref HitFlash hitFlash,
            ref Invulnerability invulnerability, ref HealthBarFeedback healthBarFeedback, ref Suffocation suffocation, ref Sprite sprite, ref EngineThrottle throttle, ref ShipMovement movement) =>
        {
            var bodyId = physicsBody.BodyId;
            B2Api.b2Body_SetTransform(bodyId, positionMeters, b2Rot.FromAngle(0f));
            B2Api.b2Body_SetLinearVelocity(bodyId, Vector2.Zero);
            B2Api.b2Body_SetAngularVelocity(bodyId, 0f);
            health.Current = health.Max;
            oxygen.Current = oxygen.Max;
            iron.Current = 0f; // cargo resets with the rest of the ship's state on death, like Health/Oxygen
            hitFlash.RemainingSeconds = 0f;
            invulnerability.RemainingSeconds = 0f;
            healthBarFeedback = new HealthBarFeedback();
            suffocation.ElapsedSeconds = 0f;
            sprite.Color = Microsoft.Xna.Framework.Color.White; // undo the hide-on-death from the collision death sequence
            throttle.Current = 0f;
            throttle.LeftStrafe = 0f;
            throttle.RightStrafe = 0f;
            movement.IsStrafing = false;
        });
    }

    private static readonly QueryDescription HideQuery = new QueryDescription().WithAll<PlayerControlled, Sprite, EngineThrottle, ShipMovement>();

    /// <summary>Hides the ship's own sprite (in favor of ShipFragments' debris) and kills its engine throttle so EngineJetRenderer stops drawing a flame with no ship attached to it, for the collision death sequence.</summary>
    public static void Hide(World world)
    {
        world.Query(in HideQuery, (ref Sprite sprite, ref EngineThrottle throttle, ref ShipMovement movement) =>
        {
            sprite.Color = Microsoft.Xna.Framework.Color.Transparent;
            throttle.Current = 0f;
            throttle.LeftStrafe = 0f;
            throttle.RightStrafe = 0f;
            movement.IsStrafing = false;
        });
    }
}
