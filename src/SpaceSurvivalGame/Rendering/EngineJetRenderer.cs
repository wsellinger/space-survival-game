using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using NVector2 = System.Numerics.Vector2;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws the ship's exhaust flames, world-space (camera-relative, like RenderSystem) so
/// they're subject to the same suffocation post-process. The main jet mounts at the hull's
/// back notch (see ShipConfig.NotchDepthFraction / ProceduralTextures.CreateConcaveArrowShip)
/// and scales with EngineThrottle.Current; two smaller strafe jets are flush-mounted on the
/// ship's own side edges (nose to wing corner) and fire tangent to that edge, mirrored to
/// point forward-and-outward — like an RCS thruster venting away from the hull rather than
/// along it — scaling with EngineThrottle.LeftStrafe/RightStrafe (see EngineThrottle for when
/// each lights up). All three share the same baked-white triangle texture and two-layer (outer
/// silhouette + inner core) treatment, tinted at draw time, with a size/brightness flicker
/// layered on top.
/// </summary>
public static class EngineJetRenderer
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Transform, EngineThrottle, PlayerControlled>();

    public static void Run(World world, SpriteBatch spriteBatch, Camera camera, EngineConfig config, int shipSpriteSizePixels, float notchDepthFraction, Texture2D flameTexture, float totalGameSeconds)
    {
        // A sum of two out-of-sync sine waves reads as an irregular flicker without needing
        // any per-frame random state; applied to both size and brightness together.
        var flicker = 1f + config.FlickerIntensity * (
            MathF.Sin(totalGameSeconds * config.FlickerSpeedHz * MathF.PI * 2f) * 0.6f +
            MathF.Sin(totalGameSeconds * config.FlickerSpeedHz * 2.7f * MathF.PI * 2f) * 0.4f);

        var outerColor = ColorHex.Parse(config.ColorHex);
        var innerColor = ColorHex.Parse(config.InnerColorHex);

        // Hull geometry in local ship space (+X forward, +Y right), matching ShipEntity's
        // physics triangle and CreateConcaveArrowShip's texture points, relative to the
        // sprite's own center (the rotation origin).
        var halfSize = shipSpriteSizePixels / 2f;
        var noseLocal = new NVector2(halfSize - 1f, 0f);
        var rightCornerLocal = new NVector2(-halfSize, halfSize - 1f);
        var leftCornerLocal = new NVector2(-halfSize, -halfSize);
        var notchLocal = new NVector2(MathHelper.Lerp(-halfSize, halfSize - 1f, notchDepthFraction), 0f);

        // Each side edge (wing corner to nose) naturally points forward-and-inward; mirroring
        // its lateral component gives forward-and-outward, i.e. a thruster flush-mounted on
        // that edge but venting away from the hull instead of along it.
        var rightEdge = noseLocal - rightCornerLocal;
        var rightMountLocal = rightCornerLocal + rightEdge * config.StrafeJetEdgeMountFraction;
        var rightExhaustLocal = NVector2.Normalize(new NVector2(rightEdge.X, -rightEdge.Y));

        var leftEdge = noseLocal - leftCornerLocal;
        var leftMountLocal = leftCornerLocal + leftEdge * config.StrafeJetEdgeMountFraction;
        var leftExhaustLocal = NVector2.Normalize(new NVector2(leftEdge.X, -leftEdge.Y));

        world.Query(in Query, (ref Transform transform, ref EngineThrottle throttle) =>
        {
            var positionMeters = transform.PositionMeters;
            var forward = new NVector2(MathF.Cos(transform.RotationRadians), MathF.Sin(transform.RotationRadians));
            var right = new NVector2(-forward.Y, forward.X);

            NVector2 ToWorldPosition(NVector2 local) => positionMeters + PhysicsWorld.PixelsToMeters(forward * local.X + right * local.Y);
            NVector2 ToWorldDirection(NVector2 local) => forward * local.X + right * local.Y;

            if (throttle.Current > 0.01f)
            {
                var tailPositionMeters = ToWorldPosition(notchLocal);
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
            }

            DrawStrafeJet(spriteBatch, flameTexture, camera, config, flicker, outerColor, innerColor,
                ToWorldPosition(leftMountLocal), ToWorldDirection(leftExhaustLocal), throttle.LeftStrafe);

            DrawStrafeJet(spriteBatch, flameTexture, camera, config, flicker, outerColor, innerColor,
                ToWorldPosition(rightMountLocal), ToWorldDirection(rightExhaustLocal), throttle.RightStrafe);
        });
    }

    private static void DrawStrafeJet(SpriteBatch spriteBatch, Texture2D flameTexture, Camera camera, EngineConfig config,
        float flicker, Color outerColor, Color innerColor, NVector2 mountPositionMeters, NVector2 exhaustDirection, float magnitude)
    {
        if (magnitude <= 0.01f) return;

        var screenPosition = camera.WorldToScreen(mountPositionMeters);
        var rotation = MathF.Atan2(exhaustDirection.Y, exhaustDirection.X);
        var origin = new Vector2(0f, config.FlameTextureSizePixels / 2f);
        var alpha = MathHelper.Clamp(magnitude * flicker, 0f, 1f);

        DrawLayer(spriteBatch, flameTexture, screenPosition, rotation, origin, config.FlameTextureSizePixels,
            config.MinStrafeFlameLengthPixels, config.MaxStrafeFlameLengthPixels, config.MinStrafeFlameWidthPixels, config.MaxStrafeFlameWidthPixels,
            magnitude, flicker, outerColor * alpha, layerDepth: 0.11f);

        DrawLayer(spriteBatch, flameTexture, screenPosition, rotation, origin, config.FlameTextureSizePixels,
            config.MinInnerStrafeFlameLengthPixels, config.MaxInnerStrafeFlameLengthPixels, config.MinInnerStrafeFlameWidthPixels, config.MaxInnerStrafeFlameWidthPixels,
            magnitude, flicker, innerColor * alpha, layerDepth: 0.1f);
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
