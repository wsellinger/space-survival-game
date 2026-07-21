using System;
using System.Numerics;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame;

/// <summary>
/// A single physics-driven ship, wired directly to Box2D. This is a placeholder —
/// once Arch ECS is set up, this becomes a Transform/Velocity/Sprite entity with
/// systems doing what HandleInput/Draw do here.
/// </summary>
public sealed class Ship : IDisposable
{
    private readonly float _thrustAcceleration; // meters/sec^2 while thrusting; Force = mass * this
    private readonly float _maxSpeedMetersPerSecond;
    private readonly float _turnSpeedRadiansPerSecond; // how fast the sprite turns to face travel direction
    private readonly int _spriteSize; // no camera yet, so a smaller ship stands in for "zooming out"

    private readonly b2BodyId _bodyId;
    private readonly Texture2D _texture;

    public Ship(PhysicsWorld physicsWorld, GraphicsDevice graphicsDevice, Vector2 startPositionMeters, ShipConfig config)
    {
        _thrustAcceleration = config.ThrustAcceleration;
        _maxSpeedMetersPerSecond = config.MaxSpeedMetersPerSecond;
        _turnSpeedRadiansPerSecond = config.TurnSpeedRadiansPerSecond;
        _spriteSize = config.SpriteSize;

        var bodyDef = B2Api.b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = startPositionMeters;
        bodyDef.linearDamping = 0f;  // no drag: momentum carries the ship, like real space
        bodyDef.angularDamping = 0f;
        _bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

        var shapeDef = B2Api.b2DefaultShapeDef();
        shapeDef.density = 1f;
        var halfExtent = PhysicsWorld.PixelsToMeters(_spriteSize / 2f);
        var box = B2Api.b2MakeBox(halfExtent, halfExtent);
        B2Api.b2CreatePolygonShape(_bodyId, in shapeDef, in box);

        _texture = ProceduralTextures.CreateRightFacingTriangle(graphicsDevice, _spriteSize, Microsoft.Xna.Framework.Color.White);
    }

    public Vector2 PositionMeters => B2Api.b2Body_GetPosition(_bodyId);

    public float AngleRadians => B2Api.b2Body_GetRotation(_bodyId).GetAngle();

    public void HandleInput(KeyboardState keyboard, Vector2 mousePositionPixels, float deltaSeconds)
    {
        // Movement (WASD, absolute screen directions) and facing (mouse) are
        // independent, twin-stick-style: you can thrust one way while facing another.
        var direction = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) direction += new Vector2(0, -1);
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) direction += new Vector2(0, 1);
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) direction += new Vector2(-1, 0);
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) direction += new Vector2(1, 0);

        if (direction != Vector2.Zero)
        {
            direction = Vector2.Normalize(direction);
            var mass = B2Api.b2Body_GetMass(_bodyId);
            B2Api.b2Body_ApplyForceToCenter(_bodyId, direction * (mass * _thrustAcceleration), wake: true);
        }

        var positionPixels = PhysicsWorld.MetersToPixels(PositionMeters);
        var toMouse = mousePositionPixels - positionPixels;
        if (toMouse.LengthSquared() > 0.01f)
        {
            var targetAngle = MathF.Atan2(toMouse.Y, toMouse.X);
            TurnTowards(targetAngle, deltaSeconds);
        }
        else
        {
            B2Api.b2Body_SetAngularVelocity(_bodyId, 0f);
        }
    }

    private void TurnTowards(float targetAngle, float deltaSeconds)
    {
        var delta = WrapAngle(targetAngle - AngleRadians);
        var maxStep = _turnSpeedRadiansPerSecond * deltaSeconds;

        if (MathF.Abs(delta) <= maxStep)
        {
            // Close enough to reach this frame — snap exactly so we don't hunt around the target.
            B2Api.b2Body_SetAngularVelocity(_bodyId, 0f);
            B2Api.b2Body_SetTransform(_bodyId, PositionMeters, b2Rot.FromAngle(targetAngle));
        }
        else
        {
            B2Api.b2Body_SetAngularVelocity(_bodyId, MathF.Sign(delta) * _turnSpeedRadiansPerSecond);
        }
    }

    private static float WrapAngle(float angle)
    {
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        return angle;
    }

    /// <summary>
    /// Call once per frame after the physics step. No drag means nothing else
    /// slows the ship down, so we hard-cap speed here instead.
    /// </summary>
    public void ClampSpeed()
    {
        var velocity = B2Api.b2Body_GetLinearVelocity(_bodyId);
        var speed = velocity.Length();
        if (speed > _maxSpeedMetersPerSecond)
        {
            B2Api.b2Body_SetLinearVelocity(_bodyId, velocity * (_maxSpeedMetersPerSecond / speed));
        }
    }

    public void Respawn(Vector2 positionMeters)
    {
        B2Api.b2Body_SetTransform(_bodyId, positionMeters, b2Rot.FromAngle(0f));
        B2Api.b2Body_SetLinearVelocity(_bodyId, Vector2.Zero);
        B2Api.b2Body_SetAngularVelocity(_bodyId, 0f);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var positionPixels = PhysicsWorld.MetersToPixels(PositionMeters).ToXna();
        var origin = new Microsoft.Xna.Framework.Vector2(_spriteSize / 2f, _spriteSize / 2f);

        spriteBatch.Draw(
            _texture,
            positionPixels,
            sourceRectangle: null,
            color: Microsoft.Xna.Framework.Color.White,
            rotation: AngleRadians,
            origin: origin,
            scale: 1f,
            effects: SpriteEffects.None,
            layerDepth: 0f);
    }

    public void Dispose() => _texture.Dispose();
}
