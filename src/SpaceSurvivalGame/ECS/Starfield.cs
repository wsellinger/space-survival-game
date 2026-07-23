using System;
using System.Numerics;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Config;

namespace SpaceSurvivalGame.ECS;

/// <summary>
/// Scatters static background star entities (Transform + Sprite only, no
/// physics) in a square field around a center point, just so camera movement
/// is visible before real procedural world generation exists. Finite and
/// fixed at creation time — flying far enough eventually runs out of stars.
/// Call once per layer (see StarfieldConfig) with a different parallax factor
/// per layer to fake depth: lower parallax reads as farther away.
/// </summary>
public static class Starfield
{
    public static void Create(World world, GraphicsDevice graphicsDevice, Vector2 centerMeters, float halfExtentMeters, int starCount, float parallax, Microsoft.Xna.Framework.Color color, float minTintStrength, float maxTintStrength)
    {
        var random = new Random();
        var texture = ProceduralTextures.CreateSolidSquare(graphicsDevice, 2, Microsoft.Xna.Framework.Color.White); // tinted per-layer via Sprite.Color instead of baking color into the texture

        for (var i = 0; i < starCount; i++)
        {
            var offset = new Vector2(
                (float)(random.NextDouble() * 2 - 1) * halfExtentMeters,
                (float)(random.NextDouble() * 2 - 1) * halfExtentMeters);

            world.Create(
                new Transform { PositionMeters = centerMeters + offset, RotationRadians = 0f },
                new Sprite { Texture = texture, Color = ApplyRandomTint(random, color, minTintStrength, maxTintStrength), Size = 2, Scale = 1f, LayerDepth = 1f, Parallax = parallax });
        }
    }

    /// <summary>Dims one or two channels a small random amount so the star reads as a slight red/yellow/blue cast rather than plain white, without straying far from it.</summary>
    private static Microsoft.Xna.Framework.Color ApplyRandomTint(Random random, Microsoft.Xna.Framework.Color baseColor, float minTintStrength, float maxTintStrength)
    {
        var r = baseColor.R / 255f;
        var g = baseColor.G / 255f;
        var b = baseColor.B / 255f;

        var dim = minTintStrength + (float)random.NextDouble() * (maxTintStrength - minTintStrength);

        switch (random.Next(3))
        {
            case 0: // reddish: dim green and blue
                g *= 1f - dim;
                b *= 1f - dim;
                break;
            case 1: // yellowish: dim blue only
                b *= 1f - dim;
                break;
            default: // blueish: dim red and green
                r *= 1f - dim;
                g *= 1f - dim;
                break;
        }

        return new Microsoft.Xna.Framework.Color(r, g, b, baseColor.A / 255f);
    }
}
