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
/// timer. Reaches a settled, non-spinning square once eased progress hits 1 (Build.SpinRevolutions
/// is normally an integer, so that also lands the reveal's own rotation back at a multiple of a
/// full turn) — from then on transform.RotationRadians (0 during the reveal itself, since the
/// core has no physics body yet — see StationCoreSystem.CreatePhysicsBody) takes over, so the
/// square keeps turning in place afterward as the landed core's own drift rotates its body.
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
            var eased = StationCoreSystem.EaseInOut(progress, config.Flight.EaseInExponent, config.Flight.EaseOutExponent);
            if (eased <= 0f) return; // not worth a draw call at zero size

            var rotation = eased * config.Build.SpinRevolutions * MathHelper.TwoPi + transform.RotationRadians;
            var positionPixels = camera.WorldToScreen(transform.PositionMeters);

            spriteBatch.Draw(squareTexture, positionPixels, sourceRectangle: null, color: Color.White,
                rotation: rotation, origin: origin, scale: eased * camera.Zoom, effects: SpriteEffects.None, layerDepth: BuildEffectLayerDepth);
        });
    }
}
