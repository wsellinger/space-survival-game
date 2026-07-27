using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws every MetallicSparkle entity's handful of animated glints on top of its own
/// already-drawn Sprite (see RenderSystem, which must run first in the same
/// SpriteBatch.Begin/End so this layers above it). Each glint's brightness follows its own
/// sharpened sine wave of elapsed time plus that glint's own phase, so it reads as a brief
/// flare rather than a smooth pulse, and neither different glints on the same entity nor
/// different entities in the field flare in unison. Both standalone iron ore pickups
/// (IronPickupField) and iron-rich asteroids' embedded ore speckles (AsteroidField) carry this
/// component, so its tunables (color/size/timing) are read straight from IronPickupConfig
/// rather than a separate config of their own.
/// </summary>
public static class MetallicSparkleRenderSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Transform, MetallicSparkle>();

    // 0 (this codebase's documented frontmost value) is also every gameplay sprite's own
    // default — including the ore chunk's — so drawing glints at 0 too would tie with their own
    // parent under SpriteSortMode.BackToFront, undefined which one wins; losing that tie hides
    // the glint behind the ore's opaque fill everywhere except the sliver of transparent canvas
    // margin right at the silhouette's edge (the "only on the edges" symptom this was chasing).
    // SpriteBatch's LayerDepth contract is documented as [0,1] — pushing this negative to "win"
    // the tie broke sorting outright instead. Keeping the glint at the safe, known-good frontmost
    // value and nudging the ORE's own sprite to IronOreLayerDepth (see IronPickupField) instead
    // stays in-range on both ends.
    private const float SparkleLayerDepth = 0f;

    public static void Run(World world, SpriteBatch spriteBatch, Camera camera, IronPickupConfig ironConfig, float totalGameTimeSeconds)
    {
        var sparkleColor = ColorHex.Parse(ironConfig.SparkleColorHex);
        var frequencyRadiansPerSecond = ironConfig.SparkleFrequencyHz * System.MathF.Tau;

        world.Query(in Query, (ref Transform transform, ref MetallicSparkle sparkle) =>
        {
            var cos = System.MathF.Cos(transform.RotationRadians);
            var sin = System.MathF.Sin(transform.RotationRadians);
            var origin = new Microsoft.Xna.Framework.Vector2(sparkle.Texture.Width / 2f, sparkle.Texture.Height / 2f);
            var basePositionPixels = camera.WorldToScreen(transform.PositionMeters);

            for (var i = 0; i < sparkle.OffsetsPixels.Length; i++)
            {
                var brightness = System.MathF.Max(0f, System.MathF.Sin(totalGameTimeSeconds * frequencyRadiansPerSecond + sparkle.PhasesRadians[i]));
                var alphaFraction = System.MathF.Pow(brightness, ironConfig.SparkleSharpness) * ironConfig.SparkleMaxAlpha;
                if (alphaFraction <= 0.001f) continue; // not worth a draw call this frame

                var offset = sparkle.OffsetsPixels[i];
                var rotatedOffset = new Microsoft.Xna.Framework.Vector2(
                    offset.X * cos - offset.Y * sin,
                    offset.X * sin + offset.Y * cos);

                var positionPixels = basePositionPixels + rotatedOffset * camera.Zoom;
                spriteBatch.Draw(sparkle.Texture, positionPixels, sourceRectangle: null, color: sparkleColor * alphaFraction,
                    rotation: 0f, origin: origin, scale: camera.Zoom, effects: SpriteEffects.None, layerDepth: SparkleLayerDepth);
            }
        });
    }
}
