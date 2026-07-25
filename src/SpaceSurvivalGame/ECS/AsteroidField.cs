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
    private const int MaxPlacementAttempts = 30;
    private const int ShapeVariantCount = 6;
    private const int MinVerticesPerShape = 6;
    private const int MaxVerticesPerShape = 8; // Box2D polygons cap out at 8 vertices
    private const float MinVertexRadiusFactor = 0.65f; // how "jagged" the rocks look; 1 = perfect circle
    private const float RockAngleJitterFraction = 0.4f;

    private static readonly Microsoft.Xna.Framework.Color RockColor = new(107, 91, 78);
    private static readonly Microsoft.Xna.Framework.Color CrystalColor = Microsoft.Xna.Framework.Color.CornflowerBlue;

    public static void Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 centerMeters, WorldConfig config)
    {
        var random = new Random(config.WorldSeed);
        var oxygenRichConfig = config.Asteroid.OxygenRich;

        // Canvas margin for the oxygen-rich textures only: embedded/protruding crystal speckles
        // can extend past the rock's own unit-magnitude-1 edge, but (unlike the plain rock
        // texture, which fills its canvas exactly to that edge with zero spare room) their
        // canvas needs padding for that — same trick OxygenPickupField uses for its own glow.
        // Sprite.Scale for oxygen-rich asteroids below is inflated by this same factor so the
        // rock's own true on-screen/physics size stays exactly 2*radiusMeters regardless.
        // Worst case: a speckle's center sits at the rock's own max possible edge distance (1,
        // since every point on the rock polygon is within radius 1 of center) plus the max
        // outward offset, and its own solid radius plus glow reach further still.
        var maxSpeckleCenterDistance = 1f + oxygenRichConfig.CrystalEdgeOffsetRange.Max;
        var maxSpeckleReach = oxygenRichConfig.CrystalSizeUnitRange.Max * (1f + oxygenRichConfig.CrystalGlowRadiusMultiplier);
        var canvasMarginScale = MathF.Max(1f, maxSpeckleCenterDistance + maxSpeckleReach) * 1.05f;

        var shapeVariants = new Vector2[ShapeVariantCount][];
        var shapeTextures = new Texture2D[ShapeVariantCount];
        var oxygenRichShapeTextures = new Texture2D[ShapeVariantCount];
        for (var v = 0; v < ShapeVariantCount; v++)
        {
            var vertexCount = random.Next(MinVerticesPerShape, MaxVerticesPerShape + 1);
            shapeVariants[v] = ProceduralShapeGenerator.GenerateJitteredPolygon(random, vertexCount, MinVertexRadiusFactor, RockAngleJitterFraction);

            var xnaVertices = new Microsoft.Xna.Framework.Vector2[shapeVariants[v].Length];
            for (var p = 0; p < xnaVertices.Length; p++) xnaVertices[p] = shapeVariants[v][p].ToXna();
            shapeTextures[v] = ProceduralTextures.CreatePolygon(graphicsDevice, BaseShapeTextureSize, RockColor, xnaVertices);

            oxygenRichShapeTextures[v] = CreateOxygenRichTexture(random, graphicsDevice, shapeVariants[v], oxygenRichConfig, canvasMarginScale);
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
            var hull = B2Api.b2ComputeHull(points, points.Length);
            var polygon = B2Api.b2MakePolygon(hull, 0f);
            B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);

            var isOxygenRich = random.NextDouble() < oxygenRichConfig.SpawnChanceFraction;

            // BaseShapeTextureSize pixels at scale 1 would be BaseShapeTextureSize px across;
            // we want it to actually measure 2*radiusMeters in world space. Oxygen-rich
            // asteroids use a different (padded) canvas, so their size/scale differ.
            var desiredDiameterPixels = PhysicsWorld.MetersToPixels(radiusMeters * 2f);

            Texture2D texture;
            int textureSize;
            float scale;
            if (isOxygenRich)
            {
                texture = oxygenRichShapeTextures[variantIndex];
                textureSize = OxygenRichShapeTextureSize;
                scale = desiredDiameterPixels * canvasMarginScale / OxygenRichShapeTextureSize;
            }
            else
            {
                texture = shapeTextures[variantIndex];
                textureSize = BaseShapeTextureSize;
                scale = desiredDiameterPixels / BaseShapeTextureSize;
            }

            world.Create(
                new PhysicsBody { BodyId = bodyId },
                new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
                new Velocity(),
                new Sprite { Texture = texture, Color = Microsoft.Xna.Framework.Color.White, Size = textureSize, Scale = scale, Parallax = 1f },
                new Asteroid { RadiusMeters = radiusMeters, Type = isOxygenRich ? AsteroidType.OxygenRich : AsteroidType.Ordinary },
                new Damaging());
        }
    }

    /// <summary>
    /// Bakes one oxygen-rich texture variant: the same rock silhouette as the plain version
    /// (rockUnitVertices, so the Box2D collision hull stays identical between an ordinary and
    /// oxygen-rich asteroid of this variant), but with a handful of glowing crystal speckles
    /// (same shape as OxygenPickupField's own crystals) stamped on top via
    /// ProceduralTextures.CreateSpeckledPolygon. Everything is baked into canvasMarginScale-
    /// deflated canvas space so speckles have room to extend past the rock's own edge without
    /// being clipped by the texture bounds.
    /// </summary>
    private static Texture2D CreateOxygenRichTexture(Random random, GraphicsDevice graphicsDevice, Vector2[] rockUnitVertices,
        OxygenRichAsteroidConfig config, float canvasMarginScale)
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
            var edgeRadius = GetPolygonRadiusAtAngle(rockUnitVertices, placementAngle);
            var edgeOffset = config.CrystalEdgeOffsetRange.Min +
                (float)random.NextDouble() * (config.CrystalEdgeOffsetRange.Max - config.CrystalEdgeOffsetRange.Min);
            var placementRadius = edgeRadius + edgeOffset;
            var speckleCenter = new Vector2(MathF.Cos(placementAngle), MathF.Sin(placementAngle)) * placementRadius;

            var speckleSize = config.CrystalSizeUnitRange.Min +
                (float)random.NextDouble() * (config.CrystalSizeUnitRange.Max - config.CrystalSizeUnitRange.Min);

            // Same generator, same parameters OxygenPickupField uses for its own crystals — the
            // speckles are meant to look exactly like the pickups' glowing O2 crystals.
            var crystalUnitVertices = ProceduralShapeGenerator.GenerateJitteredPolygon(random, OxygenPickupField.CrystalVerticesPerShape,
                OxygenPickupField.CrystalMinVertexRadiusFactor, OxygenPickupField.CrystalAngleJitterFraction, OxygenPickupField.CrystalElongationFactor,
                rescaleToUnitBounds: true);
            var crystalXnaVertices = new Microsoft.Xna.Framework.Vector2[crystalUnitVertices.Length];
            for (var p = 0; p < crystalXnaVertices.Length; p++) crystalXnaVertices[p] = crystalUnitVertices[p].ToXna();

            speckles[s] = new ProceduralTextures.PolygonSpeckle(
                (speckleCenter / canvasMarginScale).ToXna(),
                crystalXnaVertices,
                speckleSize / canvasMarginScale);
        }

        return ProceduralTextures.CreateSpeckledPolygon(graphicsDevice, OxygenRichShapeTextureSize, RockColor, marginedXnaVertices,
            speckles, CrystalColor, CrystalColor, config.CrystalGlowRadiusMultiplier);
    }

    /// <summary>
    /// Distance from the origin to the star-shaped polygon's own boundary in the given
    /// direction — i.e. where a ray from the center at this angle exits the polygon. Vertices
    /// must be in angular order around the origin (guaranteed by
    /// ProceduralShapeGenerator.GenerateJitteredPolygon's jitter-below-half-angle-step rule),
    /// so exactly one edge brackets any given angle.
    /// </summary>
    private static float GetPolygonRadiusAtAngle(Vector2[] vertices, float angle)
    {
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

        for (var i = 0; i < vertices.Length; i++)
        {
            var a = vertices[i];
            var edge = vertices[(i + 1) % vertices.Length] - a;

            // Solve a + s*edge == t*direction for s in [0,1], t >= 0 (a ray-vs-segment
            // intersection, via Cramer's rule on the 2x2 system).
            var denom = edge.X * direction.Y - edge.Y * direction.X;
            if (MathF.Abs(denom) < 1e-6f) continue; // edge parallel to the ray

            var s = (direction.X * a.Y - direction.Y * a.X) / denom;
            if (s < 0f || s > 1f) continue;

            var t = (edge.X * a.Y - edge.Y * a.X) / denom;
            if (t >= 0f) return t;
        }

        return 0.8f; // shouldn't happen for a valid simple star-shaped polygon; a safe fallback
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
