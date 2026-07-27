using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// Drives a handful of animated glints drawn on top of this entity's own sprite (see
/// MetallicSparkleRenderSystem) — small bright highlights that briefly flare in and out over
/// time as if catching the light, rather than a static glow. OffsetsPixels are fixed points on
/// the entity's own surface, in its own unrotated local space (rolled once at spawn time); the
/// render system rotates each by the entity's current Transform.RotationRadians every frame, so
/// every glint appears to stay fixed to its own facet as the object spins instead of sliding
/// around it. PhasesRadians (same length, index-paired with OffsetsPixels) is each glint's own
/// random offset into the flicker cycle so they don't all flare in unison. Texture is a shared
/// baked-white dot (like ParticleEffects' spark texture), tinted at draw time.
/// </summary>
public struct MetallicSparkle
{
    public Vector2[] OffsetsPixels;
    public float[] PhasesRadians;
    public Texture2D Texture;
}
