using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws the station core's one-time arrival shockwave: a barely-visible ring expanding from
/// nothing out to Shockwave.RadiusMeters over Shockwave.DurationSeconds while fading out, tracked
/// by StationCore.ShockwaveElapsedSeconds (started by StationCoreSystem the instant the core
/// arrives; -1 means it hasn't fired yet). ringTexture is pre-baked at exactly
/// Shockwave.RadiusMeters*2 (in pixels) so scale 1 is the ring's true final size — the ring only
/// ever shrinks from that baked resolution rather than upscaling a smaller texture, so it stays
/// crisp all the way out to full size.
/// </summary>
public static class StationCoreShockwaveRenderSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Transform, StationCore>();

    public static void Run(World world, SpriteBatch spriteBatch, Camera camera, StationCoreConfig config, Texture2D ringTexture, Color color)
    {
        var origin = new Vector2(ringTexture.Width / 2f, ringTexture.Height / 2f);

        world.Query(in Query, (ref Transform transform, ref StationCore core) =>
        {
            if (core.ShockwaveElapsedSeconds < 0f || core.ShockwaveElapsedSeconds >= config.Shockwave.DurationSeconds) return;

            var progress = core.ShockwaveElapsedSeconds / config.Shockwave.DurationSeconds;
            var alphaFraction = (1f - progress) * config.Shockwave.MaxAlpha;
            var positionPixels = camera.WorldToScreen(transform.PositionMeters);

            spriteBatch.Draw(ringTexture, positionPixels, sourceRectangle: null, color: color * alphaFraction,
                rotation: 0f, origin: origin, scale: progress * camera.Zoom, effects: SpriteEffects.None, layerDepth: 0f);
        });
    }
}
