using System;
using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Detects the two ways the player dies (lethal collision, suffocation timeout) and performs
/// the death-trigger side effects. Doesn't touch GameState itself — callers decide what
/// transition a triggered death means for the outer state machine.
/// </summary>
public static class PlayerDeathSystem
{
    private static readonly QueryDescription HealthQuery = new QueryDescription().WithAll<Health, PlayerControlled>();
    private static readonly QueryDescription PlayerPhysicsBodyQuery = new QueryDescription().WithAll<PhysicsBody, PlayerControlled>();
    private static readonly QueryDescription SuffocationQuery = new QueryDescription().WithAll<Suffocation>();

    /// <summary>Fires the explosion/debris/hide sequence and returns true if the ship's Health just hit 0.</summary>
    public static bool TryTriggerCollisionDeath(World world, GameAssets assets, DeathSequenceConfig deathConfig, Random random)
    {
        var shipHealth = float.MaxValue;
        world.Query(in HealthQuery, (ref Health health) => shipHealth = health.Current);
        if (shipHealth > 0f || !CameraFollowSystem.TryGetShipPositionMeters(world, out var deathPositionMeters))
            return false;

        for (var i = 0; i < deathConfig.Explosion.BurstCount; i++)
            ParticleEffects.SpawnExplosionBurst(world, assets.Spark, deathPositionMeters, random, deathConfig);

        // Read the Box2D body directly rather than the ECS Velocity component — PhysicsSyncSystem
        // (which mirrors Box2D into Velocity) hasn't run yet this frame, so Velocity would still
        // be last frame's value; the body itself already reflects the collision this Step() just
        // resolved, so fragments fly off the way the ship itself actually bounced.
        var shipVelocity = System.Numerics.Vector2.Zero;
        world.Query(in PlayerPhysicsBodyQuery, (ref PhysicsBody physicsBody) => shipVelocity = B2Api.b2Body_GetLinearVelocity(physicsBody.BodyId));
        ShipFragments.SpawnDebris(world, assets.ShipFragmentTextures, deathPositionMeters, shipVelocity, random, deathConfig);
        ShipEntity.Hide(world);
        StationCoreEntity.Hide(world); // no-op if it already detached and became its own object

        return true;
    }

    public static float GetSuffocationElapsedSeconds(World world)
    {
        var suffocationElapsedSeconds = 0f;
        world.Query(in SuffocationQuery, (ref Suffocation suffocation) => suffocationElapsedSeconds = suffocation.ElapsedSeconds);
        return suffocationElapsedSeconds;
    }

    /// <summary>Zeroes Health and returns true once the suffocation post-process effect has fully played out. No explosion/fade here — the screen's already fully black from the vignette by this point.</summary>
    public static bool TryTriggerSuffocationDeath(World world, SuffocationEffectConfig suffocationConfig)
    {
        if (GetSuffocationElapsedSeconds(world) < suffocationConfig.EffectDurationSeconds)
            return false;

        world.Query(in HealthQuery, (ref Health health) => health.Current = 0f);
        return true;
    }
}
