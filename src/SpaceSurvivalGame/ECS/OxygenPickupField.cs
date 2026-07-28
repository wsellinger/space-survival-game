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

    // Exposed publicly so AsteroidField can generate crystal speckles that look exactly like
    // these pickups (same shape parameters), not just coincidentally similar.
    public const int CrystalVerticesPerShape = 6;
    public const float CrystalMinVertexRadiusFactor = 0.45f; // how jagged/asymmetric the facets look
    public const float CrystalAngleJitterFraction = 0.35f;
    public const float CrystalElongationFactor = 1.4f; // stretches vertically for a gem-like silhouette rather than a round rock

    /// <summary>Already-baked shape data shared by every pickup crystal, so a new one can be spawned at runtime (see SpawnPickup) without re-baking textures.</summary>
    public readonly struct PickupAssets
    {
        public readonly Vector2[][] ShapeVariants;
        public readonly Texture2D[] ShapeTextures;

        public PickupAssets(Vector2[][] shapeVariants, Texture2D[] shapeTextures)
        {
            ShapeVariants = shapeVariants;
            ShapeTextures = shapeTextures;
        }
    }

    public static PickupAssets Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 centerMeters, WorldConfig worldConfig, OxygenPickupConfig pickupConfig)
    {
        var random = new Random();

        // The baked texture's canvas needs to extend past the crystal's own edge (unit
        // magnitude 1) to have room for the glow reaching GlowRadius beyond it, with a little
        // padding so the glow's feathered edge isn't clipped right at the canvas boundary.
        // Sprite.Scale below is inflated by the same factor so the crystal's own on-screen/
        // physics size stays exactly SpriteSizePixels regardless of GlowRadius.
        var glowCanvasScale = MathF.Max(1f, pickupConfig.GlowRadius) * 1.05f;
        var crystalColor = ColorHex.Parse(pickupConfig.ColorHex);

        var shapeVariants = new Vector2[ShapeVariantCount][];
        var shapeTextures = new Texture2D[ShapeVariantCount];
        for (var v = 0; v < ShapeVariantCount; v++)
        {
            shapeVariants[v] = ProceduralShapeGenerator.GenerateJitteredPolygon(random, CrystalVerticesPerShape, CrystalMinVertexRadiusFactor,
                CrystalAngleJitterFraction, CrystalElongationFactor, rescaleToUnitBounds: true);

            var crystalXnaVertices = new Microsoft.Xna.Framework.Vector2[shapeVariants[v].Length];
            for (var p = 0; p < crystalXnaVertices.Length; p++)
                crystalXnaVertices[p] = (shapeVariants[v][p] / glowCanvasScale).ToXna();

            shapeTextures[v] = ProceduralTextures.CreateGlowingPolygon(graphicsDevice, TextureSize,
                crystalColor, crystalColor, crystalXnaVertices, pickupConfig.GlowRadius / glowCanvasScale);
        }

        var assets = new PickupAssets(shapeVariants, shapeTextures);

        for (var i = 0; i < pickupConfig.PickupCount; i++)
        {
            var positionMeters = centerMeters + new Vector2(
                (float)(random.NextDouble() * 2 - 1) * worldConfig.FieldHalfExtentMeters,
                (float)(random.NextDouble() * 2 - 1) * worldConfig.FieldHalfExtentMeters);

            SpawnPickup(world, physicsWorld, positionMeters, assets, pickupConfig, random);
        }

        return assets;
    }

    /// <summary>
    /// Spawns a single new O2 pickup crystal at the given position, reusing already-baked shape
    /// assets (see Create/PickupAssets) rather than re-baking textures — used both by Create's
    /// initial scatter and by OxygenCrystalReleaseSystem for crystals popped loose from an
    /// oxygen-rich asteroid at runtime.
    /// </summary>
    public static void SpawnPickup(World world, PhysicsWorld physicsWorld, Vector2 positionMeters, PickupAssets assets, OxygenPickupConfig config, Random random)
    {
        var rotationRadians = (float)(random.NextDouble() * Math.PI * 2);

        var bodyDef = B2Api.b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = positionMeters;
        bodyDef.rotation = b2Rot.FromAngle(rotationRadians);

        var speed = config.Motion.SpeedMetersPerSecondRange.Min +
                    (float)random.NextDouble() * (config.Motion.SpeedMetersPerSecondRange.Max - config.Motion.SpeedMetersPerSecondRange.Min);
        var velocityAngle = (float)(random.NextDouble() * Math.PI * 2);
        bodyDef.linearVelocity = new Vector2(MathF.Cos(velocityAngle), MathF.Sin(velocityAngle)) * speed;

        var angularSpeed = config.Motion.AngularVelocityRadiansPerSecondRange.Min +
                            (float)random.NextDouble() * (config.Motion.AngularVelocityRadiansPerSecondRange.Max - config.Motion.AngularVelocityRadiansPerSecondRange.Min);
        bodyDef.angularVelocity = random.Next(2) == 0 ? -angularSpeed : angularSpeed;

        var bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

        // Matches the crystal's own true size exactly (unaffected by the glow — see the
        // canvas-scale compensation below, which keeps the crystal's own footprint fixed
        // regardless of GlowRadius).
        var glowCanvasScale = MathF.Max(1f, config.GlowRadius) * 1.05f;
        var radiusMeters = PhysicsWorld.PixelsToMeters(config.SpriteSizePixels / 2f);
        var scale = config.SpriteSizePixels * glowCanvasScale / TextureSize;

        var variantIndex = random.Next(assets.ShapeVariants.Length);
        var unitVertices = assets.ShapeVariants[variantIndex];
        var points = new Vector2[unitVertices.Length];
        for (var p = 0; p < unitVertices.Length; p++) points[p] = unitVertices[p] * radiusMeters;

        // Solid (so it still bounces off asteroids like one), but its collision mask
        // excludes the ship specifically — the ship should fly straight through and get
        // it collected via OxygenPickupSystem's distance check, not bounce off it.
        var shapeDef = B2Api.b2DefaultShapeDef();
        shapeDef.density = config.Motion.MaterialDensity;
        shapeDef.material.restitution = config.Motion.Restitution;
        shapeDef.filter.maskBits = ~CollisionCategories.Ship;
        var hull = B2Api.b2ComputeHull(points, points.Length);
        var polygon = B2Api.b2MakePolygon(hull, 0f);
        B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);

        world.Create(
            new PhysicsBody { BodyId = bodyId },
            new Transform { PositionMeters = positionMeters, RotationRadians = rotationRadians },
            new Velocity(),
            new Sprite { Texture = assets.ShapeTextures[variantIndex], Color = Microsoft.Xna.Framework.Color.White, Size = TextureSize, Scale = scale, Parallax = 1f },
            new OxygenPickup());
    }
}
