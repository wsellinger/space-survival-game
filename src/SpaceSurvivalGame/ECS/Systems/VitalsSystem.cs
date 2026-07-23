using Arch.Core;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Drains oxygen over time at a fixed configured rate. Health isn't touched here — it only changes via CollisionDamageSystem.</summary>
public static class VitalsSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Oxygen>();

    public static void Run(World world, float deltaSeconds, PlayerConfig config)
    {
        world.Query(in Query, (ref Oxygen oxygen) =>
        {
            oxygen.Current = System.Math.Clamp(oxygen.Current - config.OxygenDrainPerSecond * deltaSeconds, 0f, oxygen.Max);
        });
    }
}
