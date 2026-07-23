using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

using SpaceSurvivalGame.Config;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws the health/O2 bars in screen space (no camera transform), bottom-left of the
/// viewport, health innermost with O2 stacked above it. Each bar is two sprites sharing
/// the same baked-white rounded-rect textures: a fill clipped via sourceRectangle to the
/// resource's remaining percentage (tinted the bar's color), and an outline on top (always
/// full bar length) so the frame reads as constant while only the fill shrinks. Both are
/// drawn around the bar's own center (via origin) so a non-1 scale grows/shrinks it evenly
/// instead of just toward one corner — used by the O2 bar's empty-oxygen pulse.
/// </summary>
public static class HudRenderer
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Health, Oxygen, HealthBarFeedback, PlayerControlled>();

    public static void Run(World world, SpriteBatch spriteBatch, int viewportHeight, HudConfig config, HudFeedbackConfig feedbackConfig,
        OxygenWarningConfig oxygenWarningConfig, float totalGameSeconds, Texture2D fillTexture, Texture2D outlineTexture)
    {
        world.Query(in Query, (ref Health health, ref Oxygen oxygen, ref HealthBarFeedback feedback) =>
        {
            var basePosition = new Vector2(config.MarginPixels, viewportHeight - config.MarginPixels - config.BarThicknessPixels);
            var oxygenPosition = basePosition - new Vector2(0, config.BarThicknessPixels + config.BarSpacingPixels);
            var healthPosition = basePosition + feedback.ShakeOffsetPixels;

            var flashFraction = MathHelper.Clamp(feedback.RemainingSeconds / feedbackConfig.FlashDurationSeconds, 0f, 1f);
            var healthColor = Color.Lerp(Color.Red, Color.White, flashFraction);
            DrawBar(spriteBatch, config, fillTexture, outlineTexture, healthPosition, health.Current / health.Max, healthColor, Color.White, 1f);

            var oxygenFraction = oxygen.Current / oxygen.Max;
            Color oxygenColor;
            Color oxygenOutlineColor;
            var oxygenScale = 1f;

            if (oxygen.Current <= 0f)
            {
                // Smooth pulse white-to-red-to-white, growing/shrinking, at a configurable
                // frequency — an ongoing alarm rather than the discrete flash used while
                // merely low.
                var pulsePhase = MathF.Sin(totalGameSeconds * oxygenWarningConfig.EmptyOxygenPulseFrequencyHz * MathF.PI * 2f);
                var pulseFraction = (pulsePhase + 1f) / 2f;
                oxygenColor = Color.Lerp(Color.White, Color.Red, pulseFraction);
                oxygenOutlineColor = oxygenColor;
                oxygenScale = 1f + oxygenWarningConfig.EmptyOxygenPulseScaleAmount * pulseFraction;
            }
            else if (oxygenFraction <= oxygenWarningConfig.LowOxygenThresholdFraction)
            {
                // flash-off-flash-off-off-off-off: 7 equal beats, a quick double-blink (lit on
                // beats 0 and 2) followed by a longer pause (beats 3-6).
                var beatIndex = (int)(totalGameSeconds / oxygenWarningConfig.LowOxygenFlashBeatSeconds) % 7;
                oxygenColor = beatIndex == 0 || beatIndex == 2 ? Color.White : Color.CornflowerBlue;
                oxygenOutlineColor = Color.White;
            }
            else
            {
                oxygenColor = Color.CornflowerBlue;
                oxygenOutlineColor = Color.White;
            }

            DrawBar(spriteBatch, config, fillTexture, outlineTexture, oxygenPosition, oxygenFraction, oxygenColor, oxygenOutlineColor, oxygenScale);
        });
    }

    private static void DrawBar(SpriteBatch spriteBatch, HudConfig config, Texture2D fillTexture, Texture2D outlineTexture, Vector2 position,
        float fraction, Color fillColor, Color outlineColor, float scale)
    {
        var origin = new Vector2(config.BarLengthPixels / 2f, config.BarThicknessPixels / 2f);
        var center = position + origin;

        var fillWidth = (int)(config.BarLengthPixels * MathHelper.Clamp(fraction, 0f, 1f));
        if (fillWidth > 0)
        {
            var sourceRectangle = new Rectangle(0, 0, fillWidth, config.BarThicknessPixels);
            spriteBatch.Draw(fillTexture, center, sourceRectangle, fillColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        spriteBatch.Draw(outlineTexture, center, null, outlineColor, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}
