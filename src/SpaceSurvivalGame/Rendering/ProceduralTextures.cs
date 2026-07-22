using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Generates placeholder textures so we have something to render before any real
/// art exists. Everything here gets replaced once the content pipeline is loading
/// actual sprites.
/// </summary>
public static class ProceduralTextures
{
    /// <summary>
    /// A triangle pointing along +X (right), filling a size x size square, transparent
    /// elsewhere. Pointing right matches angle=0 in our convention (forward = (cos, sin)),
    /// so drawing it with rotation = the body's angle needs no extra offset. A line of
    /// accentColor from the tip to the sprite's center (the rotation origin) marks the
    /// front at a glance.
    /// </summary>
    public static Texture2D CreateRightFacingTriangle(GraphicsDevice graphicsDevice, int size, Color color, Color accentColor)
    {
        var data = new Color[size * size];
        var tip = new Vector2(size - 1, size / 2f);
        var tailTop = new Vector2(0, 0);
        var tailBottom = new Vector2(0, size - 1);
        var center = new Vector2(size / 2f, size / 2f);
        var lineThickness = MathHelper.Max(1f, size * 0.06f);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                if (!IsInsideTriangle(point, tip, tailTop, tailBottom))
                {
                    data[y * size + x] = Color.Transparent;
                    continue;
                }

                data[y * size + x] = DistanceToSegment(point, tip, center) <= lineThickness ? accentColor : color;
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    private static bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
            (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);

        var d1 = Sign(p, a, b);
        var d2 = Sign(p, b, c);
        var d3 = Sign(p, c, a);

        var hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        var hasPositive = d1 > 0 || d2 > 0 || d3 > 0;

        return !(hasNegative && hasPositive);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        var segment = segmentEnd - segmentStart;
        var t = MathHelper.Clamp(Vector2.Dot(point - segmentStart, segment) / segment.LengthSquared(), 0f, 1f);
        var closest = segmentStart + segment * t;
        return Vector2.Distance(point, closest);
    }
}
