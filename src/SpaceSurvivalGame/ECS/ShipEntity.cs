using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS;

/// <summary>Creates the player ship entity and handles the one-off respawn action.</summary>
public static class ShipEntity
{
    public static Entity Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 startPositionMeters, ShipConfig config)
    {
        var bodyDef = B2Api.b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = startPositionMeters;
        bodyDef.linearDamping = 0f; // no drag: momentum carries the ship, like real space
        bodyDef.angularDamping = 0f;
        var bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

        var shapeDef = B2Api.b2DefaultShapeDef();
        shapeDef.density = 1f;
        var halfExtent = PhysicsWorld.PixelsToMeters(config.SpriteSize / 2f);
        var box = B2Api.b2MakeBox(halfExtent, halfExtent);
        B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in box);

        var texture = ProceduralTextures.CreateRightFacingTriangle(graphicsDevice, config.SpriteSize, Microsoft.Xna.Framework.Color.White);

        return world.Create(
            new PhysicsBody { BodyId = bodyId },
            new Transform { PositionMeters = startPositionMeters, RotationRadians = 0f },
            new Velocity(),
            new Sprite { Texture = texture, Color = Microsoft.Xna.Framework.Color.White, Size = config.SpriteSize },
            new ShipMovement
            {
                ThrustAcceleration = config.ThrustAcceleration,
                MaxSpeedMetersPerSecond = config.MaxSpeedMetersPerSecond,
                TurnSpeedRadiansPerSecond = config.TurnSpeedRadiansPerSecond
            },
            new PlayerControlled());
    }

    private static readonly QueryDescription RespawnQuery =
        new QueryDescription().WithAll<PhysicsBody, PlayerControlled>();

    public static void Respawn(World world, Vector2 positionMeters)
    {
        world.Query(in RespawnQuery, (ref PhysicsBody physicsBody) =>
        {
            var bodyId = physicsBody.BodyId;
            B2Api.b2Body_SetTransform(bodyId, positionMeters, b2Rot.FromAngle(0f));
            B2Api.b2Body_SetLinearVelocity(bodyId, Vector2.Zero);
            B2Api.b2Body_SetAngularVelocity(bodyId, 0f);
        });
    }
}
