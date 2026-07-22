using System;
using System.Numerics;
using SpaceSurvivalGame.Physics;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Tracks a world position (meters) and converts other world positions into screen
/// pixels centered on it — i.e. whatever PositionMeters is set to always draws at
/// the middle of the viewport. PositionMeters is the actual (smoothed) position used
/// for rendering; TargetPositionMeters is where it's easing toward.
/// </summary>
public class Camera
{
    public Vector2 PositionMeters;
    public Vector2 TargetPositionMeters;
    public int ViewportWidth;
    public int ViewportHeight;

    /// <summary>
    /// Eases PositionMeters toward TargetPositionMeters, framerate-independent — higher
    /// smoothingSpeed catches up faster. smoothingSpeed &lt;= 0 disables easing entirely
    /// and snaps straight to the target.
    /// </summary>
    public void MoveTowardTarget(float deltaSeconds, float smoothingSpeed)
    {
        if (smoothingSpeed <= 0f)
        {
            PositionMeters = TargetPositionMeters;
            return;
        }

        var t = 1f - MathF.Exp(-smoothingSpeed * deltaSeconds);
        PositionMeters = Vector2.Lerp(PositionMeters, TargetPositionMeters, t);
    }

    /// <summary>
    /// parallax scales how much camera movement affects the result: 1 (default) is normal;
    /// less than 1 moves less than the camera, reading as farther away (distant background
    /// layers); greater than 1 moves more, reading as closer than the camera's own plane.
    /// </summary>
    public Microsoft.Xna.Framework.Vector2 WorldToScreen(Vector2 positionMeters, float parallax = 1f)
    {
        var offsetPixels = PhysicsWorld.MetersToPixels((positionMeters - PositionMeters) * parallax).ToXna();
        return new Microsoft.Xna.Framework.Vector2(ViewportWidth / 2f, ViewportHeight / 2f) + offsetPixels;
    }
}
