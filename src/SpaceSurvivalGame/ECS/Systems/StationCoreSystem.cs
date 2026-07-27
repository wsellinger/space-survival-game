using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// While the station core is still Attached, copies the ship's own position onto it every frame
/// so it visually rides along at the ship's center. Once the ship's Iron.Current first reaches
/// StationCoreConfig.IronAmountRequired, spends that amount, flips Attached false, and picks a
/// TargetPositionMeters — the point within the CURRENT on-screen view (and within
/// MaxSearchRangeMeters of the detach position) farthest from any asteroid's edge, a one-time
/// search never re-evaluated even if asteroids later drift closer. From then on this system
/// eases the core toward that target over a fixed duration (distance / FlightSpeedMetersPerSecond,
/// shaped by FlightEaseExponent for a slow start/finish), stopping exactly on arrival and
/// becoming an independent, stationary object. A one-time shockwave fires once flight progress
/// first reaches ShockwaveTriggerProgress — not necessarily full arrival, so it can go off
/// slightly before the core actually stops: an outward impulse on every nearby PhysicsBody (see
/// ApplyShockwave, which also grants the ship a brief Invulnerability window as a failsafe) and a
/// starting timer for the matching expanding-ring visual (see StationCoreShockwaveRenderSystem).
/// </summary>
public static class StationCoreSystem
{
    private static readonly QueryDescription ShipQuery = new QueryDescription().WithAll<Transform, Iron, PlayerControlled>();
    private static readonly QueryDescription CoreQuery = new QueryDescription().WithAll<Transform, StationCore>();
    private static readonly QueryDescription AsteroidQuery = new QueryDescription().WithAll<Transform, Asteroid>();
    private static readonly QueryDescription PhysicsBodyQuery = new QueryDescription().WithAll<PhysicsBody, Transform>();

    public static void Run(World world, Camera camera, StationCoreConfig config, float deltaSeconds)
    {
        var shipEntity = Entity.Null;
        var shipPositionMeters = Vector2.Zero;
        var ironCurrent = 0f;
        var foundShip = false;
        world.Query(in ShipQuery, (Entity entity, ref Transform transform, ref Iron iron) =>
        {
            shipEntity = entity;
            shipPositionMeters = transform.PositionMeters;
            ironCurrent = iron.Current;
            foundShip = true;
        });
        if (!foundShip) return;

        world.Query(in CoreQuery, (ref Transform coreTransform, ref StationCore core) =>
        {
            if (core.Attached)
            {
                if (ironCurrent >= config.IronAmountRequired)
                {
                    core.Attached = false;
                    world.Get<Iron>(shipEntity).Current -= config.IronAmountRequired;

                    core.FlightStartPositionMeters = coreTransform.PositionMeters;
                    core.TargetPositionMeters = FindOpenSpotOnScreen(world, camera, config, coreTransform.PositionMeters);
                    core.FlightElapsedSeconds = 0f;
                    var flightDistance = Vector2.Distance(core.FlightStartPositionMeters, core.TargetPositionMeters);
                    core.FlightDurationSeconds = flightDistance / MathF.Max(0.0001f, config.FlightSpeedMetersPerSecond);

                    // No flight needed (it already detached right at its own target) — the usual
                    // per-frame trigger check below never runs for this core, so fire it here
                    // instead of losing the shockwave entirely.
                    if (core.FlightDurationSeconds <= 0f)
                    {
                        core.ShockwaveElapsedSeconds = 0f;
                        ApplyShockwave(world, coreTransform.PositionMeters, config, shipEntity);
                    }
                }
                else
                {
                    coreTransform.PositionMeters = shipPositionMeters;
                }

                return;
            }

            if (core.FlightDurationSeconds > 0f)
            {
                core.FlightElapsedSeconds += deltaSeconds;
                var progress = MathF.Min(1f, core.FlightElapsedSeconds / core.FlightDurationSeconds);
                var easedProgress = EaseInOut(progress, config.FlightEaseInExponent, config.FlightEaseOutExponent);
                coreTransform.PositionMeters = Vector2.Lerp(core.FlightStartPositionMeters, core.TargetPositionMeters, easedProgress);

                // Fires once, as soon as progress first crosses ShockwaveTriggerProgress — not
                // necessarily full arrival (progress >= 1) — from wherever the core currently is,
                // guarded by ShockwaveElapsedSeconds still being -1 so it can't refire next frame.
                if (core.ShockwaveElapsedSeconds < 0f && progress >= config.ShockwaveTriggerProgress)
                {
                    core.ShockwaveElapsedSeconds = 0f;
                    ApplyShockwave(world, coreTransform.PositionMeters, config, shipEntity);
                }

                if (progress >= 1f) core.FlightDurationSeconds = 0f; // marks arrival so this branch is skipped from here on
            }

            if (core.ShockwaveElapsedSeconds >= 0f && core.ShockwaveElapsedSeconds < config.ShockwaveDurationSeconds)
                core.ShockwaveElapsedSeconds += deltaSeconds; // stops advancing once past the duration — GetFlightProgress-style done flag, not an unbounded timer
        });
    }

