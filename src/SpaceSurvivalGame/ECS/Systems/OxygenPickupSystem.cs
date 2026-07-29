using System;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Collects O2 pickups the ship has touched (see PickupCollectionSystem). On collection,
/// Oxygen.Current gains OxygenPickupConfig.OxygenAmount per pickup, clamped to Max.
/// </summary>
public static class OxygenPickupSystem
{
    private static readonly QueryDescription ShipOxygenQuery = new QueryDescription().WithAll<Oxygen, PlayerControlled>();

    public static void Run(World world, ShipConfig shipConfig, OxygenPickupConfig pickupConfig, SparkConfig sparkConfig, Texture2D sparkTexture,
        FloatingTextConfig floatingTextConfig, Camera camera, Random random)
    {
        var crystalColor = ColorHex.Parse(pickupConfig.ColorHex);
        var floatingTextColor = ColorHex.Parse(pickupConfig.FloatingTextColorHex);

        PickupCollectionSystem.Run<OxygenPickup>(world, shipConfig, pickupConfig.SpriteSizePixels, sparkConfig, sparkTexture, floatingTextConfig, camera, random,
            crystalColor, Microsoft.Xna.Framework.Color.White, $"+{(int)pickupConfig.OxygenAmount} Oxygen", floatingTextColor,
            collectedCount =>
            {
                var totalOxygenGained = pickupConfig.OxygenAmount * collectedCount;
                world.Query(in ShipOxygenQuery, (ref Oxygen oxygen) => oxygen.Current = Math.Min(oxygen.Current + totalOxygenGained, oxygen.Max));
            });
    }
}
