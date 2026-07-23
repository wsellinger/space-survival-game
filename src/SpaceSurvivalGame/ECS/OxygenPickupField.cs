using System;
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
/// Scatters a handful of collectible O2 pickups — small cornflower-blue crystals — at random
/// positions within the asteroid field's extent. Each is a solid dynamic Box2D body like an
/// asteroid (so it can be physically bumped around), but its shape never opts into hit events,
/// so it's invisible to CollisionDamageSystem — collection is a simple distance check in
/// OxygenPickupSystem instead, not a damage-style hit event.
/// </summary>
public static class OxygenPickupField
{
    private const int TextureSize = 32;
    private const int ShapeVariantCount = 3;
    private const int VerticesPerShape = 6;
    private const float MinVertexRadiusFactor = 0.45f; // how jagged/asymmetric the facets look
    private const float ElongationFactor = 1.4f; // stretches vertically for a gem-like silhouette rather than a round rock

    public static void Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 centerMeters, WorldConfig worldConfig, PickupConfig pickupConfig)
    {
        var random = new Random();

        // The baked texture's canvas needs to extend past the crystal's own edge (unit
        // magnitude 1) to have room for the glow reaching GlowRadius beyond it, with a little
        // padding so the glow's feathered edge isn't clipped right at the canvas boundary.
        // Sprite.Scale below is inflated by the same factor so the crystal's own on-screen/
        // physics size stays exactly SpriteSizePixels regardless of GlowRadius.
        var glowCanvasScale = MathF.Max(1f, pickupConfig.GlowRadius) * 1.05f;

        var shapeVariants = new Vector2[ShapeVariantCount][];
        var shapeTextures = new Texture2D[ShapeVariantCount];
        for (var v = 0; v < ShapeVariantCount; v++)
        {
            shapeVariants[v] = GenerateCrystalShape(random);

            var crystalXnaVertices = new Microsoft.Xna.Framework.Vector2[shapeVariants[v].Length];
            for (var p = 0; p < crystalXnaVertices.Length; p++)
                crystalXnaVertices[p] = (shapeVariants[v][p] / glowCanvasScale).ToXna();

            shapeTextures[v] = ProceduralTextures.CreateGlowingPolygon(graphicsDevice, TextureSize,
                Microsoft.Xna.Framework.Color.CornflowerBlue, Microsoft.Xna.Framework.Color.CornflowerBlue, crystalXnaVertices, pickupConfig.GlowRadius / glowCanvasScale);
        }

        var radiusMeters = PhysicsWorld.PixelsToMeters(pickupConfig.SpriteSizePixels / 2f);
        var scale = pickupConfig.SpriteSizePixels * glowCanvasScale / TextureSize;

        for (var i = 0; i < pickupConfig.PickupCount; i++)
        {
            var positionMeters = centerMeters + new Vector2(
                (float)(random.NextDouble() * 2 - 1) * worldConfig.FieldHalfExtentMeters,
                (float)(random.NextDouble() * 2 - 1) * worldConfig.FieldHalfExtentMeters);

            var rotationRadians = (float)(random.NextDouble() * Math.PI * 2);

            var bodyDef = B2Api.b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = positionMeters;
            bodyDef.rotation = b2Rot.FromAngle(rotationRadians);

            var speed = pickupConfig.MinSpeedMetersPerSecond +
                        (float)random.NextDouble() * (pickupConfig.MaxSpeedMetersPerSecond - pickupConfig.MinSpeedMetersPerSecond);
            var velocityAngle = (float)(random.NextDouble() * Math.PI * 2);
            bodyDef.linearVelocity = new Vector2(MathF.Cos(velocityAngle), MathF.Sin(velocityAngle)) * speed;

            var angularSpeed = pickupConfig.MinAngularVelocityRadiansPerSecond +
                                (float)random.NextDouble() * (pickupConfig.MaxAngularVelocityRadiansPerSecond - pickupConfig.MinAngularVelocityRadiansPerSecond);
            bodyDef.angularVelocity = random.Next(2) == 0 ? -angularSpeed : angularSpeed;

            var bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

            // Matches the crystal's own true size exactly (unaffected by the glow — see the
            // canvas-scale compensation above, which keeps the crystal's own footprint fixed
            // regardless of GlowRadius).
            var variantIndex = random.Next(ShapeVariantCount);
            var unitVertices = shapeVariants[variantIndex];
            var points = new Vector2[unitVertices.Length];
            for (var p = 0; p < unitVertices.Length; p++) points[p] = unitVertices[p] * radiusMeters;

            // Solid (so it still bounces off asteroids like one), but its collision mask
            // excludes the ship specifically — the ship should fly straight through and get
            // it collected via OxygenPickupSystem's distance check, not bounce off it.
            var shapeDef = B2Api.b2DefaultShapeDef();
            shapeDef.density = pickupConfig.MaterialDensity;
            shapeDef.material.restitution = pickupConfig.Restitution;
            shapeDef.filter.maskBits = ~CollisionCategories.Ship;
            var hull = B2Api.b2ComputeHull(points, points.Length);
            var polygon = B2Api.b2MakePolygon(hull, 0f);
            B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);

            world.Create(
                new PhysicsBody { BodyId = bodyId },
                new Transform { PositionMeters = positionMeters, RotationRadians = rotationRadians },
                new Velocity(),
                new Sprite { Texture = shapeTextures[variantIndex], Color = Microsoft.Xna.Framework.Color.White, Size = TextureSize, Scale = scale, Parallax = 1f },
                new OxygenPickup());
        }
    }

    /// <summary>
    /// An irregular, elongated polygon in unit (-1..1) space: vertices at evenly-spaced angles
    /// with per-vertex radius and angle jitter (same technique as AsteroidField's rock shapes),
    /// stretched vertically for a gem-like silhouette, then rescaled so nothing exceeds the
    /// canvas after stretching.
    /// </summary>
    private static Vector2[] GenerateCrystalShape(Random random)
    {
        var angleStep = MathF.PI * 2f / VerticesPerShape;
        var vertices = new Vector2[VerticesPerShape];
        var maxExtent = 0f;

        for (var i = 0; i < VerticesPerShape; i++)
        {
            var angleJitter = ((float)random.NextDouble() * 2f - 1f) * (angleStep * 0.35f);
            var angle = i * angleStep + angleJitter;
            var radius = MinVertexRadiusFactor + (float)random.NextDouble() * (1f - MinVertexRadiusFactor);
            var vertex = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * ElongationFactor);
            vertices[i] = vertex;
            maxExtent = MathF.Max(maxExtent, MathF.Max(MathF.Abs(vertex.X), MathF.Abs(vertex.Y)));
        }

        for (var i = 0; i < vertices.Length; i++) vertices[i] /= maxExtent;
        return vertices;
    }
}
