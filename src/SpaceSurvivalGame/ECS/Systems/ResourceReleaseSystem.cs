using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Shared implementation behind IronOreReleaseSystem/OxygenCrystalReleaseSystem: reads this
/// frame's Box2D hit events (same source CollisionDamageSystem reads — asteroid shapes opt into
/// hit events, see AsteroidField.Create, so this sees any collision a rich asteroid of the given
/// type is part of, not just ones involving the ship) and, for every hit event touching one that
/// isn't still on cooldown from a previous spawn (Asteroid.CrystalReleaseCooldownSecondsRemaining),
/// rolls richConfig.CrystalReleaseChanceOnCollision for a chance to pop loose a fresh pickup at
/// the impact point via spawnPickup. Must run after PhysicsWorld.Step and before the next Step
/// call overwrites the event buffer.
/// </summary>
public static class ResourceReleaseSystem
{
    private static readonly QueryDescription RichAsteroidQuery = new QueryDescription().WithAll<PhysicsBody, Asteroid>();

    public static void Run(World world, PhysicsWorld physicsWorld, AsteroidType asteroidType, RichAsteroidConfig richConfig,
        Random random, float deltaSeconds, Action<Vector2> spawnPickup)
    {
        var qualifyingEntities = new Dictionary<(int, ushort, ushort), Entity>();
        world.Query(in RichAsteroidQuery, (Entity entity, ref PhysicsBody physicsBody, ref Asteroid asteroid) =>
        {
            if (asteroid.Type != asteroidType) return;
            if (asteroid.CrystalReleaseCooldownSecondsRemaining > 0f)
                asteroid.CrystalReleaseCooldownSecondsRemaining = MathF.Max(0f, asteroid.CrystalReleaseCooldownSecondsRemaining - deltaSeconds);
            qualifyingEntities[BodyIdKey(physicsBody.BodyId)] = entity;
        });
        if (qualifyingEntities.Count == 0) return;

        var contactEvents = B2Api.b2World_GetContactEvents(physicsWorld.WorldId);

        foreach (var hitEvent in contactEvents.hitEventsAsSpan)
        {
            var bodyA = B2Api.b2Shape_GetBody(hitEvent.shapeIdA);
            var bodyB = B2Api.b2Shape_GetBody(hitEvent.shapeIdB);

            if (!qualifyingEntities.TryGetValue(BodyIdKey(bodyA), out var qualifyingEntity) &&
                !qualifyingEntities.TryGetValue(BodyIdKey(bodyB), out qualifyingEntity))
                continue; // neither side is a rich asteroid of the given type

            ref var asteroid = ref world.Get<Asteroid>(qualifyingEntity);
            if (asteroid.CrystalReleaseCooldownSecondsRemaining > 0f) continue; // still cooling down from a very recent spawn

            if (random.NextDouble() >= richConfig.CrystalReleaseChanceOnCollision) continue;

            asteroid.CrystalReleaseCooldownSecondsRemaining = richConfig.CrystalReleaseCooldownSeconds;
            spawnPickup(hitEvent.point);
        }
    }

    private static (int, ushort, ushort) BodyIdKey(b2BodyId bodyId) => (bodyId.index1, bodyId.world0, bodyId.generation);
}