    /// <summary>
    /// A one-time outward push on every nearby PhysicsBody, scaling from ShockwaveImpulseStrength
    /// at zero distance down to 0 at ShockwaveRadiusMeters. Applying the same base impulse
    /// regardless of an entity's own mass is deliberate — Box2D's own impulse/mass=deltaV means
    /// heavier bodies (a big asteroid) still end up pushed less far than light ones (a pickup) for
    /// the same blast, matching what a real shockwave would do. The player's own ship is a
    /// deliberate exception on top of that: ShockwaveShipImpulseMultiplier scales its impulse down
    /// separately so the player barely feels it while everything else nearby gets the full push.
    /// Also grants the ship a failsafe window of Invulnerability lasting ShockwaveDurationSeconds,
    /// since the shockwave can fling an asteroid straight into it right as it's still settling.
    /// </summary>
    private static void ApplyShockwave(World world, Vector2 originMeters, StationCoreConfig config, Entity shipEntity)
    {
        world.Query(in PhysicsBodyQuery, (Entity entity, ref PhysicsBody physicsBody, ref Transform transform) =>
        {
            var offset = transform.PositionMeters - originMeters;
            var distance = offset.Length();
            if (distance < 0.0001f || distance > config.ShockwaveRadiusMeters) return;

            var falloff = 1f - distance / config.ShockwaveRadiusMeters;
            var strength = config.ShockwaveImpulseStrength * falloff;
            if (entity == shipEntity) strength *= config.ShockwaveShipImpulseMultiplier;

            var impulse = offset / distance * strength;
            B2Api.b2Body_ApplyLinearImpulseToCenter(physicsBody.BodyId, impulse, wake: true);
        });

        world.Get<Invulnerability>(shipEntity).RemainingSeconds = config.ShockwaveDurationSeconds;
    }

    /// <summary>
    /// Raw (un-eased) 0-1 flight progress for a detached core — 1 once FlightDurationSeconds has
    /// been zeroed out to mark arrival, since the original duration isn't kept around past that
    /// point. Shared with StationCoreBuildEffectRenderSystem so its grow/spin reveal tracks the
    /// exact same progress (and, via EaseInOut below, the exact same easing) as the core's own
    /// movement.
    /// </summary>
    public static float GetFlightProgress(in StationCore core) =>
        core.FlightDurationSeconds <= 0f ? 1f : MathF.Min(1f, core.FlightElapsedSeconds / core.FlightDurationSeconds);

    /// <summary>
    /// Ease-in-ease-out power curve with independent exponents either side of the midpoint —
    /// 1 = linear (no easing) for that half, higher = a more pronounced ease. Both halves meet
    /// at (0.5, 0.5) regardless of how different easeInExponent/easeOutExponent are, so there's
    /// no visible seam even with very different values.
    /// </summary>
    public static float EaseInOut(float t, float easeInExponent, float easeOutExponent)
    {
        return t < 0.5f
            ? 0.5f * MathF.Pow(2f * t, easeInExponent)
            : 1f - 0.5f * MathF.Pow(2f * (1f - t), easeOutExponent);
    }

    /// <summary>
    /// Samples a resolution x resolution grid across the current viewport (world-space, centered
    /// on the camera) and returns whichever candidate within MaxSearchRangeMeters of originMeters
    /// maximizes its distance to the nearest asteroid's own edge (distance to center minus that
    /// asteroid's RadiusMeters) — the biggest gap currently visible, not just the biggest gap
    /// between centers. Falls back to originMeters itself if no candidate qualifies within range.
    /// </summary>
    private static Vector2 FindOpenSpotOnScreen(World world, Camera camera, StationCoreConfig config, Vector2 originMeters)
    {
        var asteroidPositions = new List<Vector2>();
        var asteroidRadii = new List<float>();
        world.Query(in AsteroidQuery, (ref Transform transform, ref Asteroid asteroid) =>
        {
            asteroidPositions.Add(transform.PositionMeters);
            asteroidRadii.Add(asteroid.RadiusMeters);
        });

        var halfWidthMeters = PhysicsWorld.PixelsToMeters(camera.ViewportWidth / 2f);
        var halfHeightMeters = PhysicsWorld.PixelsToMeters(camera.ViewportHeight / 2f);

        var resolution = Math.Max(1, config.OpenSpotSearchResolution);
        var bestPositionMeters = originMeters;
        var bestClearanceMeters = float.NegativeInfinity;

        for (var gx = 0; gx < resolution; gx++)
        {
            var fractionX = resolution == 1 ? 0.5f : gx / (float)(resolution - 1);
            for (var gy = 0; gy < resolution; gy++)
            {
                var fractionY = resolution == 1 ? 0.5f : gy / (float)(resolution - 1);
                var candidateMeters = camera.PositionMeters + new Vector2(
                    (fractionX * 2f - 1f) * halfWidthMeters,
                    (fractionY * 2f - 1f) * halfHeightMeters);

                if (Vector2.Distance(candidateMeters, originMeters) > config.MaxSearchRangeMeters) continue;

                var clearanceMeters = float.PositiveInfinity;
                for (var i = 0; i < asteroidPositions.Count; i++)
                {
                    var candidateClearance = Vector2.Distance(candidateMeters, asteroidPositions[i]) - asteroidRadii[i];
                    if (candidateClearance < clearanceMeters) clearanceMeters = candidateClearance;
                }

                if (asteroidPositions.Count == 0) clearanceMeters = 0f; // no asteroids at all — anywhere in range works

                if (clearanceMeters > bestClearanceMeters)
                {
                    bestClearanceMeters = clearanceMeters;
                    bestPositionMeters = candidateMeters;
                }
            }
        }

        return bestPositionMeters;
    }
}
