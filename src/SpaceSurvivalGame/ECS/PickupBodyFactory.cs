using System;
using System.Numerics;
using Box2dNet.Interop;
using SpaceSurvivalGame.Physics;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS;

/// <summary>
/// The dynamic Box2D body + convex-hull polygon shape setup shared by every pickup spawn
/// (IronPickupField/OxygenPickupField): random rotation/velocity/spin from a PickupMotionConfig,
/// then a hull shape whose collision mask excludes the ship specifically (so the ship flies
/// straight through and collects it via a distance check instead of bouncing off it). Sprite and
/// any resource-specific extra components (e.g. MetallicSparkle) are left to the caller, since
/// those genuinely differ per pickup type.
/// </summary>
public static class PickupBodyFactory
{
    public static b2BodyId CreateBody(PhysicsWorld physicsWorld, Vector2 positionMeters, float rotationRadians,
        Vector2[] unitVertices, float radiusMeters, PickupMotionConfig motion, Random random)
    {
        var speed = random.NextFloat(motion.SpeedMetersPerSecondRange);
        var velocityAngle = (float)(random.NextDouble() * Math.PI * 2);

        var angularSpeed = random.NextFloat(motion.AngularVelocityRadiansPerSecondRange);
        angularSpeed = random.Next(2) == 0 ? -angularSpeed : angularSpeed;

        var bodyId = PhysicsBodyFactory.CreateDynamicBody(physicsWorld, positionMeters, rotationRadians,
            linearVelocityMetersPerSecond: new Vector2(MathF.Cos(velocityAngle), MathF.Sin(velocityAngle)) * speed,
            angularVelocityRadiansPerSecond: angularSpeed);

        var points = new Vector2[unitVertices.Length];
        for (var p = 0; p < unitVertices.Length; p++) points[p] = unitVertices[p] * radiusMeters;

        PhysicsBodyFactory.CreateHullPolygonShape(bodyId, points, motion.MaterialDensity, motion.Restitution,
            maskBits: ~CollisionCategories.Ship);

        return bodyId;
    }
}
