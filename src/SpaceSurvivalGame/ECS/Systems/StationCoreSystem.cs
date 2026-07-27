using System.Numerics;
using Arch.Core;
using SpaceSurvivalGame.ECS.Components;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// While the station core is still Attached, copies the ship's own position onto it every frame
/// so it visually rides along at the ship's center. Once the ship's Iron.Current first reaches
/// StationCoreConfig.IronAmountRequired, spends that amount and flips Attached false — from then
/// on this system leaves the core alone, so it stays fixed wherever the ship happened to be at
/// that instant, becoming an independent, stationary object.
/// </summary>
public static class StationCoreSystem
{
    private static readonly QueryDescription ShipQuery = new QueryDescription().WithAll<Transform, Iron, PlayerControlled>();
    private static readonly QueryDescription CoreQuery = new QueryDescription().WithAll<Transform, StationCore>();

    public static void Run(World world, StationCoreConfig config)
    {
        var shipEntity = Entity.Null;
        var shipPositionMeters = Vector2.Zero;
        var ironCurrent = 0f;
        var foundShip = false;
        world.Query(in ShipQuery, (Entity entity, ref Transform transform, ref Iron iron) =>
        {
            shipEntity = entity;
            shipPositionMeters = transform.PositionMeters;
            ironCurrent = iron.Current;
            foundShip = true;
        });
        if (!foundShip) return;

        world.Query(in CoreQuery, (ref Transform coreTransform, ref StationCore core) =>
        {
            if (!core.Attached) return;

            if (ironCurrent >= config.IronAmountRequired)
            {
                core.Attached = false;
                world.Get<Iron>(shipEntity).Current -= config.IronAmountRequired;
            }
            else
            {
                coreTransform.PositionMeters = shipPositionMeters;
            }
        });
    }
}
