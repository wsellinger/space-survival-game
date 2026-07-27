using System.Numerics;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS;

/// <summary>
/// Creates the single station core entity, spawned once at game start already Attached — riding
/// at the ship's own center until StationCoreSystem detaches it (see StationCore.Attached).
/// Plain default (0, frontmost) LayerDepth so it draws on top of the ship's own sprite, which is
/// nudged slightly back for exactly this reason (see ShipEntity.Create).
/// </summary>
public static class StationCoreEntity
{
    public static void Create(World world, Vector2 positionMeters, Texture2D texture, int spriteSizePixels)
    {
        world.Create(
            new Transform { PositionMeters = positionMeters, RotationRadians = 0f },
            new Sprite { Texture = texture, Color = Microsoft.Xna.Framework.Color.White, Size = spriteSizePixels, Scale = 1f, Parallax = 1f },
            new StationCore { Attached = true });
    }
}
