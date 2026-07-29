using System;
using System.Numerics;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS;

/// <summary>Spawns short-lived, physics-free spark particles and text popups at a world position, e.g. for collision impact, pickup-collection, or death-explosion feedback.</summary>
public static class ParticleEffects
{
    public static void SpawnSparkBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, SparkConfig config) =>
        SpawnBurst(world, sparkTexture, positionMeters, random,
            config.Burst.CountRange.Min, config.Burst.CountRange.Max,
            config.Burst.SpeedMetersPerSecondRange.Min, config.Burst.SpeedMetersPerSecondRange.Max,
            config.Burst.LifetimeSecondsRange.Min, config.Burst.LifetimeSecondsRange.Max, config.Burst.SizePixels,
            new Microsoft.Xna.Framework.Color(255, 140, 0),   // orange
            new Microsoft.Xna.Framework.Color(255, 215, 60)); // yellow

    /// <summary>Independently tunable from regular collision taps (DeathSequenceConfig rather than SparkConfig) so the death explosion can be made bigger/longer-lived without affecting ordinary hit sparks.</summary>
    public static void SpawnExplosionBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, DeathSequenceConfig config) =>
        SpawnBurst(world, sparkTexture, positionMeters, random,
            config.Explosion.Burst.CountRange.Min, config.Explosion.Burst.CountRange.Max,
            config.Explosion.Burst.SpeedMetersPerSecondRange.Min, config.Explosion.Burst.SpeedMetersPerSecondRange.Max,
            config.Explosion.Burst.LifetimeSecondsRange.Min, config.Explosion.Burst.LifetimeSecondsRange.Max, config.Explosion.Burst.SizePixels,
            new Microsoft.Xna.Framework.Color(255, 140, 0),   // orange
            new Microsoft.Xna.Framework.Color(255, 215, 60)); // yellow

    /// <summary>
    /// Unlike SpawnSparkBurst, these start on a ring around positionMeters and move inward,
    /// timed so each one arrives (and fades out) right at the center — reads as being drawn
    /// in/absorbed rather than exploding outward like a collision impact. colorA/colorB let
    /// each pickup type (O2, iron, ...) use its own look rather than a fixed color.
    /// </summary>
    public static void SpawnPickupBurst(World world, Texture2D sparkTexture, Vector2 positionMeters, Random random, SparkConfig config,
        Microsoft.Xna.Framework.Color colorA, Microsoft.Xna.Framework.Color colorB)
    {
        var count = random.NextInt(config.Burst.CountRange);
        for (var i = 0; i < count; i++)
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            var speed = random.NextFloat(config.Burst.SpeedMetersPerSecondRange);
            var lifetime = random.NextFloat(config.Burst.LifetimeSecondsRange);
            var color = random.Next(2) == 0 ? colorA : colorB;

            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var startRadius = speed * lifetime; // covers exactly this distance over its lifetime, so it arrives right as it fades out
            var spawnPosition = positionMeters + direction * startRadius;
            var velocity = -direction * speed;

            world.Create(
                new Transform { PositionMeters = spawnPosition, RotationRadians = 0f },
                new Velocity { LinearMetersPerSecond = velocity, AngularRadiansPerSecond = 0f },
                new Sprite { Texture = sparkTexture, Color = color, Size = config.Burst.SizePixels, Scale = 1f, LayerDepth = 0f, Parallax = 1f },
                new Particle { RemainingSeconds = lifetime, TotalSeconds = lifetime, BaseColor = color });
        }
    }

    /// <summary>
    /// A "+N Resource" text popup (see FloatingText/FloatingTextSystem/FloatingTextRenderSystem)
    /// that rises straight up in screen space from screenPositionPixels and fades out over
    /// config.DurationSeconds — used by OxygenPickupSystem/IronPickupSystem to confirm how much
    /// of what was just collected, in that resource's own configured color. Deliberately
    /// screen-space (not world-space) so the rise stays a constant, camera-independent pixel
    /// rate — see the FloatingText component's own doc comment.
    /// </summary>
    public static void SpawnFloatingText(World world, Microsoft.Xna.Framework.Vector2 screenPositionPixels, string text, Microsoft.Xna.Framework.Color color, FloatingTextConfig config) =>
        world.Create(new FloatingText
        {
            Text = text,
            Color = color,
            ScreenPositionPixels = screenPositionPixels,
            RiseSpeedPixelsPerSecond = config.RiseSpeedPixelsPerSecond,
            RemainingSeconds = config.DurationSeconds,
            TotalSeconds = config.DurationSeconds
        });

    /// <summary>
    /// A small handful of short-lived particles puffing outward from a mount point, spread
    /// randomly around outwardDirection — see RotationJetSystem (fired from the ship's hull while
    /// turning) and StationCoreSystem.ApplyDriftImpulse (fired from the landed core's edges
    /// whenever a drift impulse fires). Takes raw values rather than a shared config type since
    /// the two callers each have their own differently-shaped config class.
    /// </summary>
    public static void SpawnRotationJetPuff(World world, Texture2D particleTexture, Vector2 positionMeters, Vector2 outwardDirection,
        Random random, IntRange particleCountPerFrame, FloatRange particleSpeedMetersPerSecondRange, FloatRange particleLifetimeSecondsRange,
        int particleSizePixels, float spreadAngleDegrees, Microsoft.Xna.Framework.Color color)
    {
        var count = random.NextInt(particleCountPerFrame);
        var spreadRadians = spreadAngleDegrees * MathF.PI / 180f;
        var baseAngle = MathF.Atan2(outwardDirection.Y, outwardDirection.X);

        for (var i = 0; i < count; i++)
        {
            var angle = baseAngle + ((float)random.NextDouble() * 2f - 1f) * spreadRadians;
            var speed = random.NextFloat(particleSpeedMetersPerSecondRange);
            var lifetime = random.NextFloat(particleLifetimeSecondsRange);
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            world.Create(
                new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
                new Velocity { LinearMetersPerSecond = velocity, AngularRadiansPerSecond = 0f },
                new Sprite { Texture = particleTexture, Color = color, Size = particleSizePixels, Scale = 1f, LayerDepth = 0f, Parallax = 1f },
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
