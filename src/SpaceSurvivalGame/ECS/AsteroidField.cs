using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

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
    private const int MaxPlacementAttempts = 30;
    private const int ShapeVariantCount = 6;
    private const int MinVerticesPerShape = 6;
    private const int MaxVerticesPerShape = 8; // Box2D polygons cap out at 8 vertices
    private const float MinVertexRadiusFactor = 0.65f; // how "jagged" the rocks look; 1 = perfect circle

    private static readonly Microsoft.Xna.Framework.Color RockColor = new(107, 91, 78);

    public static void Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 centerMeters, WorldConfig config)
    {
        var random = new Random(config.WorldSeed);

        var shapeVariants = new Vector2[ShapeVariantCount][];
        var shapeTextures = new Texture2D[ShapeVariantCount];
        for (var v = 0; v < ShapeVariantCount; v++)
        {
            shapeVariants[v] = GenerateRockShape(random);
            var xnaVertices = new Microsoft.Xna.Framework.Vector2[shapeVariants[v].Length];
            for (var p = 0; p < xnaVertices.Length; p++) xnaVertices[p] = shapeVariants[v][p].ToXna();
            shapeTextures[v] = ProceduralTextures.CreatePolygon(graphicsDevice, BaseShapeTextureSize, RockColor, xnaVertices);
        }

        // Cell size = the largest possible sum-of-radii between any two asteroids,
        // which is the standard correctness condition for checking only the 3x3
        // neighborhood during overlap tests.
        var grid = new SpatialGrid(config.AsteroidMaxRadiusMeters * 2f);

        var fieldSideMeters = config.FieldHalfExtentMeters * 2f;
        var fieldAreaSquareMeters = fieldSideMeters * fieldSideMeters;
        var asteroidCount = (int)(config.AsteroidSpawnDensityPerSquareMeter * fieldAreaSquareMeters);

        for (var i = 0; i < asteroidCount; i++)
        {
            if (!TryFindPosition(random, grid, centerMeters, config, out var positionMeters, out var radiusMeters))
                continue; // field's too packed to fit another one here; skip it and keep going

            grid.Add(positionMeters, radiusMeters);

            var bodyDef = B2Api.b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = positionMeters;

            var speed = config.AsteroidMinSpeedMetersPerSecond +
                        (float)random.NextDouble() * (config.AsteroidMaxSpeedMetersPerSecond - config.AsteroidMinSpeedMetersPerSecond);
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            bodyDef.linearVelocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            var angularSpeed = config.AsteroidMinAngularVelocityRadiansPerSecond +
                                (float)random.NextDouble() * (config.AsteroidMaxAngularVelocityRadiansPerSecond - config.AsteroidMinAngularVelocityRadiansPerSecond);
            bodyDef.angularVelocity = random.Next(2) == 0 ? -angularSpeed : angularSpeed;

            var bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

            var variantIndex = random.Next(ShapeVariantCount);
            var unitVertices = shapeVariants[variantIndex];
            var points = new Vector2[unitVertices.Length];
            for (var p = 0; p < unitVertices.Length; p++) points[p] = unitVertices[p] * radiusMeters;

            var shapeDef = B2Api.b2DefaultShapeDef();
            shapeDef.density = config.AsteroidMaterialDensity;
            shapeDef.material.restitution = config.AsteroidRestitution;
            var hull = B2Api.b2ComputeHull(points, points.Length);
            var polygon = B2Api.b2MakePolygon(hull, 0f);
            B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);

            // BaseShapeTextureSize pixels at scale 1 would be BaseShapeTextureSize px across;
            // we want it to actually measure 2*radiusMeters in world space.
            var desiredDiameterPixels = PhysicsWorld.MetersToPixels(radiusMeters * 2f);
            var scale = desiredDiameterPixels / BaseShapeTextureSize;

            world.Create(
                new PhysicsBody { BodyId = bodyId },
                new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
                new Velocity(),
                new Sprite { Texture = shapeTextures[variantIndex], Color = Microsoft.Xna.Framework.Color.White, Size = BaseShapeTextureSize, Scale = scale, Parallax = 1f },
                new Asteroid { RadiusMeters = radiusMeters },
                new Damaging());
        }
    }

    /// <summary>
    /// An irregular, star-shaped-around-center polygon in unit (-1..1) space: vertices at
    /// evenly-spaced angles with per-vertex radius and angle jitter. Jitter is kept below
    /// half the angle step so vertices stay in angular order — guarantees a simple polygon
    /// (possibly concave) rather than one with self-intersecting edges.
    /// </summary>
    private static Vector2[] GenerateRockShape(Random random)
    {
        var vertexCount = random.Next(MinVerticesPerShape, MaxVerticesPerShape + 1);
        var vertices = new Vector2[vertexCount];
        var angleStep = MathF.PI * 2f / vertexCount;

        for (var i = 0; i < vertexCount; i++)
        {
            var angleJitter = ((float)random.NextDouble() * 2f - 1f) * (angleStep * 0.4f);
            var angle = i * angleStep + angleJitter;
            var radius = MinVertexRadiusFactor + (float)random.NextDouble() * (1f - MinVertexRadiusFactor);
            vertices[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }

        return vertices;
    }

    private static bool TryFindPosition(Random random, SpatialGrid grid, Vector2 centerMeters, WorldConfig config, out Vector2 positionMeters, out float radiusMeters)
    {
        var clearRadiusMeters = PhysicsWorld.PixelsToMeters(config.ShipSpawnClearRadiusPixels);

        for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            var candidatePosition = centerMeters + new Vector2(
                (float)(random.NextDouble() * 2 - 1) * config.FieldHalfExtentMeters,
                (float)(random.NextDouble() * 2 - 1) * config.FieldHalfExtentMeters);
            var candidateRadius = config.AsteroidMinRadiusMeters +
                                   (float)random.NextDouble() * (config.AsteroidMaxRadiusMeters - config.AsteroidMinRadiusMeters);

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
