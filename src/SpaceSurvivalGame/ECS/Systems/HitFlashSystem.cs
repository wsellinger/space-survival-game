using Arch.Core;
using Microsoft.Xna.Framework;
using SpaceSurvivalGame.ECS.Components;

using SpaceSurvivalGame.Config;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Fades the ship's Sprite.Color from red back to white as HitFlash.RemainingSeconds counts down, reset to full by CollisionDamageSystem on each hit.</summary>
public static class HitFlashSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<HitFlash, Sprite, PlayerControlled>();

    public static void Run(World world, float deltaSeconds, HitFlashConfig config)
    {
        world.Query(in Query, (ref HitFlash hitFlash, ref Sprite sprite) =>
        {
            if (hitFlash.RemainingSeconds <= 0f)
            {
                sprite.Color = Color.White;
                return;
            }

            hitFlash.RemainingSeconds = System.Math.Max(0f, hitFlash.RemainingSeconds - deltaSeconds);
            var fraction = MathHelper.Clamp(hitFlash.RemainingSeconds / config.FlashDurationSeconds * config.FlashIntensity, 0f, 1f);
            sprite.Color = Color.Lerp(Color.White, Color.Red, fraction);
        });
    }
}
