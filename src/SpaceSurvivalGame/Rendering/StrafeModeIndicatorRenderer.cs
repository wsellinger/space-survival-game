using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws a "STRAFE MODE" indicator while the ship is in strafe mode (right stick/RMB held for
/// independent aim, or Space/right trigger held — see ShipMovement.IsStrafing, set by
/// ShipInputSystem): corner brackets baked once at load time (see
/// ProceduralTextures.CreateCornerBrackets), drawn solid the whole time, plus "STRAFE MODE"
/// text near the top-left one, which hard-blinks on/off (no fade) BlinkFrequencyHz times per
/// second.
/// </summary>
public static class StrafeModeIndicatorRenderer
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<ShipMovement, PlayerControlled>();

    public static void Run(World world, SpriteBatch spriteBatch, SpriteFont font, StrafeModeIndicatorConfig config,
        Texture2D bracketsTexture, float totalGameSeconds)
    {
        var isStrafing = false;
        world.Query(in Query, (ref ShipMovement movement) => isStrafing = movement.IsStrafing);
        if (!isStrafing) return;

        var color = ColorHex.Parse(config.ColorHex);
        var inset = new Vector2(config.InsetPixels, config.InsetPixels);
        spriteBatch.Draw(bracketsTexture, inset, color);

        var textVisible = totalGameSeconds * config.BlinkFrequencyHz % 1f < 0.5f;
        if (textVisible)
            spriteBatch.DrawString(font, "STRAFE MODE", inset + new Vector2(config.TextMarginPixels, config.TextMarginPixels), color, 0f, Vector2.Zero, config.TextScale, SpriteEffects.None, 0f);
    }
}
