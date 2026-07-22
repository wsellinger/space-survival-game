using System;
using System.Numerics;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS;

/// <summary>
/// Scatters static background star entities (Transform + Sprite only, no
/// physics) in a square field around a center point, just so camera movement
/// is visible before real procedural world generation exists. Finite and
/// fixed at creation time — flying far enough eventually runs out of stars.
/// </summary>
public static class Starfield
{
    public static void Create(World world, GraphicsDevice graphicsDevice, Vector2 centerMeters, float halfExtentMeters, int starCount)
    {
        var random = new Random();
        var texture = ProceduralTextures.CreateSolidSquare(graphicsDevice, 2, Microsoft.Xna.Framework.Color.White);

        for (var i = 0; i < starCount; i++)
        {
            var offset = new Vector2(
                (float)(random.NextDouble() * 2 - 1) * halfExtentMeters,
                (float)(random.NextDouble() * 2 - 1) * halfExtentMeters);

            world.Create(
                new Transform { PositionMeters = centerMeters + offset, RotationRadians = 0f },
                new Sprite { Texture = texture, Color = Microsoft.Xna.Framework.Color.White, Size = 2 });
        }
    }
}
