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

    private float _shakeMagnitudePixels;
    private Microsoft.Xna.Framework.Vector2 _shakeOffsetPixels;
    private readonly Random _shakeRandom = new();

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

    /// <summary>Bumps the current shake magnitude up to at least magnitudePixels — repeated hits refresh rather than stack, so rapid impacts don't compound into an absurd shake.</summary>
    public void AddShake(float magnitudePixels)
    {
        _shakeMagnitudePixels = MathF.Max(_shakeMagnitudePixels, magnitudePixels);
    }

    /// <summary>Decays the shake magnitude and rolls a fresh random jitter offset for this frame. Call once per frame regardless of whether a hit occurred.</summary>
    public void UpdateShake(float deltaSeconds, float decaySpeed)
    {
        _shakeMagnitudePixels *= MathF.Exp(-decaySpeed * deltaSeconds);
        if (_shakeMagnitudePixels < 0.05f)
        {
            _shakeMagnitudePixels = 0f;
            _shakeOffsetPixels = Microsoft.Xna.Framework.Vector2.Zero;
            return;
        }

        var angle = (float)(_shakeRandom.NextDouble() * Math.PI * 2);
        _shakeOffsetPixels = new Microsoft.Xna.Framework.Vector2(MathF.Cos(angle), MathF.Sin(angle)) * _shakeMagnitudePixels;
    }

    /// <summary>
    /// parallax scales how much camera movement affects the result: 1 (default) is normal;
    /// less than 1 moves less than the camera, reading as farther away (distant background
    /// layers); greater than 1 moves more, reading as closer than the camera's own plane.
    /// </summary>
    public Microsoft.Xna.Framework.Vector2 WorldToScreen(Vector2 positionMeters, float parallax = 1f)
    {
        var offsetPixels = PhysicsWorld.MetersToPixels((positionMeters - PositionMeters) * parallax).ToXna();
        return new Microsoft.Xna.Framework.Vector2(ViewportWidth / 2f, ViewportHeight / 2f) + offsetPixels + _shakeOffsetPixels;
    }
}
