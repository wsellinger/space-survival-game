using System;
using System.Collections.Generic;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Reads Box2D hit events generated during the last PhysicsWorld.Step call and applies
/// damage to the ship's Health when one of them involves the ship's body AND the other body
/// has the Damaging tag (e.g. asteroids) — since only the ship's shape has enableHitEvents set
/// (see ShipEntity.Create), every hit event involving the ship fires regardless of what it hit,
/// so the Damaging check is what keeps harmless things like O2 pickups from ever dealing
/// damage. Damage is a linear map from the event's approach speed to HP, clamped at both ends
/// by PlayerConfig's Min/MaxCollisionSpeedMetersPerSecond and Min/MaxCollisionDamage. Each
/// qualifying hit also spawns a spark burst at the impact point; total damage this frame also
/// triggers the ship's hit-flash and a screen shake scaled by how much of MaxCollisionDamage it
/// represents. Must run after PhysicsWorld.Step (hit events are only populated post-step) and
/// before the next Step call overwrites them.
/// </summary>
public static class CollisionDamageSystem
{
    private static readonly QueryDescription ShipQuery =
        new QueryDescription().WithAll<PhysicsBody, PlayerControlled, Health, HitFlash, HealthBarFeedback>();

    private static readonly QueryDescription DamagingBodyQuery =
        new QueryDescription().WithAll<PhysicsBody, Damaging>();

    public static void Run(World world, PhysicsWorld physicsWorld, PlayerConfig config, Texture2D sparkTexture, Random random,
        ParticleConfig particleConfig, Camera camera, ScreenShakeConfig screenShakeConfig, HitFlashConfig hitFlashConfig, HudFeedbackConfig hudFeedbackConfig)
    {
        var shipBodyId = default(b2BodyId);
        var foundShip = false;
        world.Query(in ShipQuery, (ref PhysicsBody physicsBody) =>
        {
            shipBodyId = physicsBody.BodyId;
            foundShip = true;
        });
        if (!foundShip) return;

        var damagingBodyIds = new HashSet<(int, ushort, ushort)>();
        world.Query(in DamagingBodyQuery, (ref PhysicsBody physicsBody) =>
            damagingBodyIds.Add(BodyIdKey(physicsBody.BodyId)));

        var contactEvents = B2Api.b2World_GetContactEvents(physicsWorld.WorldId);
        var totalDamage = 0f;

        foreach (var hitEvent in contactEvents.hitEventsAsSpan)
        {
            var bodyA = B2Api.b2Shape_GetBody(hitEvent.shapeIdA);
            var bodyB = B2Api.b2Shape_GetBody(hitEvent.shapeIdB);

            b2BodyId otherBody;
            if (BodyIdEquals(bodyA, shipBodyId)) otherBody = bodyB;
            else if (BodyIdEquals(bodyB, shipBodyId)) otherBody = bodyA;
            else continue; // doesn't involve the ship at all

            if (!damagingBodyIds.Contains(BodyIdKey(otherBody))) continue; // e.g. an O2 pickup — never deals damage

            var speedFraction = (hitEvent.approachSpeed - config.MinCollisionSpeedMetersPerSecond) /
                                 (config.MaxCollisionSpeedMetersPerSecond - config.MinCollisionSpeedMetersPerSecond);
            speedFraction = System.Math.Clamp(speedFraction, 0f, 1f);
            totalDamage += config.MinCollisionDamage + speedFraction * (config.MaxCollisionDamage - config.MinCollisionDamage);

            ParticleEffects.SpawnSparkBurst(world, sparkTexture, hitEvent.point, random, particleConfig);
        }

        if (totalDamage <= 0f) return;

        var damageFraction = System.Math.Clamp(totalDamage / config.MaxCollisionDamage, 0f, 1f);
        var shakeMagnitude = screenShakeConfig.MinShakeMagnitudePixels +
                              damageFraction * (screenShakeConfig.MaxShakeMagnitudePixels - screenShakeConfig.MinShakeMagnitudePixels);
        camera.AddShake(shakeMagnitude);

        var hudShakeMagnitude = hudFeedbackConfig.MinShakeMagnitudePixels +
                                 damageFraction * (hudFeedbackConfig.MaxShakeMagnitudePixels - hudFeedbackConfig.MinShakeMagnitudePixels);

        world.Query(in ShipQuery, (ref Health health, ref HitFlash hitFlash, ref HealthBarFeedback healthBarFeedback) =>
        {
            health.Current = System.Math.Clamp(health.Current - totalDamage, 0f, health.Max);
            hitFlash.RemainingSeconds = hitFlashConfig.FlashDurationSeconds;
            healthBarFeedback.RemainingSeconds = hudFeedbackConfig.FlashDurationSeconds;
            healthBarFeedback.ShakeMagnitudePixels = MathF.Max(healthBarFeedback.ShakeMagnitudePixels, hudShakeMagnitude);
        });
    }

    private static bool BodyIdEquals(b2BodyId a, b2BodyId b) =>
        a.index1 == b.index1 && a.world0 == b.world0 && a.generation == b.generation;

    private static (int, ushort, ushort) BodyIdKey(b2BodyId bodyId) => (bodyId.index1, bodyId.world0, bodyId.generation);
}
