using System.Collections.Generic;
using Arch.Core;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Ages and rises FloatingText popups in pure screen space (no Transform/Velocity involved — see the component's own doc comment), destroying them once their lifetime expires.</summary>
public static class FloatingTextSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<FloatingText>();

    public static void Run(World world, float deltaSeconds)
    {
        var expired = new List<Entity>();

        world.Query(in Query, (Entity entity, ref FloatingText floatingText) =>
        {
            floatingText.RemainingSeconds -= deltaSeconds;
            if (floatingText.RemainingSeconds <= 0f)
            {
                expired.Add(entity);
                return;
            }

            floatingText.ScreenPositionPixels.Y -= floatingText.RiseSpeedPixelsPerSecond * deltaSeconds;
        });

        foreach (var entity in expired) world.Destroy(entity);
    }
}
