using System;
using System.Numerics;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS;

/// <summary>
/// Generates a small set of shared hull-fragment shard textures once at startup, and spawns
/// tumbling debris from them when the ship dies from damage. Fragments are physics-free
/// (Particle+Transform+Velocity+Sprite, aged by ParticleSystem exactly like sparks) — they
/// inherit the ship's own velocity plus an outward kick, and spin via Velocity.AngularRadiansPerSecond.
/// </summary>
public static class ShipFragments
{
    private const int FragmentTextureSize = 16;
    private const int ShapeVariantCount = 3;

    public static Texture2D[] CreateFragmentTextures(GraphicsDevice graphicsDevice, Random random)
    {
        var textures = new Texture2D[ShapeVariantCount];
        for (var v = 0; v < textures.Length; v++)
        {
            var vertices = GenerateShardVertices(random);
            textures[v] = ProceduralTextures.CreatePolygon(graphicsDevice, FragmentTextureSize, Microsoft.Xna.Framework.Color.White, vertices);
        }

        return textures;
    }

    public static void SpawnDebris(World world, Texture2D[] fragmentTextures, Vector2 positionMeters, Vector2 shipVelocityMetersPerSecond, Random random, DeathSequenceConfig config)
    {
        var lifetime = config.FadeDelaySeconds + config.FadeDurationSeconds;
        var scale = config.FragmentSizePixels / (float)FragmentTextureSize;

        var shipSpeed = shipVelocityMetersPerSecond.Length();
        var baseAngle = shipSpeed > 0.01f
            ? MathF.Atan2(shipVelocityMetersPerSecond.Y, shipVelocityMetersPerSecond.X)
            : (float)(random.NextDouble() * Math.PI * 2);

        for (var i = 0; i < config.ShipFragmentCount; i++)
        {
            var texture = fragmentTextures[random.Next(fragmentTextures.Length)];

            // Rotates the ship's own travel direction by the spread (rather than adding a small
            // independent kick vector) so the spread is actually visible regardless of how fast
            // the ship was going — a small kick added to a large ship-speed vector barely
            // changes the resulting direction at all.
            var fragmentAngle = baseAngle + ((float)random.NextDouble() * 2f - 1f) * config.FragmentSpreadAngleRadians;
            var kickSpeed = config.FragmentMinSpeedMetersPerSecond + (float)random.NextDouble() * (config.FragmentMaxSpeedMetersPerSecond - config.FragmentMinSpeedMetersPerSecond);
            var velocity = new Vector2(MathF.Cos(fragmentAngle), MathF.Sin(fragmentAngle)) * (shipSpeed + kickSpeed);

            var angularSpeed = config.FragmentMinAngularVelocityRadiansPerSecond +
                                (float)random.NextDouble() * (config.FragmentMaxAngularVelocityRadiansPerSecond - config.FragmentMinAngularVelocityRadiansPerSecond);
            var angularVelocity = random.Next(2) == 0 ? -angularSpeed : angularSpeed;

            world.Create(
                new Transform { PositionMeters = positionMeters, RotationRadians = (float)(random.NextDouble() * Math.PI * 2) },
                new Velocity { LinearMetersPerSecond = velocity, AngularRadiansPerSecond = angularVelocity },
                new Sprite { Texture = texture, Color = Microsoft.Xna.Framework.Color.White, Size = FragmentTextureSize, Scale = scale, Parallax = 1f },
                new Particle { RemainingSeconds = lifetime, TotalSeconds = lifetime, BaseColor = Microsoft.Xna.Framework.Color.White });
        }
    }

    /// <summary>A small irregular triangle in unit (-1..1) space — a rough hull shard, not a precise partition of the ship's own triangle.</summary>
    private static Microsoft.Xna.Framework.Vector2[] GenerateShardVertices(Random random)
    {
        var vertices = new Microsoft.Xna.Framework.Vector2[3];
        for (var i = 0; i < 3; i++)
        {
            var angle = (float)(i * (Math.PI * 2 / 3) + random.NextDouble() * 0.6 - 0.3);
            var radius = 0.6f + (float)random.NextDouble() * 0.4f;
            vertices[i] = new Microsoft.Xna.Framework.Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }

        return vertices;
    }
}
