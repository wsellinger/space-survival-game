using System;
using System.Collections.Generic;
using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Reads this frame's Box2D hit events (same source CollisionDamageSystem reads — asteroid
/// shapes now opt into hit events too, see AsteroidField.Create, so this sees any collision an
/// oxygen-rich asteroid is part of, not just ones involving the ship) and, for every hit event
/// touching an oxygen-rich asteroid that isn't still on cooldown from a previous spawn (see
/// Asteroid.CrystalReleaseCooldownSecondsRemaining), rolls
/// OxygenRichAsteroidConfig.CrystalReleaseChanceOnCollision for a chance to pop loose a fresh O2
/// pickup crystal at the impact point. Must run after PhysicsWorld.Step and before the next Step
/// call overwrites the event buffer.
/// </summary>
public static class OxygenCrystalReleaseSystem
{
    private static readonly QueryDescription OxygenRichAsteroidQuery = new QueryDescription().WithAll<PhysicsBody, Asteroid>();

    public static void Run(World world, PhysicsWorld physicsWorld, OxygenPickupField.PickupAssets pickupAssets,
        PickupConfig pickupConfig, OxygenRichAsteroidConfig oxygenRichConfig, Random random, float deltaSeconds)
    {
        var oxygenRichEntities = new Dictionary<(int, ushort, ushort), Entity>();
        world.Query(in OxygenRichAsteroidQuery, (Entity entity, ref PhysicsBody physicsBody, ref Asteroid asteroid) =>
        {
            if (asteroid.Type != AsteroidType.OxygenRich) return;
            if (asteroid.CrystalReleaseCooldownSecondsRemaining > 0f)
                asteroid.CrystalReleaseCooldownSecondsRemaining = MathF.Max(0f, asteroid.CrystalReleaseCooldownSecondsRemaining - deltaSeconds);
            oxygenRichEntities[BodyIdKey(physicsBody.BodyId)] = entity;
        });
        if (oxygenRichEntities.Count == 0) return;

        var contactEvents = B2Api.b2World_GetContactEvents(physicsWorld.WorldId);

        foreach (var hitEvent in contactEvents.hitEventsAsSpan)
        {
            var bodyA = B2Api.b2Shape_GetBody(hitEvent.shapeIdA);
            var bodyB = B2Api.b2Shape_GetBody(hitEvent.shapeIdB);

            if (!oxygenRichEntities.TryGetValue(BodyIdKey(bodyA), out var qualifyingEntity) &&
                !oxygenRichEntities.TryGetValue(BodyIdKey(bodyB), out qualifyingEntity))
                continue; // neither side is an oxygen-rich asteroid

            ref var asteroid = ref world.Get<Asteroid>(qualifyingEntity);
            if (asteroid.CrystalReleaseCooldownSecondsRemaining > 0f) continue; // still cooling down from a very recent spawn

            if (random.NextDouble() >= oxygenRichConfig.CrystalReleaseChanceOnCollision) continue;

            asteroid.CrystalReleaseCooldownSecondsRemaining = oxygenRichConfig.CrystalReleaseCooldownSeconds;
            OxygenPickupField.SpawnPickup(world, physicsWorld, hitEvent.point, pickupAssets, pickupConfig, random);
        }
    }

    private static (int, ushort, ushort) BodyIdKey(b2BodyId bodyId) => (bodyId.index1, bodyId.world0, bodyId.generation);
}
