using System;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Collects iron ore pickups the ship has touched (see PickupCollectionSystem). On collection,
/// Iron.Current gains IronPickupConfig.IronAmount per pickup — no cap, nothing consumes it yet.
/// </summary>
public static class IronPickupSystem
{
    private static readonly QueryDescription ShipIronQuery = new QueryDescription().WithAll<Iron, PlayerControlled>();

    private static readonly Microsoft.Xna.Framework.Color BurstAccentColor = new(230, 232, 235); // pale metallic glint, paired with the ore's own configured color

    public static void Run(World world, ShipConfig shipConfig, IronPickupConfig ironConfig, SparkConfig sparkConfig, Texture2D sparkTexture,
        FloatingTextConfig floatingTextConfig, Camera camera, Random random)
    {
        var oreColor = ColorHex.Parse(ironConfig.ColorHex);
        var floatingTextColor = ColorHex.Parse(ironConfig.FloatingTextColorHex);

        PickupCollectionSystem.Run<IronPickup>(world, shipConfig, ironConfig.SpriteSizePixels, sparkConfig, sparkTexture, floatingTextConfig, camera, random,
            oreColor, BurstAccentColor, $"+{(int)ironConfig.IronAmount} Iron", floatingTextColor,
            collectedCount =>
            {
                var totalIronGained = ironConfig.IronAmount * collectedCount;
                world.Query(in ShipIronQuery, (ref Iron iron) => iron.Current += totalIronGained);
            });
    }
}
