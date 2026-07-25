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
            config.SparkCountRange.Min, config.SparkCountRange.Max,
            config.SparkSpeedMetersPerSecondRange.Min, config.SparkSpeedMetersPerSecondRange.Max,
            config.SparkLifetimeSecondsRange.Min, config.SparkLifetimeSecondsRange.Max, config.SparkTextureSizePixels,
            new Microsoft.Xna.Framework.Color(255, 140, 0),   // orange
            new Microsoft.Xna.Framework.Color(255, 215, 60)); // yellow

    /// <summary>Independently tunable from regular collision taps (DeathSequenceConfig rather than ParticleConfig) so the death explosion can be made bigger/longer-lived without affecting ordinary hit sparks.</summary>
    public static void SpawnExplosionBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, DeathSequenceConfig config) =>
        SpawnBurst(world, sparkTexture, positionMeters, random,
            config.Explosion.SparkCountRange.Min, config.Explosion.SparkCountRange.Max,
            config.Explosion.SparkSpeedMetersPerSecondRange.Min, config.Explosion.SparkSpeedMetersPerSecondRange.Max,
            config.Explosion.SparkLifetimeSecondsRange.Min, config.Explosion.SparkLifetimeSecondsRange.Max, config.Explosion.SparkSizePixels,
            new Microsoft.Xna.Framework.Color(255, 140, 0),   // orange
            new Microsoft.Xna.Framework.Color(255, 215, 60)); // yellow

    /// <summary>
    /// Unlike SpawnSparkBurst, these start on a ring around positionMeters and move inward,
    /// timed so each one arrives (and fades out) right at the center — reads as being drawn
    /// in/absorbed rather than exploding outward like a collision impact.
    /// </summary>
    public static void SpawnPickupBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, ParticleConfig config)
    {
        var count = random.Next(config.SparkCountRange.Min, config.SparkCountRange.Max + 1);
        for (var i = 0; i < count; i++)
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            var speed = config.SparkSpeedMetersPerSecondRange.Min + (float)random.NextDouble() * (config.SparkSpeedMetersPerSecondRange.Max - config.SparkSpeedMetersPerSecondRange.Min);
            var lifetime = config.SparkLifetimeSecondsRange.Min + (float)random.NextDouble() * (config.SparkLifetimeSecondsRange.Max - config.SparkLifetimeSecondsRange.Min);
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

    /// <summary>
    /// A small handful of short-lived particles puffing outward from a hull mount point, spread
    /// randomly around outwardDirection — see RotationJetSystem, which calls this once per
    /// active jet per frame while the ship is turning.
    /// </summary>
    public static void SpawnRotationJetPuff(World world, Texture2D particleTexture, Vector2 positionMeters, Vector2 outwardDirection,
        Random random, RotationJetConfig config, Microsoft.Xna.Framework.Color color)
    {
        var count = random.Next(config.ParticleCountPerFrame.Min, config.ParticleCountPerFrame.Max + 1);
        var spreadRadians = config.SpreadAngleDegrees * MathF.PI / 180f;
        var baseAngle = MathF.Atan2(outwardDirection.Y, outwardDirection.X);

        for (var i = 0; i < count; i++)
        {
            var angle = baseAngle + ((float)random.NextDouble() * 2f - 1f) * spreadRadians;
            var speed = config.ParticleSpeedMetersPerSecondRange.Min +
                        (float)random.NextDouble() * (config.ParticleSpeedMetersPerSecondRange.Max - config.ParticleSpeedMetersPerSecondRange.Min);
            var lifetime = config.ParticleLifetimeSecondsRange.Min +
                           (float)random.NextDouble() * (config.ParticleLifetimeSecondsRange.Max - config.ParticleLifetimeSecondsRange.Min);
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            world.Create(
                new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
                new Velocity { LinearMetersPerSecond = velocity, AngularRadiansPerSecond = 0f },
                new Sprite { Texture = particleTexture, Color = color, Size = config.ParticleSizePixels, Scale = 1f, LayerDepth = 0f, Parallax = 1f },
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
