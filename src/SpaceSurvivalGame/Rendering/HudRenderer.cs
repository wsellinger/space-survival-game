using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws the health/O2 bars in screen space (no camera transform), bottom-left of the
/// viewport, health innermost with O2 stacked above it. Each bar is two sprites sharing
/// the same baked-white rounded-rect textures: a fill clipped via sourceRectangle to the
/// resource's remaining percentage (tinted the bar's color), and an unclipped outline on
/// top (always white, always full bar length) so the frame reads as constant while only
/// the fill shrinks.
/// </summary>
public static class HudRenderer
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Health, Oxygen, PlayerControlled>();

    public static void Run(World world, SpriteBatch spriteBatch, int viewportHeight, HudConfig config, Texture2D fillTexture, Texture2D outlineTexture)
    {
        world.Query(in Query, (ref Health health, ref Oxygen oxygen) =>
        {
            var healthPosition = new Vector2(config.MarginPixels, viewportHeight - config.MarginPixels - config.BarThicknessPixels);
            var oxygenPosition = healthPosition - new Vector2(0, config.BarThicknessPixels + config.BarSpacingPixels);

            DrawBar(spriteBatch, config, fillTexture, outlineTexture, healthPosition, health.Current / health.Max, Color.Red);
            DrawBar(spriteBatch, config, fillTexture, outlineTexture, oxygenPosition, oxygen.Current / oxygen.Max, Color.CornflowerBlue);
        });
    }

    private static void DrawBar(SpriteBatch spriteBatch, HudConfig config, Texture2D fillTexture, Texture2D outlineTexture, Vector2 position, float fraction, Color color)
    {
        var fillWidth = (int)(config.BarLengthPixels * MathHelper.Clamp(fraction, 0f, 1f));
        if (fillWidth > 0)
        {
            var sourceRectangle = new Rectangle(0, 0, fillWidth, config.BarThicknessPixels);
            spriteBatch.Draw(fillTexture, position, sourceRectangle, color);
        }

        spriteBatch.Draw(outlineTexture, position, null, Color.White);
    }
}
