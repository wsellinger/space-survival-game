using System;
using System.Numerics;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

using SpaceSurvivalGame.Config;

namespace SpaceSurvivalGame.ECS;

/// <summary>Spawns short-lived, physics-free spark particles at a world position, e.g. for collision impact or pickup-collection feedback.</summary>
public static class ParticleEffects
{
    public static void SpawnSparkBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, ParticleConfig config) =>
        SpawnBurst(world, sparkTexture, positionMeters, random, config,
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

    private static void SpawnBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, ParticleConfig config,
        Microsoft.Xna.Framework.Color colorA, Microsoft.Xna.Framework.Color colorB)
    {
        var count = random.Next(config.SparkCountMin, config.SparkCountMax + 1);
        for (var i = 0; i < count; i++)
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            var speed = config.SparkSpeedMinMetersPerSecond + (float)random.NextDouble() * (config.SparkSpeedMaxMetersPerSecond - config.SparkSpeedMinMetersPerSecond);
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            var lifetime = config.SparkLifetimeMinSeconds + (float)random.NextDouble() * (config.SparkLifetimeMaxSeconds - config.SparkLifetimeMinSeconds);
            var color = random.Next(2) == 0 ? colorA : colorB;

            world.Create(
                new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
                new Velocity { LinearMetersPerSecond = velocity, AngularRadiansPerSecond = 0f },
                new Sprite { Texture = sparkTexture, Color = color, Size = config.SparkTextureSizePixels, Scale = 1f, LayerDepth = 0f, Parallax = 1f },
                new Particle { RemainingSeconds = lifetime, TotalSeconds = lifetime, BaseColor = color });
        }
    }
}
