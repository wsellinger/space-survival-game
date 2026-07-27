using System;
using System.Collections.Generic;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Reads Box2D hit events generated during the last PhysicsWorld.Step call and applies
/// damage to the ship's Health when one of them involves the ship's body AND the other body
/// has the Damaging tag (e.g. asteroids) — since only the ship's shape has enableHitEvents set
/// (see ShipEntity.Create), every hit event involving the ship fires regardless of what it hit,
/// so the Damaging check is what keeps harmless things like O2 pickups from ever dealing
/// damage. Damage is a linear map from the event's approach speed to HP, clamped at both ends
/// by PlayerConfig.Collision's SpeedMetersPerSecondRange and DamageRange. Each qualifying hit
/// also spawns a spark burst at the impact point; total damage this frame also triggers the
/// ship's hit-flash and a screen shake scaled by how much of DamageRange.Max it represents.
/// Must run after PhysicsWorld.Step (hit events are only populated post-step) and before the
/// next Step call overwrites them. Skips everything (damage, sparks, shake, flash) entirely
/// while the ship's own Invulnerability.RemainingSeconds is still positive — see
/// InvulnerabilitySystem and whatever set it (e.g. StationCoreSystem's arrival shockwave).
/// </summary>
public static class CollisionDamageSystem
{
    private static readonly QueryDescription ShipQuery =
        new QueryDescription().WithAll<PhysicsBody, PlayerControlled, Health, HitFlash, Invulnerability, HealthBarFeedback>();

    private static readonly QueryDescription DamagingBodyQuery =
        new QueryDescription().WithAll<PhysicsBody, Damaging>();

    public static void Run(World world, PhysicsWorld physicsWorld, PlayerConfig config, Texture2D sparkTexture, Random random,
        SparkConfig sparkConfig, Camera camera, ScreenShakeConfig screenShakeConfig, HitFlashConfig hitFlashConfig, HudFeedbackConfig hudFeedbackConfig)
    {
        var shipBodyId = default(b2BodyId);
        var foundShip = false;
        var isInvulnerable = false;
        world.Query(in ShipQuery, (ref PhysicsBody physicsBody, ref Invulnerability invulnerability) =>
        {
            shipBodyId = physicsBody.BodyId;
            isInvulnerable = invulnerability.RemainingSeconds > 0f;
            foundShip = true;
        });
        if (!foundShip || isInvulnerable) return;

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

            var speedFraction = (hitEvent.approachSpeed - config.Collision.SpeedMetersPerSecondRange.Min) /
                                 (config.Collision.SpeedMetersPerSecondRange.Max - config.Collision.SpeedMetersPerSecondRange.Min);
            speedFraction = System.Math.Clamp(speedFraction, 0f, 1f);
            totalDamage += config.Collision.DamageRange.Min + speedFraction * (config.Collision.DamageRange.Max - config.Collision.DamageRange.Min);

            ParticleEffects.SpawnSparkBurst(world, sparkTexture, hitEvent.point, random, sparkConfig);
        }

        if (totalDamage <= 0f) return;

        var damageFraction = System.Math.Clamp(totalDamage / config.Collision.DamageRange.Max, 0f, 1f);
        var shakeMagnitude = screenShakeConfig.MagnitudePixelsRange.Min +
                              damageFraction * (screenShakeConfig.MagnitudePixelsRange.Max - screenShakeConfig.MagnitudePixelsRange.Min);
        camera.AddShake(shakeMagnitude);

        var hudShakeMagnitude = hudFeedbackConfig.ShakeMagnitudePixelsRange.Min +
                                 damageFraction * (hudFeedbackConfig.ShakeMagnitudePixelsRange.Max - hudFeedbackConfig.ShakeMagnitudePixelsRange.Min);

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
