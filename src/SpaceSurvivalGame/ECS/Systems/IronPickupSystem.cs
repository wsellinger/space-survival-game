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
/// Collects iron ore pickups the ship has touched: a plain distance check against the ship's
/// Transform each frame (pickups don't opt into Box2D hit events, so CollisionDamageSystem
/// never sees them — this is the only place that detects contact with one). On collection,
/// the pickup's body and entity are destroyed, a small particle burst plays at its position (in
/// the ore's own color rather than the O2 pickups' blue), Iron.Current gains
/// IronPickupConfig.IronAmount — no cap, nothing consumes it yet — and a "+N Iron" popup (see
/// ParticleEffects.SpawnFloatingText) appears above the ship per pickup collected.
/// </summary>
public static class IronPickupSystem
{
    private static readonly QueryDescription ShipTransformQuery = new QueryDescription().WithAll<Transform, PlayerControlled>();
    private static readonly QueryDescription ShipIronQuery = new QueryDescription().WithAll<Iron, PlayerControlled>();
    private static readonly QueryDescription PickupQuery = new QueryDescription().WithAll<PhysicsBody, Transform, IronPickup>();

    private static readonly Microsoft.Xna.Framework.Color BurstAccentColor = new(230, 232, 235); // pale metallic glint, paired with the ore's own configured color

    public static void Run(World world, ShipConfig shipConfig, IronPickupConfig ironConfig, SparkConfig sparkConfig, Texture2D sparkTexture,
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

        var collectDistanceMeters = PhysicsWorld.PixelsToMeters(shipConfig.SpriteSizePixels / 2f) + PhysicsWorld.PixelsToMeters(ironConfig.SpriteSizePixels / 2f);
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
        var oreColor = ColorHex.Parse(ironConfig.ColorHex);
        foreach (var position in collectedPositions)
            ParticleEffects.SpawnPickupBurst(world, sparkTexture, position, random, sparkConfig, oreColor, BurstAccentColor);

        var popupPositionPixels = camera.WorldToScreen(shipPositionMeters) + new Microsoft.Xna.Framework.Vector2(0f, -floatingTextConfig.SpawnHeightAbovePixels);
        var floatingTextColor = ColorHex.Parse(ironConfig.FloatingTextColorHex);
        for (var i = 0; i < collectedEntities.Count; i++)
            ParticleEffects.SpawnFloatingText(world, popupPositionPixels, $"+{(int)ironConfig.IronAmount} Iron", floatingTextColor, floatingTextConfig);

        var totalIronGained = ironConfig.IronAmount * collectedEntities.Count;
        world.Query(in ShipIronQuery, (ref Iron iron) => iron.Current += totalIronGained);
    }
}
