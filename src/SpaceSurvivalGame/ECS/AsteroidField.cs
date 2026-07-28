using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS;

/// <summary>
/// Scatters a fixed-size field of dynamic, collidable asteroid entities around a
/// center point, generated once at startup (no chunk streaming yet — deferred
/// until the world needs to feel unbounded or per-region persistence matters).
/// Seeded from WorldConfig.WorldSeed so the layout is reproducible across runs.
/// Every asteroid starts with a small random drift velocity and bounces off
/// others on contact. Density is a fixed value from config (not tied to the
/// ship at runtime), same for every asteroid, so mass just scales with area.
///
/// Asteroids pick from a small shared set of irregular rock shapes (not a
/// unique shape per asteroid — keeps texture count and per-asteroid generation
/// cost down) at whatever radius that instance rolled; the Box2D shape is the
/// convex hull of the same points, so physics roughly matches what's drawn.
/// </summary>
public static class AsteroidField
{
    private const int BaseShapeTextureSize = 64;
    private const int OxygenRichShapeTextureSize = 128; // bigger canvas than BaseShapeTextureSize: the margined-down rock silhouette plus small crystal speckles need finer detail than a plain rock does
    private const int IronRichShapeTextureSize = 128;
    private const int MaxPlacementAttempts = 30;
    private const int ShapeVariantCount = 6;
    private const int MinVerticesPerShape = 6;
    private const int MaxVerticesPerShape = 8; // Box2D polygons cap out at 8 vertices
    private const float MinVertexRadiusFactor = 0.65f; // how "jagged" the rocks look; 1 = perfect circle
    private const float RockAngleJitterFraction = 0.4f;

