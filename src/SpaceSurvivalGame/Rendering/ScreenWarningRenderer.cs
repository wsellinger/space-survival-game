using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws a pulsing colored outline + soft vignette around the screen edges: red while health
/// is below its low threshold, cornflower blue while O2 is below its low threshold — the blue
/// keeps pulsing even once O2 reaches exactly 0 (unlike health, which stops mattering at 0
/// since that's already game over). If both are low at once, the two colors alternate rather
/// than blend. Taking damage also pulses red momentarily, reusing HealthBarFeedback's existing
/// hit-flash timer so it decays in lockstep with the health bar's own on-hit flash.
/// </summary>
public static class ScreenWarningRenderer
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Health, Oxygen, HealthBarFeedback, PlayerControlled>();

    public static void Run(World world, SpriteBatch spriteBatch, ScreenWarningConfig config, HealthWarningConfig healthWarningConfig,
        OxygenWarningConfig oxygenWarningConfig, HudFeedbackConfig hudFeedbackConfig, float totalGameSeconds,
        Texture2D outlineTexture, Texture2D vignetteTexture)
    {
        world.Query(in Query, (ref Health health, ref Oxygen oxygen, ref HealthBarFeedback feedback) =>
        {
            var healthLow = health.Current > 0f && health.Current / health.Max <= healthWarningConfig.LowHealthThresholdFraction;
            var oxygenLow = oxygen.Current / oxygen.Max <= oxygenWarningConfig.LowOxygenThresholdFraction;

            var pulsePhase = MathHelper.Clamp((MathF.Sin(totalGameSeconds * config.PulseFrequencyHz * MathF.PI * 2f) + 1f) / 2f, 0f, 1f);

            var redIntensity = 0f;
            var blueIntensity = 0f;

            if (healthLow && oxygenLow)
            {
                var isRedTurn = (int)(totalGameSeconds / config.AlternateSeconds) % 2 == 0;
                if (isRedTurn) redIntensity = pulsePhase; else blueIntensity = pulsePhase;
            }
            else if (healthLow) redIntensity = pulsePhase;
            else if (oxygenLow) blueIntensity = pulsePhase;

            var hitFlashFraction = MathHelper.Clamp(feedback.RemainingSeconds / hudFeedbackConfig.FlashDurationSeconds, 0f, 1f);
            redIntensity = MathF.Max(redIntensity, hitFlashFraction);

            if (redIntensity > 0f) Draw(spriteBatch, outlineTexture, vignetteTexture, Color.Red, redIntensity * config.MaxIntensity);
            if (blueIntensity > 0f) Draw(spriteBatch, outlineTexture, vignetteTexture, Color.CornflowerBlue, blueIntensity * config.MaxIntensity);
        });
    }

    private static void Draw(SpriteBatch spriteBatch, Texture2D outlineTexture, Texture2D vignetteTexture, Color color, float intensity)
    {
        var tint = color * intensity;
        spriteBatch.Draw(vignetteTexture, Vector2.Zero, tint);
        spriteBatch.Draw(outlineTexture, Vector2.Zero, tint);
    }
}
