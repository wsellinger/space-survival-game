using System;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using NVector2 = System.Numerics.Vector2;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Puffs a couple of small particle jets (see ParticleEffects.SpawnRotationJetPuff) from the
/// ship's hull whenever it's actively turning this frame (Box2D angular velocity nonzero, set
/// by ShipInputSystem's TurnTowards earlier the same frame) — a visual RCS-thruster cue, not an
/// actual force. A diagonal pair fires to sell the couple: since the world uses a Y-down screen
/// convention, positive angular velocity sweeps the nose clockwise on screen, which fires the
/// nose-left and tail-right jets; negative (counterclockwise) fires nose-right and tail-left.
/// Hull mount points/exhaust directions mirror EngineJetRenderer's own geometry (nose-to-wing-
/// corner edges for the nose jets, straight out from the wing corners themselves for the tail
/// jets) so they sit consistently with the strafe jets already mounted there.
/// </summary>
public static class RotationJetSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<PhysicsBody, Transform, PlayerControlled>();

    private const float AngularVelocityDeadzone = 0.01f;

    public static void Run(World world, Texture2D particleTexture, EngineConfig config, int shipSpriteSizePixels,
        Microsoft.Xna.Framework.Color jetColor, Random random)
    {
        var halfSize = shipSpriteSizePixels / 2f;
        var noseLocal = new NVector2(halfSize - 1f, 0f);
        var rightCornerLocal = new NVector2(-halfSize, halfSize - 1f);
        var leftCornerLocal = new NVector2(-halfSize, -halfSize);

        var rightEdge = noseLocal - rightCornerLocal;
        var noseRightLocal = rightCornerLocal + rightEdge * config.RotationJets.NoseMountFraction;
        var rightExhaustLocal = NVector2.Normalize(new NVector2(rightEdge.X, -rightEdge.Y));

        var leftEdge = noseLocal - leftCornerLocal;
        var noseLeftLocal = leftCornerLocal + leftEdge * config.RotationJets.NoseMountFraction;
        var leftExhaustLocal = NVector2.Normalize(new NVector2(leftEdge.X, -leftEdge.Y));

        var tailLeftExhaustLocal = NVector2.Normalize(leftCornerLocal);
        var tailRightExhaustLocal = NVector2.Normalize(rightCornerLocal);

        world.Query(in Query, (ref PhysicsBody physicsBody, ref Transform transform) =>
        {
            var angularVelocity = B2Api.b2Body_GetAngularVelocity(physicsBody.BodyId);
            if (MathF.Abs(angularVelocity) < AngularVelocityDeadzone) return; // not actively turning this frame

            var positionMeters = transform.PositionMeters;
            var forward = new NVector2(MathF.Cos(transform.RotationRadians), MathF.Sin(transform.RotationRadians));
            var right = new NVector2(-forward.Y, forward.X);

            NVector2 ToWorldPosition(NVector2 local) => positionMeters + PhysicsWorld.PixelsToMeters(forward * local.X + right * local.Y);
            NVector2 ToWorldDirection(NVector2 local) => forward * local.X + right * local.Y;

            if (angularVelocity > 0f) // clockwise: nose-left + tail-right
            {
                ParticleEffects.SpawnRotationJetPuff(world, particleTexture, ToWorldPosition(noseLeftLocal), ToWorldDirection(leftExhaustLocal),
                    random, config.RotationJets, jetColor);
                ParticleEffects.SpawnRotationJetPuff(world, particleTexture, ToWorldPosition(rightCornerLocal), ToWorldDirection(tailRightExhaustLocal),
                    random, config.RotationJets, jetColor);
            }
            else // counterclockwise: nose-right + tail-left
            {
                ParticleEffects.SpawnRotationJetPuff(world, particleTexture, ToWorldPosition(noseRightLocal), ToWorldDirection(rightExhaustLocal),
                    random, config.RotationJets, jetColor);
                ParticleEffects.SpawnRotationJetPuff(world, particleTexture, ToWorldPosition(leftCornerLocal), ToWorldDirection(tailLeftExhaustLocal),
                    random, config.RotationJets, jetColor);
            }
        });
    }
}
