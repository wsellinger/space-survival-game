using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.ECS.Systems;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Draws a circuit-board-patterned square (see ProceduralTextures.CreateCircuitSquare — its two
/// colors are baked in, not tinted here) growing from nothing to full size and spinning to a stop
/// behind the station core's own dot (BuildEffectLayerDepth sorts behind the core's
/// default-frontmost sprite) — starts the instant the core detaches from the ship and finishes
/// exactly when it arrives at its target, sharing StationCoreSystem's own flight progress and
/// easing so the reveal is tied directly to the core's movement rather than running on its own
/// timer. Stays fully grown (a settled, non-spinning square — BuildEffectSpinRevolutions is
/// normally an integer, so the eased progress reaching 1 also lands the rotation back at a
/// multiple of a full turn) forever after arrival, as a permanent part of the built station's
/// look.
/// </summary>
public static class StationCoreBuildEffectRenderSystem
{
    private const float BuildEffectLayerDepth = 0.01f;

    private static readonly QueryDescription Query = new QueryDescription().WithAll<Transform, StationCore>();

    public static void Run(World world, SpriteBatch spriteBatch, Camera camera, StationCoreConfig config, Texture2D squareTexture)
    {
        var origin = new Vector2(squareTexture.Width / 2f, squareTexture.Height / 2f);

        world.Query(in Query, (ref Transform transform, ref StationCore core) =>
        {
            if (core.Attached) return; // hasn't detached yet — no reveal to show

            var progress = StationCoreSystem.GetFlightProgress(in core);
            var eased = StationCoreSystem.EaseInOut(progress, config.FlightEaseInExponent, config.FlightEaseOutExponent);
            if (eased <= 0f) return; // not worth a draw call at zero size

            var rotation = eased * config.BuildEffectSpinRevolutions * MathHelper.TwoPi;
            var positionPixels = camera.WorldToScreen(transform.PositionMeters);

            spriteBatch.Draw(squareTexture, positionPixels, sourceRectangle: null, color: Color.White,
                rotation: rotation, origin: origin, scale: eased * camera.Zoom, effects: SpriteEffects.None, layerDepth: BuildEffectLayerDepth);
        });
    }
}
