using System;
using Arch.Core;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Pops loose a fresh O2 pickup crystal wherever an oxygen-rich asteroid qualifies on collision — see ResourceReleaseSystem.</summary>
public static class OxygenCrystalReleaseSystem
{
    public static void Run(World world, PhysicsWorld physicsWorld, OxygenPickupField.PickupAssets pickupAssets,
        OxygenPickupConfig pickupConfig, OxygenRichAsteroidConfig oxygenRichConfig, Random random, float deltaSeconds) =>
        ResourceReleaseSystem.Run(world, physicsWorld, AsteroidType.OxygenRich, oxygenRichConfig, random, deltaSeconds,
            point => OxygenPickupField.SpawnPickup(world, physicsWorld, point, pickupAssets, pickupConfig, random));
}
