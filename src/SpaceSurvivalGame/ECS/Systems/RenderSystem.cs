using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS.Systems;

public static class RenderSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Transform, Sprite>();

    public static void Run(World world, SpriteBatch spriteBatch, Camera camera)
    {
        world.Query(in Query, (ref Transform transform, ref Sprite sprite) =>
        {
            var positionPixels = camera.WorldToScreen(transform.PositionMeters);
            var origin = new Microsoft.Xna.Framework.Vector2(sprite.Size / 2f, sprite.Size / 2f);

            spriteBatch.Draw(
                sprite.Texture,
                positionPixels,
                sourceRectangle: null,
                color: sprite.Color,
                rotation: transform.RotationRadians,
                origin: origin,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: 0f);
        });
    }
}
