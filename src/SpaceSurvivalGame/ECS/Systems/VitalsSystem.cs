using Arch.Core;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Drains oxygen over time at a fixed configured rate. Health isn't touched here — it only
/// changes via CollisionDamageSystem. Also tracks how long Oxygen has been at 0 via
/// Suffocation.ElapsedSeconds (reset instantly once it rises above 0), which MainGame.Draw
/// reads to drive the suffocation post-process effect.
/// </summary>
public static class VitalsSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Oxygen, Suffocation>();

    public static void Run(World world, float deltaSeconds, PlayerConfig config, SuffocationEffectConfig suffocationConfig)
    {
        world.Query(in Query, (ref Oxygen oxygen, ref Suffocation suffocation) =>
        {
            oxygen.Current = System.Math.Clamp(oxygen.Current - config.OxygenDrainPerSecond * deltaSeconds, 0f, oxygen.Max);

            suffocation.ElapsedSeconds = oxygen.Current <= 0f
                ? System.Math.Min(suffocation.ElapsedSeconds + deltaSeconds, suffocationConfig.EffectDurationSeconds)
                : 0f;
        });
    }
}
