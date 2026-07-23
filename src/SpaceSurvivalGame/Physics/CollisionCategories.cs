namespace SpaceSurvivalGame.Physics;

/// <summary>
/// Box2D collision filter category bits. Everything defaults to Box2D's own category (bit 0)
/// and a mask that collides with everything — only shapes that need to selectively skip
/// collision with one specific other category (without becoming a full sensor, which would
/// skip collision with everyone) need to touch filter.categoryBits/maskBits at all.
/// </summary>
public static class CollisionCategories
{
    public const ulong Ship = 1UL << 1;
}
