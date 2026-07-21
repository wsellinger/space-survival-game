using System.Numerics;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// Position and rotation in physics world units (meters), mirrored from Box2D
/// each frame by PhysicsSyncSystem. Systems that don't need to touch physics
/// directly (rendering, a future camera) read this instead of the Box2D body.
/// </summary>
public struct Transform
{
    public Vector2 PositionMeters;
    public float RotationRadians;
}
