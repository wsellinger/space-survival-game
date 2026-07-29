using System.Numerics;
using Box2dNet.Interop;

namespace SpaceSurvivalGame.Physics;

/// <summary>
/// Collapses the "bodyDef -> create body -> shapeDef -> create shape" skeleton repeated by every
/// dynamic Box2D entity factory (ship, station core, asteroids, pickups) into two composable
/// steps. Callers that need something a plain hull polygon can't express (the station core's
/// square shape) just create their own shape after calling CreateDynamicBody.
/// </summary>
public static class PhysicsBodyFactory
{
    public static b2BodyId CreateDynamicBody(PhysicsWorld physicsWorld, Vector2 positionMeters, float rotationRadians,
        Vector2 linearVelocityMetersPerSecond, float angularVelocityRadiansPerSecond, float linearDamping = 0f, float angularDamping = 0f)
    {
        var bodyDef = B2Api.b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = positionMeters;
        bodyDef.rotation = b2Rot.FromAngle(rotationRadians);
        bodyDef.linearVelocity = linearVelocityMetersPerSecond;
        bodyDef.angularVelocity = angularVelocityRadiansPerSecond;
        bodyDef.linearDamping = linearDamping;
        bodyDef.angularDamping = angularDamping;
        return B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);
    }

    /// <summary>Attaches a convex-hull polygon shape (in local meters, relative to the body's own origin) to an already-created body.</summary>
    public static void CreateHullPolygonShape(b2BodyId bodyId, Vector2[] pointsMeters, float density, float restitution,
        bool enableHitEvents = false, ulong? categoryBits = null, ulong? maskBits = null)
    {
        var shapeDef = BuildShapeDef(density, restitution, enableHitEvents, categoryBits, maskBits);
        var hull = B2Api.b2ComputeHull(pointsMeters, pointsMeters.Length);
        var polygon = B2Api.b2MakePolygon(hull, 0f);
        B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);
    }

    /// <summary>Attaches a fixed square shape (used only by the station core, whose bounding box is a config-driven max size rather than a hull) to an already-created body.</summary>
    public static void CreateSquareShape(b2BodyId bodyId, float halfWidthMeters, float density, float restitution)
    {
        var shapeDef = BuildShapeDef(density, restitution, enableHitEvents: false, categoryBits: null, maskBits: null);
        var square = B2Api.b2MakeSquare(halfWidthMeters);
        B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in square);
    }

    private static b2ShapeDef BuildShapeDef(float density, float restitution, bool enableHitEvents, ulong? categoryBits, ulong? maskBits)
    {
        var shapeDef = B2Api.b2DefaultShapeDef();
        shapeDef.density = density;
        shapeDef.material.restitution = restitution;
        shapeDef.enableHitEvents = enableHitEvents;
        if (categoryBits.HasValue) shapeDef.filter.categoryBits = categoryBits.Value;
        if (maskBits.HasValue) shapeDef.filter.maskBits = maskBits.Value;
        return shapeDef;
    }
}
