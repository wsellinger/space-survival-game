using System;
using System.Numerics;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS;

/// <summary>Spawns short-lived, physics-free spark particles at a world position, e.g. for collision impact, pickup-collection, or death-explosion feedback.</summary>
public static class ParticleEffects
{
    public static void SpawnSparkBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, ParticleConfig config) =>
        SpawnBurst(world, sparkTexture, positionMeters, random,
            config.SparkCountMin, config.SparkCountMax, config.SparkSpeedMinMetersPerSecond, config.SparkSpeedMaxMetersPerSecond,
            config.SparkLifetimeMinSeconds, config.SparkLifetimeMaxSeconds, config.SparkTextureSizePixels,
            new Microsoft.Xna.Framework.Color(255, 140, 0),   // orange
            new Microsoft.Xna.Framework.Color(255, 215, 60)); // yellow

    /// <summary>Independently tunable from regular collision taps (DeathSequenceConfig rather than ParticleConfig) so the death explosion can be made bigger/longer-lived without affecting ordinary hit sparks.</summary>
    public static void SpawnExplosionBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, DeathSequenceConfig config) =>
        SpawnBurst(world, sparkTexture, positionMeters, random,
            config.ExplosionSparkCountMin, config.ExplosionSparkCountMax, config.ExplosionSparkSpeedMinMetersPerSecond, config.ExplosionSparkSpeedMaxMetersPerSecond,
            config.ExplosionSparkLifetimeMinSeconds, config.ExplosionSparkLifetimeMaxSeconds, config.ExplosionSparkSizePixels,
            new Microsoft.Xna.Framework.Color(255, 140, 0),   // orange
            new Microsoft.Xna.Framework.Color(255, 215, 60)); // yellow

    /// <summary>
    /// Unlike SpawnSparkBurst, these start on a ring around positionMeters and move inward,
    /// timed so each one arrives (and fades out) right at the center — reads as being drawn
    /// in/absorbed rather than exploding outward like a collision impact.
    /// </summary>
    public static void SpawnPickupBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, ParticleConfig config)
    {
        var count = random.Next(config.SparkCountMin, config.SparkCountMax + 1);
        for (var i = 0; i < count; i++)
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            var speed = config.SparkSpeedMinMetersPerSecond + (float)random.NextDouble() * (config.SparkSpeedMaxMetersPerSecond - config.SparkSpeedMinMetersPerSecond);
            var lifetime = config.SparkLifetimeMinSeconds + (float)random.NextDouble() * (config.SparkLifetimeMaxSeconds - config.SparkLifetimeMinSeconds);
            var color = random.Next(2) == 0 ? Microsoft.Xna.Framework.Color.CornflowerBlue : Microsoft.Xna.Framework.Color.White;

            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var startRadius = speed * lifetime; // covers exactly this distance over its lifetime, so it arrives right as it fades out
            var spawnPosition = positionMeters + direction * startRadius;
            var velocity = -direction * speed;

            world.Create(
                new Transform { PositionMeters = spawnPosition, RotationRadians = 0f },
                new Velocity { LinearMetersPerSecond = velocity, AngularRadiansPerSecond = 0f },
                new Sprite { Texture = sparkTexture, Color = color, Size = config.SparkTextureSizePixels, Scale = 1f, LayerDepth = 0f, Parallax = 1f },
                new Particle { RemainingSeconds = lifetime, TotalSeconds = lifetime, BaseColor = color });
        }
    }

    private static void SpawnBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random,
        int countMin, int countMax, float speedMin, float speedMax, float lifetimeMin, float lifetimeMax, int textureSizePixels,
        Microsoft.Xna.Framework.Color colorA, Microsoft.Xna.Framework.Color colorB)
    {
        var count = random.Next(countMin, countMax + 1);
        for (var i = 0; i < count; i++)
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            var speed = speedMin + (float)random.NextDouble() * (speedMax - speedMin);
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            var lifetime = lifetimeMin + (float)random.NextDouble() * (lifetimeMax - lifetimeMin);
            var color = random.Next(2) == 0 ? colorA : colorB;

            world.Create(
                new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
                new Velocity { LinearMetersPerSecond = velocity, AngularRadiansPerSecond = 0f },
                new Sprite { Texture = sparkTexture, Color = color, Size = textureSizePixels, Scale = 1f, LayerDepth = 0f, Parallax = 1f },
                new Particle { RemainingSeconds = lifetime, TotalSeconds = lifetime, BaseColor = color });
        }
    }
}
