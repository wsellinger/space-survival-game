using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Decays the health bar's shake magnitude and rolls a fresh jitter offset each frame; the flash fade itself is just RemainingSeconds counting down, read directly by HudRenderer.</summary>
public static class HudFeedbackSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<HealthBarFeedback, PlayerControlled>();

    public static void Run(World world, float deltaSeconds, HudFeedbackConfig config, Random random)
    {
        world.Query(in Query, (ref HealthBarFeedback feedback) =>
        {
            feedback.RemainingSeconds = Math.Max(0f, feedback.RemainingSeconds - deltaSeconds);
            feedback.ShakeMagnitudePixels *= MathF.Exp(-config.ShakeDecaySpeed * deltaSeconds);

            if (feedback.ShakeMagnitudePixels < 0.05f)
            {
                feedback.ShakeMagnitudePixels = 0f;
                feedback.ShakeOffsetPixels = Vector2.Zero;
                return;
            }

            var angle = (float)(random.NextDouble() * Math.PI * 2);
            feedback.ShakeOffsetPixels = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * feedback.ShakeMagnitudePixels;
        });
    }
}
