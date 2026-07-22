using System.Numerics;
using SpaceSurvivalGame.Physics;

namespace SpaceSurvivalGame.Rendering;

/// <summary>Tracks a world position (meters) and converts other world positions into screen pixels centered on it — i.e. whatever PositionMeters is set to always draws at the middle of the viewport.</summary>
public class Camera
{
    public Vector2 PositionMeters;
    public int ViewportWidth;
    public int ViewportHeight;

    public Microsoft.Xna.Framework.Vector2 WorldToScreen(Vector2 positionMeters)
    {
        var offsetPixels = PhysicsWorld.MetersToPixels(positionMeters - PositionMeters).ToXna();
        return new Microsoft.Xna.Framework.Vector2(ViewportWidth / 2f, ViewportHeight / 2f) + offsetPixels;
    }
}
