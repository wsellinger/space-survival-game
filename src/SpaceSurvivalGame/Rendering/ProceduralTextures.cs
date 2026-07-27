using System;
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

    /// <summary>
    /// Like CreateRightFacingTriangle, but concave: the flat back edge is replaced by a notch
    /// pulled forward to a point between the two wing corners, giving a chevron/arrowhead
    /// silhouette. notchDepthFraction (0-1) is how far forward that point sits, as a fraction
    /// of the distance from the wing corners to the nose (0 = degenerates back to a plain flat
    /// back). Splits into the two triangles sharing the nose-notch diagonal for the inside test.
    /// </summary>
    public static Texture2D CreateConcaveArrowShip(GraphicsDevice graphicsDevice, int size, float notchDepthFraction, Color color, Color accentColor)
    {
        var data = new Color[size * size];
        var nose = new Vector2(size - 1, size / 2f);
        var wingTop = new Vector2(0, 0);
        var wingBottom = new Vector2(0, size - 1);
        var notch = new Vector2(MathHelper.Lerp(0, size - 1, notchDepthFraction), size / 2f);
        var center = new Vector2(size / 2f, size / 2f);
        var lineThickness = MathHelper.Max(1f, size * 0.06f);

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

                data[y * size + x] = DistanceToSegment(point, nose, center) <= lineThickness ? accentColor : color;
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

    public readonly struct PolygonSpeckle
    {
        public readonly Vector2 Center;         // canvas unit space, same space as the rock's own unitVertices
        public readonly Vector2[] UnitVertices; // the speckle's own small polygon, in its own -1..1 local space
        public readonly float Radius;           // the speckle's own radius, in canvas unit space
        public readonly PolygonSpot[] Spots;    // small translucent patches (e.g. rust) blended into this speckle's own fill, in ITS OWN -1..1 local space; empty if none

        public PolygonSpeckle(Vector2 center, Vector2[] unitVertices, float radius, PolygonSpot[] spots = null)
        {
            Center = center;
            UnitVertices = unitVertices;
            Radius = radius;
            Spots = spots ?? Array.Empty<PolygonSpot>();
        }
    }

    /// <summary>
    /// Like CreatePolygon, but also stamps small crystal-shaped "speckles" on top of the rock fill —
    /// each with its own small solid polygon plus a radial glow — so they read as glowing crystals
    /// embedded in (or, wherever a speckle's placement puts it near/past the rock's own edge,
    /// sticking out of) the rock's surface.
    ///
    /// Three passes: (1) fill the rock polygon as CreatePolygon does, recording which pixels are
    /// background (not rock); (2) stamp every speckle's own solid polygon on top, unconditionally —
    /// this single overwrite regardless of what's underneath (rock fill or transparent background)
    /// is what makes a speckle placed near/past the rock's edge visually poke out of the silhouette
    /// — and clear those pixels from the background mask; (3) for every pixel within glow range of
    /// a speckle, blend toward speckleGlowColor with the same eased radial falloff as
    /// CreateGlowingPolygon — background pixels fade toward transparent (a halo bleeding into open
    /// space), while rock pixels blend it into the existing rock color instead (a halo bleeding
    /// into the surrounding surface), since CrystalEdgeOffsetRange typically embeds a speckle well
    /// inside the rock with no adjacent background pixel at all — skipping rock pixels here (an
    /// earlier version of this did) left the glow invisible for every embedded speckle, which in
    /// practice is most of them.
    /// Doing solid stamping as its own pass before any glow — rather than solid+glow per speckle in
    /// one interleaved pass — is what keeps a later speckle's glow from overwriting an earlier
    /// speckle's already-stamped solid fill where two speckles happen to sit close together.
    ///
    /// Each speckle can also carry its own PolygonSpeckle.Spots (e.g. rust patches on iron ore) —
    /// blended into that speckle's own solid fill during the stamping pass, using spotBlendColor's
    /// alpha as the blend fraction (same technique as CreateSpottedPolygon), so a rich-asteroid's
    /// embedded speckles can match the look of that resource's own standalone pickup chunks.
    /// </summary>
    public static Texture2D CreateSpeckledPolygon(GraphicsDevice graphicsDevice, int size, Color rockColor, Vector2[] rockUnitVertices,
        PolygonSpeckle[] speckles, Color speckleColor, Color speckleGlowColor, float speckleGlowRadiusMultiplier, Color spotBlendColor)
    {
        var data = new Color[size * size];
        var isBackground = new bool[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        var scale = size / 2f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var localPoint = (point - center) / scale;
                var isRock = IsInsidePolygon(localPoint, rockUnitVertices);
                var index = y * size + x;
                data[index] = isRock ? rockColor : Color.Transparent;
                isBackground[index] = !isRock;
            }
        }

        foreach (var speckle in speckles)
        {
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var localPoint = (point - center) / scale;
                    var offset = localPoint - speckle.Center;
                    if (offset.Length() > speckle.Radius) continue;
                    var speckleLocalPoint = offset / speckle.Radius;
                    if (!IsInsidePolygon(speckleLocalPoint, speckle.UnitVertices)) continue;

                    var pixelColor = speckleColor;
                    foreach (var spot in speckle.Spots)
                    {
                        var spotOffset = speckleLocalPoint - spot.Center;
                        if (spotOffset.Length() > spot.Radius) continue;
                        if (!IsInsidePolygon(spotOffset / spot.Radius, spot.UnitVertices)) continue;

                        pixelColor = BlendRgb(pixelColor, spotBlendColor, spotBlendColor.A / 255f);
                    }

                    var index = y * size + x;
                    data[index] = pixelColor;
                    isBackground[index] = false;
                }
            }
        }

        foreach (var speckle in speckles)
        {
            var glowRadius = speckle.Radius * speckleGlowRadiusMultiplier;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;

                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var localPoint = (point - center) / scale;
                    var distance = (localPoint - speckle.Center).Length();
                    if (distance > glowRadius) continue;

                    var falloff = 1f - distance / glowRadius;
                    falloff *= falloff; // eases the fade, matching CreateGlowingPolygon

                    if (isBackground[index])
                    {
                        var glow = speckleGlowColor * falloff;

                        // Two nearby speckles' glow can overlap this same background pixel — keep
                        // whichever is stronger (higher alpha) rather than letting iteration order
                        // decide, so overlapping halos read as one smooth combined glow.
                        if (glow.A > data[index].A) data[index] = glow;
                    }
                    else
                    {
                        // No adjacent background pixel to fade into (the common case — an
                        // embedded speckle sits entirely inside the opaque rock) — blend the glow
                        // into the rock's own color instead, so it still reads as a soft halo
                        // lighting up the surface around the crystal rather than not appearing.
                        data[index] = BlendRgb(data[index], speckleGlowColor, falloff);
                    }
                }
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    public readonly struct PolygonSpot
    {
        public readonly Vector2 Center;         // canvas unit space, same space as the base polygon's own unitVertices
        public readonly Vector2[] UnitVertices; // the spot's own small jagged polygon, in its own -1..1 local space
        public readonly float Radius;           // the spot's own radius, in canvas unit space

        public PolygonSpot(Vector2 center, Vector2[] unitVertices, float radius)
        {
            Center = center;
            UnitVertices = unitVertices;
            Radius = radius;
        }
    }

    /// <summary>
    /// Like CreatePolygon, but blends small translucent jagged patches ("rust spots" on iron ore)
    /// into the fill wherever they land — CPU-side straight compositing using spotColor's own
    /// alpha as the blend fraction, keeping the output pixel fully opaque (unlike
    /// CreateGlowingPolygon/CreateSpeckledPolygon's glow, which actually fades to transparent
    /// background). A spot only ever blends where the base polygon itself is already filled —
    /// rust forms on the ore's own surface, unlike the crystal speckles elsewhere which
    /// intentionally poke past the rock's edge — so a spot placed near the boundary is simply
    /// clipped there, no special edge-anchoring needed.
    /// </summary>
    public static Texture2D CreateSpottedPolygon(GraphicsDevice graphicsDevice, int size, Color baseColor, Vector2[] unitVertices, PolygonSpot[] spots, Color spotColor)
    {
        var data = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        var scale = size / 2f;
        var spotAlphaFraction = spotColor.A / 255f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var localPoint = (point - center) / scale;
                var index = y * size + x;

                if (!IsInsidePolygon(localPoint, unitVertices))
                {
                    data[index] = Color.Transparent;
                    continue;
                }

                var pixelColor = baseColor;

                foreach (var spot in spots)
                {
                    var offset = localPoint - spot.Center;
                    if (offset.Length() > spot.Radius) continue;
                    if (!IsInsidePolygon(offset / spot.Radius, spot.UnitVertices)) continue;

                    pixelColor = BlendRgb(pixelColor, spotColor, spotAlphaFraction);
                }

                data[index] = pixelColor;
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
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
