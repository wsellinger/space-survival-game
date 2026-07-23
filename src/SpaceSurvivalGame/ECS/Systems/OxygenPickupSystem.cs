using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Collects O2 pickups the ship has touched: a plain distance check against the ship's
/// Transform each frame (pickups don't opt into Box2D hit events, so CollisionDamageSystem
/// never sees them — this is the only place that detects contact with one). On collection,
/// the pickup's body and entity are destroyed, a small particle burst plays at its position,
/// and Oxygen.Current gains PickupConfig.OxygenAmount (clamped to Max).
/// </summary>
public static class OxygenPickupSystem
{
    private static readonly QueryDescription ShipTransformQuery = new QueryDescription().WithAll<Transform, PlayerControlled>();
    private static readonly QueryDescription ShipOxygenQuery = new QueryDescription().WithAll<Oxygen, PlayerControlled>();
    private static readonly QueryDescription PickupQuery = new QueryDescription().WithAll<PhysicsBody, Transform, OxygenPickup>();

    public static void Run(World world, ShipConfig shipConfig, PickupConfig pickupConfig, ParticleConfig particleConfig, Texture2D sparkTexture, Random random)
    {
        var shipPositionMeters = Vector2.Zero;
        var foundShip = false;
        world.Query(in ShipTransformQuery, (ref Transform transform) =>
        {
            shipPositionMeters = transform.PositionMeters;
            foundShip = true;
        });
        if (!foundShip) return;

        var collectDistanceMeters = PhysicsWorld.PixelsToMeters(shipConfig.SpriteSize / 2f) + PhysicsWorld.PixelsToMeters(pickupConfig.SpriteSizePixels / 2f);
        var collectDistanceSquared = collectDistanceMeters * collectDistanceMeters;

        var collectedEntities = new List<Entity>();
        var collectedPositions = new List<Vector2>();

        world.Query(in PickupQuery, (Entity entity, ref PhysicsBody physicsBody, ref Transform transform) =>
        {
            if (Vector2.DistanceSquared(transform.PositionMeters, shipPositionMeters) > collectDistanceSquared) return;

            collectedEntities.Add(entity);
            collectedPositions.Add(transform.PositionMeters);
            B2Api.b2DestroyBody(physicsBody.BodyId);
        });

        if (collectedEntities.Count == 0) return;

        foreach (var entity in collectedEntities) world.Destroy(entity);
        foreach (var position in collectedPositions) ParticleEffects.SpawnPickupBurst(world, sparkTexture, position, random, particleConfig);

        var totalOxygenGained = pickupConfig.OxygenAmount * collectedEntities.Count;
        world.Query(in ShipOxygenQuery, (ref Oxygen oxygen) => oxygen.Current = Math.Min(oxygen.Current + totalOxygenGained, oxygen.Max));
    }
}
