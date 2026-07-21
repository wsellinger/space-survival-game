using System.Numerics;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Mirrored from Box2D each frame, same reasoning as Transform.</summary>
public struct Velocity
{
    public Vector2 LinearMetersPerSecond;
    public float AngularRadiansPerSecond;
}
