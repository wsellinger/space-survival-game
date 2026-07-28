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
            new StationCore { Attached = true, ShockwaveElapsedSeconds = -1f });
    }

    private static readonly QueryDescription HideQuery = new QueryDescription().WithAll<StationCore, Sprite>();

    /// <summary>Hides the core's own dot sprite for the ship's collision death sequence, but only while it's still Attached — once detached it's already its own independently built object, so the ship dying shouldn't make it vanish too.</summary>
    public static void Hide(World world)
    {
        world.Query(in HideQuery, (ref StationCore core, ref Sprite sprite) =>
        {
            if (core.Attached) sprite.Color = Microsoft.Xna.Framework.Color.Transparent;
        });
    }

    /// <summary>Undoes Hide on respawn — same Attached guard, so an already-detached core (never hidden in the first place) is left untouched.</summary>
    public static void Show(World world)
    {
        world.Query(in HideQuery, (ref StationCore core, ref Sprite sprite) =>
        {
            if (core.Attached) sprite.Color = Microsoft.Xna.Framework.Color.White;
        });
    }
}
