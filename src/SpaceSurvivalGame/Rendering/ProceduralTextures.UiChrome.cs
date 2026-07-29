using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSurvivalGame.Rendering;

public static partial class ProceduralTextures
{
    /// <summary>A solid size x size square, e.g. for a star dot.</summary>
    public static Texture2D CreateSolidSquare(GraphicsDevice graphicsDevice, int size, Color color)
    {
        var data = new Color[size * size];
        for (var i = 0; i < data.Length; i++) data[i] = color;

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// A solid size x size square (like CreateSolidSquare) overlaid with a handful of
    /// axis-aligned "trace" lines — a circuit-board look. The traces are authored once in a
    /// single quadrant (0..1, 0..1 in unit space, where 1 is the square's own outer edge);
    /// several deliberately run out to that edge so the pattern reads as traces reaching the
    /// border rather than floating in the middle. Every pixel is folded into that same quadrant
    /// via Abs() before testing distance to them, so the result is mirror-symmetric across both
    /// axes for free rather than needing 4 copies of each segment.
    /// </summary>
    public static Texture2D CreateCircuitSquare(GraphicsDevice graphicsDevice, int size, Color baseColor, Color lineColor, float lineThicknessFraction)
    {
        var segments = new (Vector2 Start, Vector2 End)[]
        {
            (new Vector2(0.10f, 0.10f), new Vector2(0.10f, 1.00f)),
            (new Vector2(0.10f, 0.35f), new Vector2(0.40f, 0.35f)),
            (new Vector2(0.40f, 0.10f), new Vector2(0.40f, 0.35f)),
            (new Vector2(0.40f, 0.10f), new Vector2(1.00f, 0.10f)),
            (new Vector2(0.65f, 0.10f), new Vector2(0.65f, 0.55f)),
            (new Vector2(0.65f, 0.55f), new Vector2(1.00f, 0.55f)),
            (new Vector2(0.25f, 0.55f), new Vector2(0.25f, 1.00f)),
            (new Vector2(0.25f, 0.55f), new Vector2(0.55f, 0.55f)),
            (new Vector2(0.80f, 0.20f), new Vector2(0.80f, 0.40f))
        };

        var data = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        var scale = size / 2f;
        var halfThickness = lineThicknessFraction / 2f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var localPoint = (point - center) / scale;
                var quadrantPoint = new Vector2(System.MathF.Abs(localPoint.X), System.MathF.Abs(localPoint.Y));

                var onLine = false;
                foreach (var segment in segments)
                {
                    if (DistanceToSegment(quadrantPoint, segment.Start, segment.End) <= halfThickness)
                    {
                        onLine = true;
                        break;
                    }
                }

                data[y * size + x] = onLine ? lineColor : baseColor;
            }
        }

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
    /// A solid filled circle inscribed in a size x size square (like CreateCircle), but with a
    /// ring band of ringColor from innerRadiusFraction out to the full radius — e.g. the station
    /// core's shiny-red-center/grey-ring look. Hard edge between the two colors (no feathering),
    /// transparent outside the full circle.
    /// </summary>
    public static Texture2D CreateRingedCircle(GraphicsDevice graphicsDevice, int size, Color innerColor, Color ringColor, float innerRadiusFraction)
    {
        var data = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        var radius = size / 2f;
        var innerRadius = radius * innerRadiusFraction;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var distance = Vector2.Distance(point, center);
                Color pixelColor;
                if (distance <= innerRadius) pixelColor = innerColor;
                else if (distance <= radius) pixelColor = ringColor;
                else pixelColor = Color.Transparent;

                data[y * size + x] = pixelColor;
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
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
    /// 4 L-shaped corner brackets (viewfinder/target-reticle style), each arm armLengthPixels
    /// long from its corner, outlineThickness wide, with cornerRadius rounding the bend.
    /// Reuses the rounded-rect outline SDF from CreateRoundedRectOutline, masked down to just
    /// the regions near each of the 4 corners — since a straight edge's outline pixels there
    /// are already just that corner's two arms, no separate line-segment geometry is needed.
    /// </summary>
    public static Texture2D CreateCornerBrackets(GraphicsDevice graphicsDevice, int width, int height, float cornerRadius, float outlineThickness, float armLengthPixels, Color color)
    {
        var data = new Color[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var distance = RoundedRectSignedDistance(point, width, height, cornerRadius);
                var onOutline = distance <= 0f && distance > -outlineThickness;

                var nearLeftOrRight = x <= armLengthPixels || x >= width - armLengthPixels;
                var nearTopOrBottom = y <= armLengthPixels || y >= height - armLengthPixels;

                data[y * width + x] = onOutline && nearLeftOrRight && nearTopOrBottom ? color : Color.Transparent;
            }
        }

        var texture = new Texture2D(graphicsDevice, width, height);
        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// A soft edge vignette: full color right at the width x height rectangle's boundary,
    /// easing out to transparent depthPixels inward. Meant to be shared/tinted at draw time
    /// (e.g. a red or blue full-screen warning).
    /// </summary>
    public static Texture2D CreateEdgeVignette(GraphicsDevice graphicsDevice, int width, int height, float depthPixels, Color color)
    {
        var data = new Color[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var distanceFromEdge = -RoundedRectSignedDistance(point, width, height, 0f); // positive inside; 0 right at the boundary
                var falloff = MathHelper.Clamp(1f - distanceFromEdge / depthPixels, 0f, 1f);
                falloff *= falloff; // eases the fade so it's softer approaching the inner edge
                data[y * width + x] = color * falloff;
            }
        }

        var texture = new Texture2D(graphicsDevice, width, height);
        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// A minimal 4-tick crosshair centered in a size x size canvas: a short tick mark above,
    /// below, left, and right of center, with a gap in the middle so the exact aim point isn't
    /// obscured. Transparent elsewhere.
    /// </summary>
    public static Texture2D CreateCrosshair(GraphicsDevice graphicsDevice, int size, float gapRadius, float tickLength, float thickness, Color color)
    {
        var data = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var offset = new Vector2(x + 0.5f, y + 0.5f) - center;
                var onVerticalTick = System.Math.Abs(offset.X) <= thickness / 2f && System.Math.Abs(offset.Y) >= gapRadius && System.Math.Abs(offset.Y) <= gapRadius + tickLength;
                var onHorizontalTick = System.Math.Abs(offset.Y) <= thickness / 2f && System.Math.Abs(offset.X) >= gapRadius && System.Math.Abs(offset.X) <= gapRadius + tickLength;
                data[y * size + x] = onVerticalTick || onHorizontalTick ? color : Color.Transparent;
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }
}
