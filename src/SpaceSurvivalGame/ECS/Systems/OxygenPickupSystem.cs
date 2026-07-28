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
/// Collects O2 pickups the ship has touched: a plain distance check against the ship's
/// Transform each frame (pickups don't opt into Box2D hit events, so CollisionDamageSystem
/// never sees them — this is the only place that detects contact with one). On collection,
/// the pickup's body and entity are destroyed, a small particle burst plays at its position,
/// Oxygen.Current gains OxygenPickupConfig.OxygenAmount (clamped to Max), and a "+N Oxygen"
/// popup (see ParticleEffects.SpawnFloatingText) appears above the ship per pickup collected.
/// </summary>
public static class OxygenPickupSystem
{
    private static readonly QueryDescription ShipTransformQuery = new QueryDescription().WithAll<Transform, PlayerControlled>();
    private static readonly QueryDescription ShipOxygenQuery = new QueryDescription().WithAll<Oxygen, PlayerControlled>();
    private static readonly QueryDescription PickupQuery = new QueryDescription().WithAll<PhysicsBody, Transform, OxygenPickup>();

    public static void Run(World world, ShipConfig shipConfig, OxygenPickupConfig pickupConfig, SparkConfig sparkConfig, Texture2D sparkTexture,
        FloatingTextConfig floatingTextConfig, Camera camera, Random random)
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
        var crystalColor = ColorHex.Parse(pickupConfig.ColorHex);
        foreach (var position in collectedPositions)
            ParticleEffects.SpawnPickupBurst(world, sparkTexture, position, random, sparkConfig, crystalColor, Microsoft.Xna.Framework.Color.White);

        var popupPositionPixels = camera.WorldToScreen(shipPositionMeters) + new Microsoft.Xna.Framework.Vector2(0f, -floatingTextConfig.SpawnHeightAbovePixels);
        var floatingTextColor = ColorHex.Parse(pickupConfig.FloatingTextColorHex);
        for (var i = 0; i < collectedEntities.Count; i++)
            ParticleEffects.SpawnFloatingText(world, popupPositionPixels, $"+{(int)pickupConfig.OxygenAmount} Oxygen", floatingTextColor, floatingTextConfig);

        var totalOxygenGained = pickupConfig.OxygenAmount * collectedEntities.Count;
        world.Query(in ShipOxygenQuery, (ref Oxygen oxygen) => oxygen.Current = Math.Min(oxygen.Current + totalOxygenGained, oxygen.Max));
    }
}
