using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Generates placeholder textures so we have something to render before any real
/// art exists. Everything here gets replaced once the content pipeline is loading
/// actual sprites. Split across several files by subject (Ship/Polygons/UiChrome) — this
/// file holds the shared low-level geometry helpers used across more than one of them.
/// </summary>
public static partial class ProceduralTextures
{
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

    private static bool IsInsidePolygon(Vector2 point, Vector2[] vertices)
    {
        var inside = false;
        for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
        {
            var a = vertices[i];
            var b = vertices[j];
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Blends baseColor's RGB toward blendColor's RGB by blendFraction, keeping baseColor's own
    /// alpha untouched — used wherever a translucent patch (rust spots) is composited CPU-side
    /// onto an already-opaque fill, where blendColor's own alpha channel is repurposed as the
    /// blend strength rather than baked into the output pixel's transparency.
    /// </summary>
    private static Color BlendRgb(Color baseColor, Color blendColor, float blendFraction) =>
        new(
            (byte)(baseColor.R * (1f - blendFraction) + blendColor.R * blendFraction),
            (byte)(baseColor.G * (1f - blendFraction) + blendColor.G * blendFraction),
            (byte)(baseColor.B * (1f - blendFraction) + blendColor.B * blendFraction),
            baseColor.A);

    /// <summary>
    /// Signed distance from point to the boundary of a width x height rounded rect centered
    /// in that box (negative = inside, positive = outside) — the standard rounded-box SDF:
    /// shrink the box by cornerRadius, measure distance to that inner rect (clamped to 0 when
    /// inside it), then subtract cornerRadius back out.
    /// </summary>
    private static float RoundedRectSignedDistance(Vector2 point, int width, int height, float cornerRadius)
    {
        var center = new Vector2(width / 2f, height / 2f);
        var halfSize = new Vector2(width / 2f - cornerRadius, height / 2f - cornerRadius);
        var offset = point - center;
        var q = new Vector2(System.Math.Abs(offset.X) - halfSize.X, System.Math.Abs(offset.Y) - halfSize.Y);
        var outsideDistance = new Vector2(MathHelper.Max(q.X, 0f), MathHelper.Max(q.Y, 0f)).Length();
        var insideDistance = MathHelper.Min(MathHelper.Max(q.X, q.Y), 0f);
        return outsideDistance + insideDistance - cornerRadius;
    }
}
