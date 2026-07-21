using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Copies each physics-linked entity's Box2D state into its Transform/Velocity components. Run once per frame after the physics step.</summary>
public static class PhysicsSyncSystem
{
    private static readonly QueryDescription Query =
        new QueryDescription().WithAll<PhysicsBody, Transform, Velocity>();

    public static void Run(World world)
    {
        world.Query(in Query, (ref PhysicsBody physicsBody, ref Transform transform, ref Velocity velocity) =>
        {
            var bodyId = physicsBody.BodyId;
            transform.PositionMeters = B2Api.b2Body_GetPosition(bodyId);
            transform.RotationRadians = B2Api.b2Body_GetRotation(bodyId).GetAngle();
            velocity.LinearMetersPerSecond = B2Api.b2Body_GetLinearVelocity(bodyId);
            velocity.AngularRadiansPerSecond = B2Api.b2Body_GetAngularVelocity(bodyId);
        });
    }
}
