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
    public static Entity Create(World world, PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 startPositionMeters, ShipConfig config, PlayerConfig playerConfig)
    {
        var bodyDef = B2Api.b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = startPositionMeters;
        bodyDef.linearDamping = 0f; // no drag: momentum carries the ship, like real space
        bodyDef.angularDamping = 0f;
        var bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

        var shapeDef = B2Api.b2DefaultShapeDef();
        shapeDef.density = 1f;
        shapeDef.enableHitEvents = true; // only one shape in a collision needs this set for CollisionDamageSystem to see it
        shapeDef.filter.categoryBits = CollisionCategories.Ship; // lets OxygenPickupField exclude just the ship from its collision mask

        // Matches ProceduralTextures.CreateRightFacingTriangle's vertex layout (tip at
        // (size-1, size/2), tail corners at (0,0)/(0,size-1)) relative to the sprite's
        // center — same center RenderSystem uses as the rotation origin — so the collider
        // actually matches the visible triangle instead of a bounding box around it.
        var halfSize = config.SpriteSize / 2f;
        var trianglePointsMeters = new[]
        {
            PhysicsWorld.PixelsToMeters(new Vector2(halfSize - 1f, 0f)),
            PhysicsWorld.PixelsToMeters(new Vector2(-halfSize, -halfSize)),
            PhysicsWorld.PixelsToMeters(new Vector2(-halfSize, halfSize - 1f))
        };
        var hull = B2Api.b2ComputeHull(trianglePointsMeters, trianglePointsMeters.Length);
        var triangle = B2Api.b2MakePolygon(hull, 0f);
        B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in triangle);

        var texture = ProceduralTextures.CreateRightFacingTriangle(graphicsDevice, config.SpriteSize, Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Red);

        return world.Create(
            new PhysicsBody { BodyId = bodyId },
            new Transform { PositionMeters = startPositionMeters, RotationRadians = 0f },
            new Velocity(),
            new Sprite { Texture = texture, Color = Microsoft.Xna.Framework.Color.White, Size = config.SpriteSize, Scale = 1f, Parallax = 1f },
            new ShipMovement
            {
                ThrustAcceleration = config.ThrustAcceleration,
                MaxSpeedMetersPerSecond = config.MaxSpeedMetersPerSecond,
                TurnSpeedRadiansPerSecond = config.TurnSpeedRadiansPerSecond
            },
            new Health { Current = playerConfig.MaxHealth, Max = playerConfig.MaxHealth },
            new Oxygen { Current = playerConfig.MaxOxygen, Max = playerConfig.MaxOxygen },
            new HitFlash { RemainingSeconds = 0f },
            new HealthBarFeedback(),
            new Suffocation { ElapsedSeconds = 0f },
            new Damaging(),
            new PlayerControlled());
    }

    private static readonly QueryDescription RespawnQuery =
        new QueryDescription().WithAll<PhysicsBody, PlayerControlled, Health, Oxygen, HitFlash, HealthBarFeedback, Suffocation>();

    public static void Respawn(World world, Vector2 positionMeters)
    {
        world.Query(in RespawnQuery, (ref PhysicsBody physicsBody, ref Health health, ref Oxygen oxygen, ref HitFlash hitFlash, ref HealthBarFeedback healthBarFeedback, ref Suffocation suffocation) =>
        {
            var bodyId = physicsBody.BodyId;
            B2Api.b2Body_SetTransform(bodyId, positionMeters, b2Rot.FromAngle(0f));
            B2Api.b2Body_SetLinearVelocity(bodyId, Vector2.Zero);
            B2Api.b2Body_SetAngularVelocity(bodyId, 0f);
            health.Current = health.Max;
            oxygen.Current = oxygen.Max;
            hitFlash.RemainingSeconds = 0f;
            healthBarFeedback = new HealthBarFeedback();
            suffocation.ElapsedSeconds = 0f;
        });
    }
}
