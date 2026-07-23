using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Reads Box2D hit events generated during the last PhysicsWorld.Step call and applies
/// damage to the ship's Health when one of them involves the ship's body. Only the ship's
/// shape has enableHitEvents set (see ShipEntity.Create), so every hit event this system
/// sees necessarily involves the ship — asteroid-vs-asteroid impacts never generate one.
/// Damage is a linear map from the event's approach speed to HP, clamped at both ends by
/// PlayerConfig's Min/MaxCollisionSpeedMetersPerSecond and Min/MaxCollisionDamage.
/// Must run after PhysicsWorld.Step (hit events are only populated post-step) and before
/// the next Step call overwrites them.
/// </summary>
public static class CollisionDamageSystem
{
    private static readonly QueryDescription ShipQuery =
        new QueryDescription().WithAll<PhysicsBody, PlayerControlled, Health>();

    public static void Run(World world, PhysicsWorld physicsWorld, PlayerConfig config)
    {
        var shipBodyId = default(b2BodyId);
        var foundShip = false;
        world.Query(in ShipQuery, (ref PhysicsBody physicsBody) =>
        {
            shipBodyId = physicsBody.BodyId;
            foundShip = true;
        });
        if (!foundShip) return;

        var contactEvents = B2Api.b2World_GetContactEvents(physicsWorld.WorldId);
        var totalDamage = 0f;

        foreach (var hitEvent in contactEvents.hitEventsAsSpan)
        {
            var bodyA = B2Api.b2Shape_GetBody(hitEvent.shapeIdA);
            var bodyB = B2Api.b2Shape_GetBody(hitEvent.shapeIdB);
            if (!BodyIdEquals(bodyA, shipBodyId) && !BodyIdEquals(bodyB, shipBodyId)) continue;

            var speedFraction = (hitEvent.approachSpeed - config.MinCollisionSpeedMetersPerSecond) /
                                 (config.MaxCollisionSpeedMetersPerSecond - config.MinCollisionSpeedMetersPerSecond);
            speedFraction = System.Math.Clamp(speedFraction, 0f, 1f);
            totalDamage += config.MinCollisionDamage + speedFraction * (config.MaxCollisionDamage - config.MinCollisionDamage);
        }

        if (totalDamage <= 0f) return;

        world.Query(in ShipQuery, (ref Health health) =>
        {
            health.Current = System.Math.Clamp(health.Current - totalDamage, 0f, health.Max);
        });
    }

    private static bool BodyIdEquals(b2BodyId a, b2BodyId b) =>
        a.index1 == b.index1 && a.world0 == b.world0 && a.generation == b.generation;
}
