using System;
using System.Collections.Generic;
using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Reads this frame's Box2D hit events (same source CollisionDamageSystem/OxygenCrystalReleaseSystem
/// read — asteroid shapes opt into hit events, see AsteroidField.Create, so this sees any collision
/// an anorthite-rich asteroid is part of, not just ones involving the ship) and, for every hit
/// event touching an anorthite-rich asteroid that isn't still on cooldown from a previous spawn
/// (see Asteroid.CrystalReleaseCooldownSecondsRemaining), rolls
/// AnorthiteRichAsteroidConfig.CrystalReleaseChanceOnCollision for a chance to pop loose a fresh
/// anorthite pickup at the impact point. Must run after PhysicsWorld.Step and before the next
/// Step call overwrites the event buffer.
/// </summary>
public static class AnorthiteCrystalReleaseSystem
{
    private static readonly QueryDescription AnorthiteRichAsteroidQuery = new QueryDescription().WithAll<PhysicsBody, Asteroid>();

    public static void Run(World world, PhysicsWorld physicsWorld, AnorthiteField.AnorthiteAssets anorthiteAssets,
        AnorthitePickupConfig anorthiteConfig, AnorthiteRichAsteroidConfig anorthiteRichConfig, Random random, float deltaSeconds)
    {
        var anorthiteRichEntities = new Dictionary<(int, ushort, ushort), Entity>();
        world.Query(in AnorthiteRichAsteroidQuery, (Entity entity, ref PhysicsBody physicsBody, ref Asteroid asteroid) =>
        {
            if (asteroid.Type != AsteroidType.AnorthiteRich) return;
            if (asteroid.CrystalReleaseCooldownSecondsRemaining > 0f)
                asteroid.CrystalReleaseCooldownSecondsRemaining = MathF.Max(0f, asteroid.CrystalReleaseCooldownSecondsRemaining - deltaSeconds);
            anorthiteRichEntities[BodyIdKey(physicsBody.BodyId)] = entity;
        });
        if (anorthiteRichEntities.Count == 0) return;

        var contactEvents = B2Api.b2World_GetContactEvents(physicsWorld.WorldId);

        foreach (var hitEvent in contactEvents.hitEventsAsSpan)
        {
            var bodyA = B2Api.b2Shape_GetBody(hitEvent.shapeIdA);
            var bodyB = B2Api.b2Shape_GetBody(hitEvent.shapeIdB);

            if (!anorthiteRichEntities.TryGetValue(BodyIdKey(bodyA), out var qualifyingEntity) &&
                !anorthiteRichEntities.TryGetValue(BodyIdKey(bodyB), out qualifyingEntity))
                continue; // neither side is an anorthite-rich asteroid

            ref var asteroid = ref world.Get<Asteroid>(qualifyingEntity);
            if (asteroid.CrystalReleaseCooldownSecondsRemaining > 0f) continue; // still cooling down from a very recent spawn

            if (random.NextDouble() >= anorthiteRichConfig.CrystalReleaseChanceOnCollision) continue;

            asteroid.CrystalReleaseCooldownSecondsRemaining = anorthiteRichConfig.CrystalReleaseCooldownSeconds;
            AnorthiteField.SpawnPickup(world, physicsWorld, hitEvent.point, anorthiteAssets, anorthiteConfig, random);
        }
    }

    private static (int, ushort, ushort) BodyIdKey(b2BodyId bodyId) => (bodyId.index1, bodyId.world0, bodyId.generation);
}
