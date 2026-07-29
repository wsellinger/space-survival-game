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
/// Scatters a handful of collectible iron ore pickups — small light silvery-gray rock chunks (no
/// glow, unlike the O2 pickup crystals) — at random positions within the asteroid field's
/// extent. Each is a solid dynamic Box2D body like an asteroid or O2 pickup (so it can be
/// physically bumped around by either), but its shape never opts into hit events, so it's
/// invisible to CollisionDamageSystem — collection is a simple distance check in
/// IronPickupSystem instead, not a damage-style hit event.
/// </summary>
public static class IronPickupField
{
    private const int TextureSize = 32;
    private const int ShapeVariantCount = 3;

    // Exposed publicly (like OxygenPickupField's own Crystal* constants) so AsteroidField's
    // embedded speckles use the exact same shape — fewer vertices, a higher min radius factor,
    // and lower angle jitter than the original "jagged rock" attempt read as a handful of large
    // flat facets meeting at sharp edges, rather than a chaotically spiky/crumpled silhouette.
    // Elongated (not round) for the same oblong, wedge-like profile a real hematite/ore fragment
    // has.
    public const int OreMinVerticesPerShape = 5;
    public const int OreMaxVerticesPerShape = 7;
    public const float OreMinVertexRadiusFactor = 0.55f;
    public const float OreAngleJitterFraction = 0.2f;
    public const float OreElongationFactor = 1.35f;

    // Nudged slightly behind the documented frontmost value (0, every other gameplay sprite's
    // own default) so MetallicSparkleRenderSystem's glints — drawn at the default 0 — always
    // win the SpriteSortMode.BackToFront tie against their own parent's sprite instead of it
    // being an undefined coin flip.
    private const float OreLayerDepth = 0.01f;

    /// <summary>Already-baked shape data shared by every pickup chunk, so a new one can be spawned at runtime (see SpawnPickup) without re-baking textures.</summary>
    public readonly struct PickupAssets
    {
        public readonly Vector2[][] ShapeVariants;
        public readonly Texture2D[] ShapeTextures;
        public readonly Texture2D SparkleTexture;

        public PickupAssets(Vector2[][] shapeVariants, Texture2D[] shapeTextures, Texture2D sparkleTexture)
        {
            ShapeVariants = shapeVariants;
            ShapeTextures = shapeTextures;
            SparkleTexture = sparkleTexture;
        }
    }

