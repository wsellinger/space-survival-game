using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws the ship's exhaust flame out the back, world-space (camera-relative, like
/// RenderSystem) so it's subject to the same suffocation post-process. Two layers share the
/// same baked-white triangle texture, tinted independently at draw time (outer silhouette,
/// inner core drawn on top) — both anchored at the ship's tail, rotated to point opposite its
/// facing, and scaled (length and width independently) with EngineThrottle.Current (0 =
/// invisible), with a size/brightness flicker layered on top of both.
/// </summary>
public static class EngineJetRenderer
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Transform, EngineThrottle, PlayerControlled>();

    public static void Run(World world, SpriteBatch spriteBatch, Camera camera, EngineConfig config, int shipSpriteSizePixels, Texture2D flameTexture, float totalGameSeconds)
    {
        // A sum of two out-of-sync sine waves reads as an irregular flicker without needing
        // any per-frame random state; applied to both size and brightness together.
        var flicker = 1f + config.FlickerIntensity * (
            MathF.Sin(totalGameSeconds * config.FlickerSpeedHz * MathF.PI * 2f) * 0.6f +
            MathF.Sin(totalGameSeconds * config.FlickerSpeedHz * 2.7f * MathF.PI * 2f) * 0.4f);

        var outerColor = new Color(config.ColorR, config.ColorG, config.ColorB);
        var innerColor = new Color(config.InnerColorR, config.InnerColorG, config.InnerColorB);

        world.Query(in Query, (ref Transform transform, ref EngineThrottle throttle) =>
        {
            if (throttle.Current <= 0.01f) return;

            var forward = new System.Numerics.Vector2(MathF.Cos(transform.RotationRadians), MathF.Sin(transform.RotationRadians));
            var tailPositionMeters = transform.PositionMeters - forward * PhysicsWorld.PixelsToMeters(shipSpriteSizePixels / 2f);
            var screenPosition = camera.WorldToScreen(tailPositionMeters);
            var rotation = transform.RotationRadians + MathF.PI;
            var origin = new Vector2(0f, config.FlameTextureSizePixels / 2f); // base (wide end), so scaling stretches the tip away from the ship
            var alpha = MathHelper.Clamp(throttle.Current * flicker, 0f, 1f);

            DrawLayer(spriteBatch, flameTexture, screenPosition, rotation, origin, config.FlameTextureSizePixels,
                config.MinFlameLengthPixels, config.MaxFlameLengthPixels, config.MinFlameWidthPixels, config.MaxFlameWidthPixels,
                throttle.Current, flicker, outerColor * alpha, layerDepth: 0.11f);

            DrawLayer(spriteBatch, flameTexture, screenPosition, rotation, origin, config.FlameTextureSizePixels,
                config.MinInnerFlameLengthPixels, config.MaxInnerFlameLengthPixels, config.MinInnerFlameWidthPixels, config.MaxInnerFlameWidthPixels,
                throttle.Current, flicker, innerColor * alpha, layerDepth: 0.1f); // smaller layerDepth so it draws on top of the outer layer, both still behind the ship's own sprite (layerDepth 0)
        });
    }

    private static void DrawLayer(SpriteBatch spriteBatch, Texture2D flameTexture, Vector2 screenPosition, float rotation, Vector2 origin,
        int flameTextureSizePixels, float minLengthPixels, float maxLengthPixels, float minWidthPixels, float maxWidthPixels,
        float throttle, float flicker, Color color, float layerDepth)
    {
        var lengthPixels = MathHelper.Lerp(minLengthPixels, maxLengthPixels, throttle) * flicker;
        var widthPixels = MathHelper.Lerp(minWidthPixels, maxWidthPixels, throttle) * flicker;
        var scale = new Vector2(lengthPixels, widthPixels) / flameTextureSizePixels; // X = length (tip direction), Y = width

        spriteBatch.Draw(flameTexture, screenPosition, null, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
    }
}
