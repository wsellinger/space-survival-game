using System;
using Arch.Core;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Pops loose a fresh iron ore pickup wherever an iron-rich asteroid qualifies on collision — see ResourceReleaseSystem.</summary>
public static class IronOreReleaseSystem
{
    public static void Run(World world, PhysicsWorld physicsWorld, IronPickupField.PickupAssets ironAssets,
        IronPickupConfig ironConfig, IronRichAsteroidConfig ironRichConfig, Random random, float deltaSeconds) =>
        ResourceReleaseSystem.Run(world, physicsWorld, AsteroidType.IronRich, ironRichConfig, random, deltaSeconds,
            point => IronPickupField.SpawnPickup(world, physicsWorld, point, ironAssets, ironConfig, random));
}
