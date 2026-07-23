using System;
using System.Numerics;
using Box2dNet.Interop;

namespace SpaceSurvivalGame.Physics;

/// <summary>
/// Owns the Box2D world and the meters&lt;-&gt;pixels conversion, since Box2D's solver
/// is tuned for objects roughly 0.1-10 units across and misbehaves if you feed it
/// raw screen-pixel magnitudes (hundreds/thousands of units).
/// </summary>
public sealed class PhysicsWorld : IDisposable
{
    public const float PixelsPerMeter = 100f;

    public b2WorldId WorldId { get; }

    public PhysicsWorld()
    {
        var worldDef = B2Api.b2DefaultWorldDef();
        worldDef.gravity = Vector2.Zero; // open space: nothing pulls the ship around
        WorldId = B2Api.b2CreateWorld(worldDef);

        // Box2D's default hit-event threshold filters out slow impacts entirely; we want
        // even a trivial tap to generate a hit event so CollisionDamageSystem's own
        // min-speed/min-damage config is what decides "this barely counts", not Box2D.
        B2Api.b2World_SetHitEventThreshold(WorldId, 0f);
    }

    public void Step(float deltaSeconds)
    {
        const int subStepCount = 4;
        B2Api.b2World_Step(WorldId, deltaSeconds, subStepCount);
    }

    public static float MetersToPixels(float meters) => meters * PixelsPerMeter;
    public static float PixelsToMeters(float pixels) => pixels / PixelsPerMeter;
    public static Vector2 MetersToPixels(Vector2 meters) => meters * PixelsPerMeter;
    public static Vector2 PixelsToMeters(Vector2 pixels) => pixels / PixelsPerMeter;

    public void Dispose() => B2Api.b2DestroyWorld(WorldId);
}
