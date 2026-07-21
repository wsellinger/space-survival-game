namespace SpaceSurvivalGame.Physics;

/// <summary>
/// Box2DNet works in System.Numerics.Vector2 (the .NET-standard numerics type);
/// MonoGame works in its own Microsoft.Xna.Framework.Vector2. They are unrelated
/// types with no implicit conversion, so every value crossing the physics/render
/// boundary needs one of these.
/// </summary>
public static class VectorConversions
{
    public static Microsoft.Xna.Framework.Vector2 ToXna(this System.Numerics.Vector2 v) => new(v.X, v.Y);

    public static System.Numerics.Vector2 ToNumerics(this Microsoft.Xna.Framework.Vector2 v) => new(v.X, v.Y);
}
