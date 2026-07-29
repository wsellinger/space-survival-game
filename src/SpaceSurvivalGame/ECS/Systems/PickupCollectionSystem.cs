using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Shared implementation behind IronPickupSystem/OxygenPickupSystem: collects every TPickupTag
/// pickup the ship has touched this frame (a plain distance check against the ship's Transform —
/// pickups don't opt into Box2D hit events, so CollisionDamageSystem never sees them, this is the
/// only place that detects contact with one), destroying each and playing a particle burst plus
/// one "+N Resource" floating-text popup per pickup collected. onCollected is invoked once with
/// the total collected this frame so the caller can apply its own resource-specific accumulation
/// (Iron has no cap; Oxygen clamps to Max).
/// </summary>
public static class PickupCollectionSystem
{
    private static readonly QueryDescription ShipTransformQuery = new QueryDescription().WithAll<Transform, PlayerControlled>();

    public static void Run<TPickupTag>(World world, ShipConfig shipConfig, int pickupSpriteSizePixels, SparkConfig sparkConfig,
        Texture2D sparkTexture, FloatingTextConfig floatingTextConfig, Camera camera, Random random,
        Microsoft.Xna.Framework.Color burstColorA, Microsoft.Xna.Framework.Color burstColorB,
        string floatingTextLine, Microsoft.Xna.Framework.Color floatingTextColor, Action<int> onCollected)
        where TPickupTag : struct
    {
        var shipPositionMeters = Vector2.Zero;
        var foundShip = false;
        world.Query(in ShipTransformQuery, (ref Transform transform) =>
        {
            shipPositionMeters = transform.PositionMeters;
            foundShip = true;
        });
        if (!foundShip) return;

        var collectDistanceMeters = PhysicsWorld.PixelsToMeters(shipConfig.SpriteSizePixels / 2f) + PhysicsWorld.PixelsToMeters(pickupSpriteSizePixels / 2f);
        var collectDistanceSquared = collectDistanceMeters * collectDistanceMeters;

        var collectedEntities = new List<Entity>();
        var collectedPositions = new List<Vector2>();

        var pickupQuery = new QueryDescription().WithAll<PhysicsBody, Transform, TPickupTag>();
        world.Query(in pickupQuery, (Entity entity, ref PhysicsBody physicsBody, ref Transform transform) =>
        {
            if (Vector2.DistanceSquared(transform.PositionMeters, shipPositionMeters) > collectDistanceSquared) return;

            collectedEntities.Add(entity);
            collectedPositions.Add(transform.PositionMeters);
            B2Api.b2DestroyBody(physicsBody.BodyId);
        });

        if (collectedEntities.Count == 0) return;

        foreach (var entity in collectedEntities) world.Destroy(entity);
        foreach (var position in collectedPositions)
            ParticleEffects.SpawnPickupBurst(world, sparkTexture, position, random, sparkConfig, burstColorA, burstColorB);

        var popupPositionPixels = camera.WorldToScreen(shipPositionMeters) + new Microsoft.Xna.Framework.Vector2(0f, -floatingTextConfig.SpawnHeightAbovePixels);
        for (var i = 0; i < collectedEntities.Count; i++)
            ParticleEffects.SpawnFloatingText(world, popupPositionPixels, floatingTextLine, floatingTextColor, floatingTextConfig);

        onCollected(collectedEntities.Count);
    }
}
