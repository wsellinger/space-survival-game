using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSurvivalGame.Rendering;

public static partial class ProceduralTextures
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

    /// <summary>
    /// Like CreateRightFacingTriangle, but concave: the flat back edge is replaced by a notch
    /// pulled forward to a point between the two wing corners, giving a chevron/arrowhead
    /// silhouette. notchDepthFraction (0-1) is how far forward that point sits, as a fraction
    /// of the distance from the wing corners to the nose (0 = degenerates back to a plain flat
    /// back). Splits into the two triangles sharing the nose-notch diagonal for the inside test.
    /// Accent lines trace the hull's own side edges (nose-to-wingTop, nose-to-wingBottom) and
    /// back/notch edges (wingTop-to-notch, wingBottom-to-notch) — a hull-trim look, deliberately
    /// leaving the nose-to-center spine plain now that the sides/back carry the accent instead.
    ///
    /// socketDiameterPixels also bakes a solid socketColor circle at the sprite's own center —
    /// the same point the station core rides at while StationCore.Attached (see
    /// StationCoreSystem) and the same size as its own dot (StationCoreConfig.CoreDot.
    /// SpriteSizePixels), so the core's own opaque sprite (drawn on top, in front) fully covers
    /// it while attached, and detaching reveals it as a permanent "mounting socket" mark showing
    /// where the core used to sit. 0 disables it entirely.
    /// </summary>
    public static Texture2D CreateConcaveArrowShip(GraphicsDevice graphicsDevice, int size, float notchDepthFraction, Color color, Color accentColor, int socketDiameterPixels, Color socketColor)
    {
        var data = new Color[size * size];
        var nose = new Vector2(size - 1, size / 2f);
        var wingTop = new Vector2(0, 0);
        var wingBottom = new Vector2(0, size - 1);
        var notch = new Vector2(MathHelper.Lerp(0, size - 1, notchDepthFraction), size / 2f);
        var center = new Vector2(size / 2f, size / 2f);
        var lineThickness = MathHelper.Max(1f, size * 0.06f);
        var socketRadius = socketDiameterPixels / 2f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                var inside = IsInsideTriangle(point, nose, wingTop, notch) || IsInsideTriangle(point, nose, notch, wingBottom);
                if (!inside)
                {
                    data[y * size + x] = Color.Transparent;
                    continue;
                }

                if (Vector2.Distance(point, center) <= socketRadius)
                {
                    data[y * size + x] = socketColor;
                    continue;
                }

                var onAccent = DistanceToSegment(point, nose, wingTop) <= lineThickness ||
                               DistanceToSegment(point, nose, wingBottom) <= lineThickness ||
                               DistanceToSegment(point, wingTop, notch) <= lineThickness ||
                               DistanceToSegment(point, wingBottom, notch) <= lineThickness;
                data[y * size + x] = onAccent ? accentColor : color;
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }
}