    public static PickupAssets Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 centerMeters, WorldConfig worldConfig, IronPickupConfig ironConfig)
    {
        var random = new Random();
        var oreColor = ColorHex.Parse(ironConfig.ColorHex);
        var rustSpotColor = ColorHex.Parse(ironConfig.RustSpots.ColorHex);

        var shapeVariants = new Vector2[ShapeVariantCount][];
        var shapeTextures = new Texture2D[ShapeVariantCount];
        for (var v = 0; v < ShapeVariantCount; v++)
        {
            var vertexCount = random.Next(OreMinVerticesPerShape, OreMaxVerticesPerShape + 1);
            shapeVariants[v] = ProceduralShapeGenerator.GenerateJitteredPolygon(random, vertexCount, OreMinVertexRadiusFactor, OreAngleJitterFraction,
                OreElongationFactor, rescaleToUnitBounds: true);

            var oreXnaVertices = new Microsoft.Xna.Framework.Vector2[shapeVariants[v].Length];
            for (var p = 0; p < oreXnaVertices.Length; p++) oreXnaVertices[p] = shapeVariants[v][p].ToXna();

            // Baked once per shape variant (not per spawned instance) — a fixed, static surface
            // stain rather than something that needs to vary chunk-to-chunk the way position/
            // physics do.
            var spots = ProceduralShapeGenerator.GenerateJaggedSpots(random, shapeVariants[v], ironConfig.RustSpots.CountRange, ironConfig.RustSpots.SizeUnitRange);

            shapeTextures[v] = ProceduralTextures.CreateSpottedPolygon(graphicsDevice, TextureSize, oreColor, oreXnaVertices, spots, rustSpotColor);
        }

        // Baked white (like ParticleEffects' own spark texture) and tinted at draw time via
        // MetallicSparkleRenderSystem, so SparkleColorHex can be re-tuned without re-baking.
        var sparkleTexture = ProceduralTextures.CreateCircle(graphicsDevice, ironConfig.Sparkle.SizePixels, Microsoft.Xna.Framework.Color.White);

        var assets = new PickupAssets(shapeVariants, shapeTextures, sparkleTexture);

        for (var i = 0; i < ironConfig.PickupCount; i++)
        {
            var positionMeters = centerMeters + new Vector2(
                (float)(random.NextDouble() * 2 - 1) * worldConfig.FieldHalfExtentMeters,
                (float)(random.NextDouble() * 2 - 1) * worldConfig.FieldHalfExtentMeters);

            SpawnPickup(world, physicsWorld, positionMeters, assets, ironConfig, random);
        }

        return assets;
    }

    /// <summary>
    /// Spawns a single new iron ore pickup at the given position, reusing already-baked shape
    /// assets (see Create/PickupAssets) rather than re-baking textures — used both by Create's
    /// initial scatter and by IronOreReleaseSystem for chunks popped loose from an iron-rich
    /// asteroid at runtime.
    /// </summary>
    public static void SpawnPickup(World world, PhysicsWorld physicsWorld, Vector2 positionMeters, PickupAssets assets, IronPickupConfig config, Random random)
    {
        var rotationRadians = (float)(random.NextDouble() * Math.PI * 2);

        var bodyDef = B2Api.b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = positionMeters;
        bodyDef.rotation = b2Rot.FromAngle(rotationRadians);

        var speed = random.NextFloat(config.Motion.SpeedMetersPerSecondRange);
        var velocityAngle = (float)(random.NextDouble() * Math.PI * 2);
        bodyDef.linearVelocity = new Vector2(MathF.Cos(velocityAngle), MathF.Sin(velocityAngle)) * speed;

        var angularSpeed = random.NextFloat(config.Motion.AngularVelocityRadiansPerSecondRange);
        bodyDef.angularVelocity = random.Next(2) == 0 ? -angularSpeed : angularSpeed;

        var bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

        var radiusMeters = PhysicsWorld.PixelsToMeters(config.SpriteSizePixels / 2f);
        var scale = (float)config.SpriteSizePixels / TextureSize;

        var variantIndex = random.Next(assets.ShapeVariants.Length);
        var unitVertices = assets.ShapeVariants[variantIndex];
        var points = new Vector2[unitVertices.Length];
        for (var p = 0; p < unitVertices.Length; p++) points[p] = unitVertices[p] * radiusMeters;

        // Solid (so it still bounces off asteroids and other pickups like one), but its
        // collision mask excludes the ship specifically — the ship should fly straight through
        // and get it collected via IronPickupSystem's distance check, not bounce off it.
        var shapeDef = B2Api.b2DefaultShapeDef();
        shapeDef.density = config.Motion.MaterialDensity;
        shapeDef.material.restitution = config.Motion.Restitution;
        shapeDef.filter.maskBits = ~CollisionCategories.Ship;
        var hull = B2Api.b2ComputeHull(points, points.Length);
        var polygon = B2Api.b2MakePolygon(hull, 0f);
        B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);

        // A handful of fixed points scattered across the chunk's own visible face (so each reads
        // as its own facet catching the light rather than one glowing bullseye), rolled once here
        // in the chunk's own unrotated local space — MetallicSparkleRenderSystem rotates each by
        // the entity's current facing every frame so they stay fixed to their facets as the chunk
        // spins, and each flares on its own independent phase.
        var (sparkleOffsetsPixels, sparklePhasesRadians) = ProceduralShapeGenerator.GenerateSparklePoints(random, unitVertices, config.Sparkle.Count, config.SpriteSizePixels / 2f);

        world.Create(
            new PhysicsBody { BodyId = bodyId },
            new Transform { PositionMeters = positionMeters, RotationRadians = rotationRadians },
            new Velocity(),
            new Sprite { Texture = assets.ShapeTextures[variantIndex], Color = Microsoft.Xna.Framework.Color.White, Size = TextureSize, Scale = scale, LayerDepth = OreLayerDepth, Parallax = 1f },
            new IronPickup(),
            new MetallicSparkle { OffsetsPixels = sparkleOffsetsPixels, PhasesRadians = sparklePhasesRadians, Texture = assets.SparkleTexture });
    }
}
