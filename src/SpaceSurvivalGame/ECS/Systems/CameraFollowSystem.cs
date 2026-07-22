using Arch.Core;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Centers the camera on the player-controlled entity. Run once per frame after PhysicsSyncSystem, so Transform is fresh.</summary>
public static class CameraFollowSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Transform, PlayerControlled>();

    public static void Run(World world, Camera camera)
    {
        world.Query(in Query, (ref Transform transform) =>
        {
            camera.PositionMeters = transform.PositionMeters;
        });
    }
}
