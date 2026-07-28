using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.Rendering;

/// <summary>Draws every live FloatingText popup at its own stored screen position (already pure screen space — no camera transform involved, see the component's own doc comment), centered horizontally on that point, fading out as FloatingText.RemainingSeconds counts down toward 0 (the actual rise/expiry lives in FloatingTextSystem).</summary>
public static class FloatingTextRenderSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<FloatingText>();

    public static void Run(World world, SpriteBatch spriteBatch, SpriteFont font, float textScale)
    {
        world.Query(in Query, (ref FloatingText floatingText) =>
        {
            var alphaFraction = floatingText.RemainingSeconds / floatingText.TotalSeconds;
            var origin = font.MeasureString(floatingText.Text) / 2f;

            spriteBatch.DrawString(font, floatingText.Text, floatingText.ScreenPositionPixels, floatingText.Color * alphaFraction,
                0f, origin, textScale, SpriteEffects.None, 0f);
        });
    }
}
