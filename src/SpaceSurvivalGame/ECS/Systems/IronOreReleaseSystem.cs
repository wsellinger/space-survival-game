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
/// an iron-rich asteroid is part of, not just ones involving the ship) and, for every hit event
/// touching an iron-rich asteroid that isn't still on cooldown from a previous spawn (see
/// Asteroid.CrystalReleaseCooldownSecondsRemaining), rolls
/// IronRichAsteroidConfig.CrystalReleaseChanceOnCollision for a chance to pop loose a fresh iron
/// ore pickup at the impact point. Must run after PhysicsWorld.Step and before the next Step call
/// overwrites the event buffer.
/// </summary>
public static class IronOreReleaseSystem
{
    private static readonly QueryDescription IronRichAsteroidQuery = new QueryDescription().WithAll<PhysicsBody, Asteroid>();

    public static void Run(World world, PhysicsWorld physicsWorld, IronPickupField.PickupAssets ironAssets,
        IronPickupConfig ironConfig, IronRichAsteroidConfig ironRichConfig, Random random, float deltaSeconds)
    {
        var ironRichEntities = new Dictionary<(int, ushort, ushort), Entity>();
        world.Query(in IronRichAsteroidQuery, (Entity entity, ref PhysicsBody physicsBody, ref Asteroid asteroid) =>
        {
            if (asteroid.Type != AsteroidType.IronRich) return;
            if (asteroid.CrystalReleaseCooldownSecondsRemaining > 0f)
                asteroid.CrystalReleaseCooldownSecondsRemaining = MathF.Max(0f, asteroid.CrystalReleaseCooldownSecondsRemaining - deltaSeconds);
            ironRichEntities[BodyIdKey(physicsBody.BodyId)] = entity;
        });
        if (ironRichEntities.Count == 0) return;

        var contactEvents = B2Api.b2World_GetContactEvents(physicsWorld.WorldId);

        foreach (var hitEvent in contactEvents.hitEventsAsSpan)
        {
            var bodyA = B2Api.b2Shape_GetBody(hitEvent.shapeIdA);
            var bodyB = B2Api.b2Shape_GetBody(hitEvent.shapeIdB);

            if (!ironRichEntities.TryGetValue(BodyIdKey(bodyA), out var qualifyingEntity) &&
                !ironRichEntities.TryGetValue(BodyIdKey(bodyB), out qualifyingEntity))
                continue; // neither side is an iron-rich asteroid

            ref var asteroid = ref world.Get<Asteroid>(qualifyingEntity);
            if (asteroid.CrystalReleaseCooldownSecondsRemaining > 0f) continue; // still cooling down from a very recent spawn

            if (random.NextDouble() >= ironRichConfig.CrystalReleaseChanceOnCollision) continue;

            asteroid.CrystalReleaseCooldownSecondsRemaining = ironRichConfig.CrystalReleaseCooldownSeconds;
            IronPickupField.SpawnPickup(world, physicsWorld, hitEvent.point, ironAssets, ironConfig, random);
        }
    }

    private static (int, ushort, ushort) BodyIdKey(b2BodyId bodyId) => (bodyId.index1, bodyId.world0, bodyId.generation);
}
