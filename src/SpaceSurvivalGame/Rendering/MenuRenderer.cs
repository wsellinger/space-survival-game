using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws a full-screen dark overlay with a centered title and a single button, used for both
/// the start screen and the game-over screen. The button's fill/outline textures are baked at
/// exactly Bounds' size by the caller so this draws them 1:1 with no stretching. Click/hover
/// detection against UiButton.Bounds is handled by the caller (MainGame) — this only draws.
/// </summary>
public static class MenuRenderer
{
    private const float TitleScale = 2f;
    private const float TitleGapPixels = 30f;

    private static readonly Color OverlayColor = new(0, 0, 0, 180);
    private static readonly Color ButtonColor = new(60, 60, 70);
    private static readonly Color ButtonHoverColor = new(90, 90, 105);

    public static void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D solidPixelTexture, Texture2D buttonFillTexture, Texture2D buttonOutlineTexture,
        int viewportWidth, int viewportHeight, string title, UiButton button, bool isButtonHovered)
    {
        spriteBatch.Draw(solidPixelTexture, new Rectangle(0, 0, viewportWidth, viewportHeight), OverlayColor);

        var titleSize = font.MeasureString(title) * TitleScale;
        var titlePosition = new Vector2((viewportWidth - titleSize.X) / 2f, button.Bounds.Top - titleSize.Y - TitleGapPixels);
        spriteBatch.DrawString(font, title, titlePosition, Color.White, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

        spriteBatch.Draw(buttonFillTexture, button.Bounds, isButtonHovered ? ButtonHoverColor : ButtonColor);
        spriteBatch.Draw(buttonOutlineTexture, button.Bounds, Color.White);

        var labelSize = font.MeasureString(button.Label);
        var labelPosition = new Vector2(button.Bounds.Center.X - labelSize.X / 2f, button.Bounds.Center.Y - labelSize.Y / 2f);
        spriteBatch.DrawString(font, button.Label, labelPosition, Color.White);
    }
}
