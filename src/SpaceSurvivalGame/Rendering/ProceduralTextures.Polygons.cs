using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSurvivalGame.Rendering;

public static partial class ProceduralTextures
{
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
}