    public static void Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 centerMeters, WorldConfig config,
        OxygenPickupConfig oxygenPickupConfig, IronPickupConfig ironPickupConfig)
    {
        var random = new Random(config.WorldSeed);
        var oxygenRichConfig = config.Asteroid.OxygenRich;
        var ironRichConfig = config.Asteroid.IronRich;

        var rockColor = ColorHex.Parse(config.Asteroid.RockColorHex);
        var crystalColor = ColorHex.Parse(oxygenPickupConfig.ColorHex);
        var oreColor = ColorHex.Parse(ironPickupConfig.ColorHex);
        var rustSpotColor = ColorHex.Parse(ironPickupConfig.RustSpots.ColorHex);

        // Canvas margin for the rich-asteroid textures only: embedded/protruding speckles can
        // extend past the rock's own unit-magnitude-1 edge, but (unlike the plain rock texture,
        // which fills its canvas exactly to that edge with zero spare room) their canvas needs
        // padding for that — same trick OxygenPickupField uses for its own glow (harmless, just
        // a little unused margin, for variants like iron with no glow at all). Sprite.Scale for
        // rich asteroids below is inflated by the matching factor so the rock's own true
        // on-screen/physics size stays exactly 2*radiusMeters regardless. Each variant gets its
        // own scale since the two configs can differ.
        var oxygenCanvasMarginScale = ComputeCanvasMarginScale(oxygenRichConfig);
        var ironCanvasMarginScale = ComputeCanvasMarginScale(ironRichConfig);

        // Baked white (like ParticleEffects' own spark texture / IronPickupField's own sparkle
        // dot) and tinted at draw time by the shared MetallicSparkleRenderSystem, so an
        // iron-rich asteroid's embedded speckles can carry the same animated glint as the
        // standalone ore chunks.
        var sparkleTexture = ProceduralTextures.CreateCircle(graphicsDevice, ironPickupConfig.Sparkle.SizePixels, Microsoft.Xna.Framework.Color.White);

        var shapeVariants = new Vector2[ShapeVariantCount][];
        var shapeTextures = new Texture2D[ShapeVariantCount];
        var oxygenRichShapeTextures = new Texture2D[ShapeVariantCount];
        var ironRichShapeTextures = new Texture2D[ShapeVariantCount];
        var ironSpecklesPerVariant = new ProceduralTextures.PolygonSpeckle[ShapeVariantCount][];
        for (var v = 0; v < ShapeVariantCount; v++)
        {
            var vertexCount = random.Next(MinVerticesPerShape, MaxVerticesPerShape + 1);
            shapeVariants[v] = ProceduralShapeGenerator.GenerateJitteredPolygon(random, vertexCount, MinVertexRadiusFactor, RockAngleJitterFraction);

            var xnaVertices = new Microsoft.Xna.Framework.Vector2[shapeVariants[v].Length];
            for (var p = 0; p < xnaVertices.Length; p++) xnaVertices[p] = shapeVariants[v][p].ToXna();
            shapeTextures[v] = ProceduralTextures.CreatePolygon(graphicsDevice, BaseShapeTextureSize, rockColor, xnaVertices);

            (oxygenRichShapeTextures[v], _) = CreateSpeckledRockTexture(random, graphicsDevice, shapeVariants[v], oxygenRichConfig, oxygenCanvasMarginScale,
                OxygenRichShapeTextureSize, OxygenPickupField.CrystalVerticesPerShape, OxygenPickupField.CrystalVerticesPerShape, OxygenPickupField.CrystalMinVertexRadiusFactor,
                OxygenPickupField.CrystalAngleJitterFraction, OxygenPickupField.CrystalElongationFactor, rockColor, crystalColor);

            // Same shape as IronPickupField's own standalone pickup chunks (unlike oxygen, whose
            // embedded speckles intentionally differ from its own pickup crystals) — including
            // the same rust spots, so an iron-rich asteroid's embedded ore matches the loose
            // chunks scattered around it.
            (ironRichShapeTextures[v], ironSpecklesPerVariant[v]) = CreateSpeckledRockTexture(random, graphicsDevice, shapeVariants[v], ironRichConfig, ironCanvasMarginScale,
                IronRichShapeTextureSize, IronPickupField.OreMinVerticesPerShape, IronPickupField.OreMaxVerticesPerShape, IronPickupField.OreMinVertexRadiusFactor,
                IronPickupField.OreAngleJitterFraction, IronPickupField.OreElongationFactor, rockColor, oreColor,
                rustSpotColor, ironPickupConfig.RustSpots.CountRange, ironPickupConfig.RustSpots.SizeUnitRange);
        }

        // Cell size = the largest possible sum-of-radii between any two asteroids,
        // which is the standard correctness condition for checking only the 3x3
        // neighborhood during overlap tests.
        var grid = new SpatialGrid(config.Asteroid.RadiusMetersRange.Max * 2f);

        var fieldSideMeters = config.FieldHalfExtentMeters * 2f;
        var fieldAreaSquareMeters = fieldSideMeters * fieldSideMeters;
        var asteroidCount = (int)(config.Asteroid.SpawnDensityPerSquareMeter * fieldAreaSquareMeters);

        for (var i = 0; i < asteroidCount; i++)
        {
            if (!TryFindPosition(random, grid, centerMeters, config, out var positionMeters, out var radiusMeters))
                continue; // field's too packed to fit another one here; skip it and keep going

            grid.Add(positionMeters, radiusMeters);

            var bodyDef = B2Api.b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = positionMeters;

            var speed = config.Asteroid.SpeedMetersPerSecondRange.Min +
                        (float)random.NextDouble() * (config.Asteroid.SpeedMetersPerSecondRange.Max - config.Asteroid.SpeedMetersPerSecondRange.Min);
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            bodyDef.linearVelocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            var angularSpeed = config.Asteroid.AngularVelocityRadiansPerSecondRange.Min +
                                (float)random.NextDouble() * (config.Asteroid.AngularVelocityRadiansPerSecondRange.Max - config.Asteroid.AngularVelocityRadiansPerSecondRange.Min);
            bodyDef.angularVelocity = random.Next(2) == 0 ? -angularSpeed : angularSpeed;

            var bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

            var variantIndex = random.Next(ShapeVariantCount);
            var unitVertices = shapeVariants[variantIndex];
            var points = new Vector2[unitVertices.Length];
            for (var p = 0; p < unitVertices.Length; p++) points[p] = unitVertices[p] * radiusMeters;

            var shapeDef = B2Api.b2DefaultShapeDef();
            shapeDef.density = config.Asteroid.MaterialDensity;
            shapeDef.material.restitution = config.Asteroid.Restitution;
            shapeDef.enableHitEvents = true; // lets OxygenCrystalReleaseSystem/IronOreReleaseSystem see any collision this asteroid is part of, not just ones involving the ship
            var hull = B2Api.b2ComputeHull(points, points.Length);
            var polygon = B2Api.b2MakePolygon(hull, 0f);
            B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);

            // Mutually exclusive: one additive roll across both rich-asteroid chances, so they
            // never overlap and their fractions add up intuitively (e.g. 15%+15% = 30% chance of
            // some special type) rather than each rolling independently.
            var typeRoll = random.NextDouble();
            AsteroidType type;
            if (typeRoll < oxygenRichConfig.SpawnChanceFraction) type = AsteroidType.OxygenRich;
            else if (typeRoll < oxygenRichConfig.SpawnChanceFraction + ironRichConfig.SpawnChanceFraction) type = AsteroidType.IronRich;
            else type = AsteroidType.Ordinary;

            // BaseShapeTextureSize pixels at scale 1 would be BaseShapeTextureSize px across;
            // we want it to actually measure 2*radiusMeters in world space. Rich-asteroid
            // variants use a different (padded) canvas each, so their size/scale differ.
            var desiredDiameterPixels = PhysicsWorld.MetersToPixels(radiusMeters * 2f);

            Texture2D texture;
            int textureSize;
            float scale;
            switch (type)
            {
                case AsteroidType.OxygenRich:
                    texture = oxygenRichShapeTextures[variantIndex];
                    textureSize = OxygenRichShapeTextureSize;
                    scale = desiredDiameterPixels * oxygenCanvasMarginScale / OxygenRichShapeTextureSize;
                    break;
                case AsteroidType.IronRich:
                    texture = ironRichShapeTextures[variantIndex];
                    textureSize = IronRichShapeTextureSize;
                    scale = desiredDiameterPixels * ironCanvasMarginScale / IronRichShapeTextureSize;
                    break;
                default:
                    texture = shapeTextures[variantIndex];
                    textureSize = BaseShapeTextureSize;
                    scale = desiredDiameterPixels / BaseShapeTextureSize;
                    break;
            }

            // Iron-rich asteroids get a small positive nudge (matching IronPickupField's own
            // OreLayerDepth) so their embedded MetallicSparkle glints — drawn at the default 0 —
            // reliably win the SpriteSortMode.BackToFront tie against this same sprite, instead of
            // repeating the "glint hidden behind its own opaque parent" bug fixed there.
            var asteroidLayerDepth = type == AsteroidType.IronRich ? 0.01f : 0f;

            var entity = world.Create(
                new PhysicsBody { BodyId = bodyId },
                new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
                new Velocity(),
                new Sprite { Texture = texture, Color = Microsoft.Xna.Framework.Color.White, Size = textureSize, Scale = scale, LayerDepth = asteroidLayerDepth, Parallax = 1f },
                new Asteroid { RadiusMeters = radiusMeters, Type = type },
                new Damaging());

            // Same animated glints as the standalone ore chunks (see MetallicSparkleRenderSystem)
            // — IronPickupConfig.Sparkle.Count of them PER embedded speckle (not one total), so an
            // asteroid's ore reads with the same sparkle density as a loose chunk rather than
            // just a single glint per speckle. Each speckle's own points are generated in that
            // speckle's own local face (GenerateSparklePoints, same technique as the standalone
            // chunks), then offset by the speckle's own baked canvas position — converted from
            // margined canvas unit space into this asteroid instance's actual on-screen pixel
            // size via textureSize/scale — so they land on the correct facet instead of floating
            // independently.
            if (type == AsteroidType.IronRich)
            {
                var speckles = ironSpecklesPerVariant[variantIndex];
                var sparkleOffsetsPixels = new Microsoft.Xna.Framework.Vector2[speckles.Length * ironPickupConfig.Sparkle.Count];
                var sparklePhasesRadians = new float[speckles.Length * ironPickupConfig.Sparkle.Count];
                var sparkleIndex = 0;
                foreach (var speckle in speckles)
                {
                    var speckleUnitVertices = new Vector2[speckle.UnitVertices.Length];
                    for (var p = 0; p < speckleUnitVertices.Length; p++) speckleUnitVertices[p] = speckle.UnitVertices[p].ToNumerics();

                    var speckleCenterPixels = speckle.Center * (textureSize / 2f) * scale;
                    var speckleRadiusPixels = speckle.Radius * (textureSize / 2f) * scale;
                    var (localOffsetsPixels, localPhasesRadians) = ProceduralShapeGenerator.GenerateSparklePoints(random, speckleUnitVertices, ironPickupConfig.Sparkle.Count, speckleRadiusPixels);

                    for (var s = 0; s < localOffsetsPixels.Length; s++)
                    {
                        sparkleOffsetsPixels[sparkleIndex] = speckleCenterPixels + localOffsetsPixels[s];
                        sparklePhasesRadians[sparkleIndex] = localPhasesRadians[s];
                        sparkleIndex++;
                    }
                }

                world.Add(entity, new MetallicSparkle { OffsetsPixels = sparkleOffsetsPixels, PhasesRadians = sparklePhasesRadians, Texture = sparkleTexture });
            }
        }
    }

    /// <summary>
    /// Worst-case canvas padding a rich-asteroid texture needs: a speckle's center can sit as far
    /// out as the rock's own max possible edge distance (1, since every point on the rock
    /// polygon is within radius 1 of center) plus the max outward offset, and its own solid
    /// radius plus glow reach further still.
    /// </summary>
    private static float ComputeCanvasMarginScale(RichAsteroidConfig config)
    {
        var maxSpeckleCenterDistance = 1f + config.CrystalEdgeOffsetRange.Max;
        var maxSpeckleReach = config.CrystalSizeUnitRange.Max * (1f + config.CrystalGlowRadiusMultiplier);
        return MathF.Max(1f, maxSpeckleCenterDistance + maxSpeckleReach) * 1.05f;
    }

    /// <summary>
    /// Bakes one rich-asteroid texture variant: the same rock silhouette as the plain version
    /// (rockUnitVertices, so the Box2D collision hull stays identical between an ordinary and
    /// rich asteroid of this variant), but with a handful of speckles (glowing or not, per
    /// config.CrystalGlowRadiusMultiplier — 0 disables it entirely) stamped on top via
    /// ProceduralTextures.CreateSpeckledPolygon, shaped via the given crystal* parameters —
    /// callers typically pass the matching resource's own standalone-pickup shape constants
    /// (crystalMinVerticesPerShape/crystalMaxVerticesPerShape lets that vary per instance the
    /// same way the pickup itself does) so the two read as the same material, though nothing
    /// requires that. Everything is baked into canvasMarginScale-deflated canvas space so
    /// speckles have room to extend past the rock's own edge without being clipped by the
    /// texture bounds.
    ///
    /// rustSpotColor is optional (null for oxygen, which has no rust) — when given, each speckle
    /// also gets a couple of small jagged translucent patches blended into its own fill, same
    /// technique and shape parameters as IronPickupField.Create's standalone pickup chunks, so an
    /// iron-rich asteroid's embedded speckles match the look of the loose ore chunks scattered
    /// around it.
    ///
    /// Also returns the baked PolygonSpeckle data itself (center/radius/shape, in the same
    /// margined canvas unit space baked into the texture) — callers that want to attach an
    /// animated MetallicSparkle to each embedded speckle (currently just iron) need these;
    /// oxygen's caller simply ignores them.
    /// </summary>
    private static (Texture2D Texture, ProceduralTextures.PolygonSpeckle[] Speckles) CreateSpeckledRockTexture(Random random, GraphicsDevice graphicsDevice, Vector2[] rockUnitVertices,
        RichAsteroidConfig config, float canvasMarginScale, int textureSize, int crystalMinVerticesPerShape, int crystalMaxVerticesPerShape,
        float crystalMinVertexRadiusFactor, float crystalAngleJitterFraction, float crystalElongationFactor,
        Microsoft.Xna.Framework.Color rockColor, Microsoft.Xna.Framework.Color crystalColor,
        Microsoft.Xna.Framework.Color? rustSpotColor = null, IntRange rustSpotCountRange = default, FloatRange rustSpotSizeUnitRange = default)
    {
        var marginedXnaVertices = new Microsoft.Xna.Framework.Vector2[rockUnitVertices.Length];
        for (var p = 0; p < marginedXnaVertices.Length; p++) marginedXnaVertices[p] = (rockUnitVertices[p] / canvasMarginScale).ToXna();

        var speckleCount = random.Next(config.CrystalCountRange.Min, config.CrystalCountRange.Max + 1);
        var speckles = new ProceduralTextures.PolygonSpeckle[speckleCount];
        for (var s = 0; s < speckleCount; s++)
        {
            var placementAngle = (float)(random.NextDouble() * Math.PI * 2);

            // The rock isn't a circle — its real edge distance varies by angle, dipping well
            // below its own nominal radius of 1 at concave points between vertices. Anchoring
            // the offset to the ACTUAL local edge (rather than a flat placement radius) is what
            // keeps a speckle from ever landing entirely outside the silhouette as a detached
            // floating blob.
            var edgeRadius = ProceduralShapeGenerator.GetPolygonRadiusAtAngle(rockUnitVertices, placementAngle);
            var edgeOffset = config.CrystalEdgeOffsetRange.Min +
                (float)random.NextDouble() * (config.CrystalEdgeOffsetRange.Max - config.CrystalEdgeOffsetRange.Min);
            var placementRadius = edgeRadius + edgeOffset;
            var speckleCenter = new Vector2(MathF.Cos(placementAngle), MathF.Sin(placementAngle)) * placementRadius;

            var speckleSize = config.CrystalSizeUnitRange.Min +
                (float)random.NextDouble() * (config.CrystalSizeUnitRange.Max - config.CrystalSizeUnitRange.Min);

            var crystalVertexCount = random.Next(crystalMinVerticesPerShape, crystalMaxVerticesPerShape + 1);
            var crystalUnitVertices = ProceduralShapeGenerator.GenerateJitteredPolygon(random, crystalVertexCount,
                crystalMinVertexRadiusFactor, crystalAngleJitterFraction, crystalElongationFactor, rescaleToUnitBounds: true);
            var crystalXnaVertices = new Microsoft.Xna.Framework.Vector2[crystalUnitVertices.Length];
            for (var p = 0; p < crystalXnaVertices.Length; p++) crystalXnaVertices[p] = crystalUnitVertices[p].ToXna();

            var rustSpots = rustSpotColor.HasValue
                ? ProceduralShapeGenerator.GenerateJaggedSpots(random, crystalUnitVertices, rustSpotCountRange, rustSpotSizeUnitRange)
                : null;

            speckles[s] = new ProceduralTextures.PolygonSpeckle(
                (speckleCenter / canvasMarginScale).ToXna(),
                crystalXnaVertices,
                speckleSize / canvasMarginScale,
                rustSpots);
        }

        var texture = ProceduralTextures.CreateSpeckledPolygon(graphicsDevice, textureSize, rockColor, marginedXnaVertices,
            speckles, crystalColor, crystalColor, config.CrystalGlowRadiusMultiplier, rustSpotColor ?? Microsoft.Xna.Framework.Color.Transparent);

        return (texture, speckles);
    }

    private static bool TryFindPosition(Random random, SpatialGrid grid, Vector2 centerMeters, WorldConfig config, out Vector2 positionMeters, out float radiusMeters)
    {
        var clearRadiusMeters = PhysicsWorld.PixelsToMeters(config.ShipSpawnClearRadiusPixels);

        for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            var candidatePosition = centerMeters + new Vector2(
                (float)(random.NextDouble() * 2 - 1) * config.FieldHalfExtentMeters,
                (float)(random.NextDouble() * 2 - 1) * config.FieldHalfExtentMeters);
            var candidateRadius = config.Asteroid.RadiusMetersRange.Min +
                                   (float)random.NextDouble() * (config.Asteroid.RadiusMetersRange.Max - config.Asteroid.RadiusMetersRange.Min);

            var minDistanceFromCenter = clearRadiusMeters + candidateRadius;
            if (Vector2.DistanceSquared(candidatePosition, centerMeters) < minDistanceFromCenter * minDistanceFromCenter)
                continue; // too close to where the ship spawns

            if (!grid.Overlaps(candidatePosition, candidateRadius))
            {
                positionMeters = candidatePosition;
                radiusMeters = candidateRadius;
                return true;
            }
        }

        positionMeters = default;
        radiusMeters = default;
        return false;
    }

    /// <summary>
    /// Uniform grid over placed (position, radius) pairs so overlap checks only
    /// look at nearby cells instead of every previously-placed asteroid — an
    /// O(n^2) all-pairs check isn't viable once AsteroidCount is in the tens of
    /// thousands.
    /// </summary>
    private sealed class SpatialGrid
    {
        private readonly float _cellSize;
        private readonly Dictionary<(int, int), List<(Vector2 Position, float Radius)>> _cells = new();

        public SpatialGrid(float cellSize) => _cellSize = cellSize;

        public void Add(Vector2 position, float radius)
        {
            var cell = CellOf(position);
            if (!_cells.TryGetValue(cell, out var list))
            {
                list = new List<(Vector2, float)>();
                _cells[cell] = list;
            }

            list.Add((position, radius));
        }

        public bool Overlaps(Vector2 position, float radius)
        {
            var (cellX, cellY) = CellOf(position);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (!_cells.TryGetValue((cellX + dx, cellY + dy), out var neighbors)) continue;

                    foreach (var (otherPosition, otherRadius) in neighbors)
                    {
                        var minDistance = radius + otherRadius;
                        if (Vector2.DistanceSquared(position, otherPosition) < minDistance * minDistance)
                            return true;
                    }
                }
            }

            return false;
        }

        private (int, int) CellOf(Vector2 position) =>
            ((int)MathF.Floor(position.X / _cellSize), (int)MathF.Floor(position.Y / _cellSize));
    }
}
