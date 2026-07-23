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

    /// <summary>A solid size x size square, e.g. for a star dot.</summary>
    public static Texture2D CreateSolidSquare(GraphicsDevice graphicsDevice, int size, Color color)
    {
        var data = new Color[size * size];
        for (var i = 0; i < data.Length; i++) data[i] = color;

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    /// <summary>A solid filled circle inscribed in a size x size square, transparent elsewhere. Meant to be shared across many entities of varying size via Sprite.Scale rather than regenerated per size.</summary>
    public static Texture2D CreateCircle(GraphicsDevice graphicsDevice, int size, Color color)
    {
        var data = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        var radius = size / 2f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                data[y * size + x] = Vector2.Distance(point, center) <= radius ? color : Color.Transparent;
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// Fills a size x size square with the polygon described by unitVertices — points
    /// given in a -1..1 local space around the texture's center, e.g. for an irregular
    /// rock shape. Vertices must be in angular order around the center (a "star-shaped"
    /// polygon relative to it) so the shape is simple even if concave; this method doesn't
    /// itself require convexity.
    /// </summary>
    public static Texture2D CreatePolygon(GraphicsDevice graphicsDevice, int size, Color color, Vector2[] unitVertices)
    {
        var data = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        var scale = size / 2f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var localPoint = (point - center) / scale;
                data[y * size + x] = IsInsidePolygon(localPoint, unitVertices) ? color : Color.Transparent;
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// Same fill as CreatePolygon, plus a soft radial glow filling the area between the
    /// polygon's edge and glowRadius (in the same -1..1 unit space), fading out via an
    /// eased falloff so it reads as a gentle halo rather than a hard-edged ring.
    /// </summary>
    public static Texture2D CreateGlowingPolygon(GraphicsDevice graphicsDevice, int size, Color polygonColor, Color glowColor, Vector2[] unitVertices, float glowRadius)
    {
        var data = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        var scale = size / 2f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var localPoint = (point - center) / scale;

                if (IsInsidePolygon(localPoint, unitVertices))
                {
                    data[y * size + x] = polygonColor;
                    continue;
                }

                var distance = localPoint.Length();
                if (distance <= glowRadius)
                {
                    var falloff = 1f - distance / glowRadius;
                    falloff *= falloff; // eases the fade so it's softer near the outer edge of the glow
                    data[y * size + x] = glowColor * falloff;
                }
                else
                {
                    data[y * size + x] = Color.Transparent;
                }
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
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

    /// <summary>A solid width x height rounded rectangle (a full pill/capsule when radius = height/2), transparent elsewhere. Meant to be shared/tinted at draw time (e.g. HUD bars of different colors).</summary>
    public static Texture2D CreateRoundedRect(GraphicsDevice graphicsDevice, int width, int height, float cornerRadius, Color color)
    {
        var data = new Color[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                data[y * width + x] = RoundedRectSignedDistance(point, width, height, cornerRadius) <= 0f ? color : Color.Transparent;
            }
        }

        var texture = new Texture2D(graphicsDevice, width, height);
        texture.SetData(data);
        return texture;
    }

    /// <summary>The stroke-only outline of a rounded rectangle (see CreateRoundedRect), outlineThickness pixels wide, hugging the inside of the boundary.</summary>
    public static Texture2D CreateRoundedRectOutline(GraphicsDevice graphicsDevice, int width, int height, float cornerRadius, float outlineThickness, Color color)
    {
        var data = new Color[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var distance = RoundedRectSignedDistance(point, width, height, cornerRadius);
                data[y * width + x] = distance <= 0f && distance > -outlineThickness ? color : Color.Transparent;
            }
        }

        var texture = new Texture2D(graphicsDevice, width, height);
        texture.SetData(data);
        return texture;
    }

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
