using System;
using System.Numerics;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS;

/// <summary>Spawns short-lived, physics-free spark particles at a world position, e.g. for collision impact feedback.</summary>
public static class ParticleEffects
{
    public static void SpawnSparkBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, ParticleConfig config)
    {
        var count = random.Next(config.SparkCountMin, config.SparkCountMax + 1);
        for (var i = 0; i < count; i++)
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            var speed = config.SparkSpeedMinMetersPerSecond + (float)random.NextDouble() * (config.SparkSpeedMaxMetersPerSecond - config.SparkSpeedMinMetersPerSecond);
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            var lifetime = config.SparkLifetimeMinSeconds + (float)random.NextDouble() * (config.SparkLifetimeMaxSeconds - config.SparkLifetimeMinSeconds);
            var color = random.Next(2) == 0
                ? new Microsoft.Xna.Framework.Color(255, 140, 0)   // orange
                : new Microsoft.Xna.Framework.Color(255, 215, 60); // yellow

            world.Create(
                new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
                new Velocity { LinearMetersPerSecond = velocity, AngularRadiansPerSecond = 0f },
                new Sprite { Texture = sparkTexture, Color = color, Size = config.SparkTextureSizePixels, Scale = 1f, LayerDepth = 0f, Parallax = 1f },
                new Particle { RemainingSeconds = lifetime, TotalSeconds = lifetime, BaseColor = color });
        }
    }
}
