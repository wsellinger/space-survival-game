using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// A short-lived "+N Resource" pickup popup (see ParticleEffects.SpawnFloatingText). Rises and
/// fades in pure screen space — deliberately NOT tied to Transform/world coordinates, so it keeps
/// moving straight up on screen at a constant pixel rate regardless of the camera panning or
/// zooming while it's alive, rather than drifting/skewing along with a world-space position that
/// then gets reprojected every frame.
/// </summary>
public struct FloatingText
{
    public string Text;
    public Color Color;
    public Vector2 ScreenPositionPixels;
    public float RiseSpeedPixelsPerSecond;
    public float RemainingSeconds;
    public float TotalSeconds;
}
