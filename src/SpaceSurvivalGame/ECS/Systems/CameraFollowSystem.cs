using System.Numerics;
using Arch.Core;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Sets the camera's target to the player-controlled entity's position plus
/// lookAheadOffsetMeters (zero vector when not aiming with the right stick),
/// then eases the camera's actual position toward that target. The offset is
/// cast straight from the right stick's own direction, not the ship's facing —
/// facing can lag behind the stick (it turns at a capped rate), which made the
/// camera's target visibly wobble as the ship caught up. Run once per frame
/// after PhysicsSyncSystem, so Transform is fresh.
/// </summary>
public static class CameraFollowSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Transform, PlayerControlled>();

    public static void Run(World world, Camera camera, Vector2 lookAheadOffsetMeters, float deltaSeconds, float smoothingSpeed)
    {
        world.Query(in Query, (ref Transform transform) =>
        {
            camera.TargetPositionMeters = transform.PositionMeters + lookAheadOffsetMeters;
        });

        camera.MoveTowardTarget(deltaSeconds, smoothingSpeed);
    }

    /// <summary>Fetches the player-controlled entity's current world position — e.g. for computing a mouse-relative look-ahead offset that needs the ship's on-screen position.</summary>
    public static bool TryGetShipPositionMeters(World world, out Vector2 positionMeters)
    {
        var found = false;
        var result = Vector2.Zero;
        world.Query(in Query, (ref Transform transform) =>
        {
            result = transform.PositionMeters;
            found = true;
        });

        positionMeters = result;
        return found;
    }
}
