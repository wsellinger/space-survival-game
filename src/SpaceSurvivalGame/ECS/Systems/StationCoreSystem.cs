using System;
using System.Collections.Generic;
using System.Numerics;
using Arch.Core;
using Box2dNet.Interop;
using Microsoft.Xna.Framework.Graphics;
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
/// Flight.MaxSearchRangeMeters of the detach position) farthest from any asteroid's edge, a
/// one-time search never re-evaluated even if asteroids later drift closer. From then on this
/// system eases the core toward that target over a fixed duration (distance /
/// Flight.SpeedMetersPerSecond, shaped by Flight.EaseInExponent/EaseOutExponent for a slow
/// start/finish), stopping exactly on arrival and becoming an independent, stationary object — at
/// which point it also gains a real static Box2D body (see CreatePhysicsBody) so it solidly
/// blocks the ship/asteroids/pickups instead of just sitting there visually; it does deal
/// collision damage to the ship on contact, scaled down by Physics.CollisionDamageMultiplier
/// compared to an ordinary asteroid hit (see CollisionDamageSystem). A one-time shockwave fires
/// once flight progress first reaches Shockwave.TriggerProgress — not necessarily full arrival, so it can go off
/// slightly before the core actually stops: an outward impulse on every nearby PhysicsBody (see
/// ApplyShockwave, which also grants the ship a brief Invulnerability window as a failsafe) and a
/// starting timer for the matching expanding-ring visual (see StationCoreShockwaveRenderSystem).
/// Once landed it also starts drifting very gently in place (see ApplyDriftImpulse) and runs a
/// continuous "anti-gravity" push on any nearby asteroid/pickup every frame (see
/// ApplyAntiGravityField), keeping the area right around it naturally clear.
/// </summary>
public static class StationCoreSystem
{
    private static readonly QueryDescription ShipQuery = new QueryDescription().WithAll<Transform, Iron, PlayerControlled>();
    private static readonly QueryDescription CoreQuery = new QueryDescription().WithAll<Transform, StationCore>();
    private static readonly QueryDescription AsteroidQuery = new QueryDescription().WithAll<Transform, Asteroid>();
    private static readonly QueryDescription PhysicsBodyQuery = new QueryDescription().WithAll<PhysicsBody, Transform>();

    public static void Run(World world, Camera camera, PhysicsWorld physicsWorld, StationCoreConfig config, float deltaSeconds, Random random,
        Texture2D driftPuffTexture, Microsoft.Xna.Framework.Color driftPuffColor)
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

        world.Query(in CoreQuery, (Entity coreEntity, ref Transform coreTransform, ref StationCore core) =>
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
                    core.FlightDurationSeconds = flightDistance / MathF.Max(0.0001f, config.Flight.SpeedMetersPerSecond);

                    // No flight needed (it already detached right at its own target) — the usual
                    // per-frame trigger check below never runs for this core, so fire the
                    // shockwave and the arrival physics body here instead of losing them entirely.
                    if (core.FlightDurationSeconds <= 0f)
                    {
                        core.ShockwaveElapsedSeconds = 0f;
                        ApplyShockwave(world, coreTransform.PositionMeters, config, shipEntity, coreEntity);
                        CreatePhysicsBody(world, physicsWorld, coreEntity, ref core, coreTransform.PositionMeters, config, random);
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
                var easedProgress = EaseInOut(progress, config.Flight.EaseInExponent, config.Flight.EaseOutExponent);
                coreTransform.PositionMeters = Vector2.Lerp(core.FlightStartPositionMeters, core.TargetPositionMeters, easedProgress);

                // Fires once, as soon as progress first crosses ShockwaveTriggerProgress — not
                // necessarily full arrival (progress >= 1) — from wherever the core currently is,
                // guarded by ShockwaveElapsedSeconds still being -1 so it can't refire next frame.
                if (core.ShockwaveElapsedSeconds < 0f && progress >= config.Shockwave.TriggerProgress)
                {
                    core.ShockwaveElapsedSeconds = 0f;
                    ApplyShockwave(world, coreTransform.PositionMeters, config, shipEntity, coreEntity);
                }

                if (progress >= 1f)
                {
                    core.FlightDurationSeconds = 0f; // marks arrival so this branch is skipped from here on
                    CreatePhysicsBody(world, physicsWorld, coreEntity, ref core, coreTransform.PositionMeters, config, random);
                }
            }

            if (core.ShockwaveElapsedSeconds >= 0f && core.ShockwaveElapsedSeconds < config.Shockwave.DurationSeconds)
                core.ShockwaveElapsedSeconds += deltaSeconds; // stops advancing once past the duration — GetFlightProgress-style done flag, not an unbounded timer

            // Landed (Attached is already false by this point, or this frame's earlier branch
            // would have returned) — gently drift in place forever after.
            if (core.FlightDurationSeconds <= 0f)
            {
                ApplyAntiGravityField(world, coreTransform.PositionMeters, config, coreEntity);

                core.DriftTimerSeconds -= deltaSeconds;
                if (core.DriftTimerSeconds <= 0f)
                {
                    ApplyDriftImpulse(world, coreEntity, ref core, config, random);
                    core.DriftTimerSeconds = NextInRange(random, config.Drift.ImpulseIntervalSecondsRange);
                }

                // Starts the next puff slot in the staggered sequence kicked off by
                // ApplyDriftImpulse above (see SpawnSequencedDriftPuff) — one starts every
                // Drift.Puffs.StaggerSeconds instead of all three at once.
                if (core.PuffNextIndex < PuffSequenceCount)
                {
                    core.PuffNextStartSeconds -= deltaSeconds;
                    if (core.PuffNextStartSeconds <= 0f)
                    {
                        SetPuffSlotElapsed(ref core, core.PuffNextIndex, 0f);
                        core.PuffNextIndex++;
                        core.PuffNextStartSeconds = config.Drift.Puffs.StaggerSeconds;
                    }
                }

                // Sustains each already-started puff slot's own emission for
                // Drift.Puffs.DurationSeconds, independent of the other slots' own timers — so an
                // individual puff still looks like a proper little burst rather than a
                // single-frame pop, regardless of how far apart the three slots started.
                if (GetPuffSlotElapsed(in core, 0) >= 0f || GetPuffSlotElapsed(in core, 1) >= 0f || GetPuffSlotElapsed(in core, 2) >= 0f)
                {
                    var bodyId = world.Get<PhysicsBody>(coreEntity).BodyId;
                    var positionMeters = B2Api.b2Body_GetPosition(bodyId);
                    var rotationRadians = B2Api.b2Body_GetRotation(bodyId).GetAngle();

                    for (var puffIndex = 0; puffIndex < PuffSequenceCount; puffIndex++)
                    {
                        var elapsedSeconds = GetPuffSlotElapsed(in core, puffIndex);
                        if (elapsedSeconds < 0f || elapsedSeconds >= config.Drift.Puffs.DurationSeconds) continue;

                        SpawnSequencedDriftPuff(world, driftPuffTexture, driftPuffColor, config, random,
                            positionMeters, rotationRadians, core.PuffPushDirection, core.PuffAngularSign, puffIndex);
                        SetPuffSlotElapsed(ref core, puffIndex, elapsedSeconds + deltaSeconds);
                    }
                }
            }
        });
    }

    /// <summary>
    /// A one-time outward push on every nearby PhysicsBody, scaling from Shockwave.ImpulseStrength
    /// at zero distance down to 0 at Shockwave.RadiusMeters. Applying the same base impulse
    /// regardless of an entity's own mass is deliberate — Box2D's own impulse/mass=deltaV means
    /// heavier bodies (a big asteroid) still end up pushed less far than light ones for the same
    /// blast, matching what a real shockwave would do — but O2/iron pickups are light enough that
    /// this alone still sends them flying far more violently than asteroids do, so
    /// Shockwave.PickupImpulseMultiplier tunes them down separately, same idea as the ship's own
    /// Shockwave.ShipImpulseMultiplier. Also grants the ship a failsafe window of Invulnerability
    /// lasting Shockwave.DurationSeconds, since the shockwave can fling an asteroid straight into it
    /// right as it's still settling. coreEntity itself is always excluded — currently it never has
    /// a PhysicsBody yet at the moment its own shockwave fires anyway (CreatePhysicsBody runs
    /// after), but skipping it explicitly means it stays immune to its own blast even if a future
    /// change (e.g. multiple station cores) ends up calling this after the body already exists.
    /// </summary>
    private static void ApplyShockwave(World world, Vector2 originMeters, StationCoreConfig config, Entity shipEntity, Entity coreEntity)
    {
        world.Query(in PhysicsBodyQuery, (Entity entity, ref PhysicsBody physicsBody, ref Transform transform) =>
        {
            if (entity == coreEntity) return;

            var offset = transform.PositionMeters - originMeters;
            var distance = offset.Length();
            if (distance < 0.0001f || distance > config.Shockwave.RadiusMeters) return;

            var falloff = 1f - distance / config.Shockwave.RadiusMeters;
            var strength = config.Shockwave.ImpulseStrength * falloff;
            if (entity == shipEntity) strength *= config.Shockwave.ShipImpulseMultiplier;
            else if (world.Has<OxygenPickup>(entity) || world.Has<IronPickup>(entity)) strength *= config.Shockwave.PickupImpulseMultiplier;

            var impulse = offset / distance * strength;
            B2Api.b2Body_ApplyLinearImpulseToCenter(physicsBody.BodyId, impulse, wake: true);
        });

        world.Get<Invulnerability>(shipEntity).RemainingSeconds = config.Shockwave.DurationSeconds;
    }

    /// <summary>
    /// A continuous, always-on outward push while the core is landed — every frame, every nearby
    /// asteroid/O2-pickup/iron-pickup gets pushed directly away from the core's own center via a
    /// real force (not a one-time impulse, so it keeps acting for as long as something lingers in
    /// range), scaling from AntiGravityField.ForceStrength at zero distance down to 0 at
    /// AntiGravityField.RadiusMeters — same linear falloff shape as ApplyShockwave, just
    /// continuous instead of a single kick. Deliberately whitelist-only (Asteroid/OxygenPickup/
    /// IronPickup) rather than "everyone except the ship" — the field is meant to keep debris off
    /// the built station, not to shove the ship itself around every time it flies nearby.
    /// </summary>
    private static void ApplyAntiGravityField(World world, Vector2 originMeters, StationCoreConfig config, Entity coreEntity)
    {
        world.Query(in PhysicsBodyQuery, (Entity entity, ref PhysicsBody physicsBody, ref Transform transform) =>
        {
            if (entity == coreEntity) return;
            if (!world.Has<Asteroid>(entity) && !world.Has<OxygenPickup>(entity) && !world.Has<IronPickup>(entity)) return;

            var offset = transform.PositionMeters - originMeters;
            var distance = offset.Length();
            if (distance < 0.0001f || distance > config.AntiGravityField.RadiusMeters) return;

            var falloff = 1f - distance / config.AntiGravityField.RadiusMeters;
            var force = offset / distance * (config.AntiGravityField.ForceStrength * falloff);
            B2Api.b2Body_ApplyForceToCenter(physicsBody.BodyId, force, wake: true);
        });
    }

    /// <summary>
    /// Gives the now-arrived core a real dynamic Box2D body — a square matching the fully-grown
    /// circuit-board build effect's own footprint (Build.MaxSizePixels; see
    /// StationCoreBuildEffectRenderSystem), not just the small core dot on top of it, since that
    /// square is what's actually left permanently visible on the ground once construction
    /// finishes — so it solidly blocks the ship/asteroids/pickups from here on instead of just
    /// being a visual. Dynamic (not static/kinematic) so later collisions (and its own drift
    /// impulses — see ApplyDriftImpulse) can actually move it like any other body, mass driven by
    /// Physics.MaterialDensity; also adds a Velocity component so PhysicsSyncSystem picks it up and
    /// keeps its Transform following the body from here on, the same way every other
    /// physics-driven entity works. Tagged Damaging with a DamageMultiplier of
    /// Physics.CollisionDamageMultiplier so bumping it does hurt the ship, just less than an
    /// ordinary asteroid hit at the same speed (see CollisionDamageSystem).
    ///
    /// Also records HomePositionMeters/HomeRotationRadians (where it landed — the body always
    /// starts at rotation 0, since bodyDef never sets one) and rolls the first DriftTimerSeconds,
    /// so drifting can start from here. Starts with a small random linear + angular velocity
    /// (InitialSpeed.LinearMetersPerSecondRange/AngularRadiansPerSecondRange) baked straight into
    /// bodyDef, so it's already gently moving the instant it lands rather than sitting frozen
    /// until the first drift impulse fires.
    /// </summary>
    private static void CreatePhysicsBody(World world, PhysicsWorld physicsWorld, Entity coreEntity, ref StationCore core, Vector2 positionMeters, StationCoreConfig config, Random random)
    {
        var initialSpeedAngle = (float)(random.NextDouble() * Math.PI * 2);
        var initialSpeed = NextInRange(random, config.InitialSpeed.LinearMetersPerSecondRange);

        var bodyDef = B2Api.b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = positionMeters;
        bodyDef.linearDamping = config.Physics.LinearDamping;
        bodyDef.angularDamping = config.Physics.AngularDamping;
        bodyDef.linearVelocity = new Vector2(MathF.Cos(initialSpeedAngle), MathF.Sin(initialSpeedAngle)) * initialSpeed;
        bodyDef.angularVelocity = NextInRange(random, config.InitialSpeed.AngularRadiansPerSecondRange) * (random.Next(2) == 0 ? -1f : 1f);
        var bodyId = B2Api.b2CreateBody(physicsWorld.WorldId, bodyDef);

        var shapeDef = B2Api.b2DefaultShapeDef();
        shapeDef.density = config.Physics.MaterialDensity;
        shapeDef.material.restitution = config.Physics.Restitution;
        var halfWidthMeters = PhysicsWorld.PixelsToMeters(config.Build.MaxSizePixels) / 2f;
        var square = B2Api.b2MakeSquare(halfWidthMeters);
        B2Api.b2CreatePolygonShape(bodyId, in shapeDef, in square);

        // Every StationCore field must be written BEFORE world.Add below — adding components
        // moves the entity to a new archetype, which invalidates this `ref core` (a reference into
        // the query's current archetype storage). Writes made through it after the move are
        // silently lost; the entity's real data keeps its previous (default/zeroed) values. This
        // bit us directly: HomePositionMeters/DriftTimerSeconds used to be set after Add and never
        // actually stuck, so the very next frame's drift check read a zeroed timer and a (0,0)
        // home, firing almost instantly with a "return to the world origin" impulse big enough to
        // look exactly like getting hit by a shockwave.
        core.HomePositionMeters = positionMeters;
        core.HomeRotationRadians = 0f;
        core.DriftTimerSeconds = NextInRange(random, config.Drift.ImpulseIntervalSecondsRange);
        core.PuffNextIndex = PuffSequenceCount; // nothing pending
        SetPuffSlotElapsed(ref core, 0, -1f);
        SetPuffSlotElapsed(ref core, 1, -1f);
        SetPuffSlotElapsed(ref core, 2, -1f);

        world.Add(coreEntity, new PhysicsBody { BodyId = bodyId }, new Velocity(),
            new Damaging(), new DamageMultiplier { Value = config.Physics.CollisionDamageMultiplier });
    }

    /// <summary>
    /// Fires once every DriftTimerSeconds for a landed core: a small random linear impulse (random
    /// direction, magnitude from Drift.LinearImpulseStrengthRange) plus a correction proportional
    /// to how far the core has drifted from homePositionMeters, and separately a small random
    /// angular impulse (random sign, magnitude from Drift.AngularImpulseStrengthRange) plus a
    /// correction proportional to how far its rotation has drifted from homeRotationRadians. The
    /// correction terms are what keep it wandering in place rather than walking away or spinning
    /// up over many impulses — each one is nudged back toward home, not just random. Also kicks
    /// off a staggered gas-puff sequence (PuffNextIndex/PuffNextStartSeconds/PuffPushDirection/
    /// PuffAngularSign; see SpawnSequencedDriftPuff, started/sustained each frame in Run) angled to
    /// match this impulse, same idea as RotationJetSystem's ship-turning puffs.
    /// </summary>
    private static void ApplyDriftImpulse(World world, Entity coreEntity, ref StationCore core, StationCoreConfig config, Random random)
    {
        var bodyId = world.Get<PhysicsBody>(coreEntity).BodyId;
        var positionMeters = B2Api.b2Body_GetPosition(bodyId);
        var rotationRadians = B2Api.b2Body_GetRotation(bodyId).GetAngle();

        var randomAngle = (float)(random.NextDouble() * Math.PI * 2);
        var randomImpulse = new Vector2(MathF.Cos(randomAngle), MathF.Sin(randomAngle)) * NextInRange(random, config.Drift.LinearImpulseStrengthRange);
        var displacementMeters = core.HomePositionMeters - positionMeters;
        var returnImpulse = displacementMeters * config.Drift.ReturnStrength;
        var totalLinearImpulse = randomImpulse + returnImpulse;
        B2Api.b2Body_ApplyLinearImpulseToCenter(bodyId, totalLinearImpulse, wake: true);

        var randomAngularImpulse = NextInRange(random, config.Drift.AngularImpulseStrengthRange) * (random.Next(2) == 0 ? -1f : 1f);
        var angleErrorRadians = WrapAngle(core.HomeRotationRadians - rotationRadians);
        var returnAngularImpulse = angleErrorRadians * config.Drift.AngularReturnStrength;
        var totalAngularImpulse = randomAngularImpulse + returnAngularImpulse;
        B2Api.b2Body_ApplyAngularImpulse(bodyId, totalAngularImpulse, wake: true);

        core.PuffPushDirection = totalLinearImpulse.LengthSquared() > 0.0000001f ? Vector2.Normalize(totalLinearImpulse) : Vector2.Zero;
        core.PuffAngularSign = MathF.Sign(totalAngularImpulse);

        // Restarts the staggered sequence from slot 0, abandoning whatever the previous impulse's
        // sequence was still doing — DriftImpulseIntervalSecondsRange can roll shorter than a full
        // sequence takes to play out (StaggerSeconds * 3, plus each slot's own DurationSeconds), so
        // a new impulse can easily land before the last one's puffs are all done.
        core.PuffNextIndex = 0;
        core.PuffNextStartSeconds = 0f; // start slot 0 immediately on the next tick
        SetPuffSlotElapsed(ref core, 0, -1f);
        SetPuffSlotElapsed(ref core, 1, -1f);
        SetPuffSlotElapsed(ref core, 2, -1f);
    }

    // Total puffs in one drift-impulse sequence: the linear one, then the two angular corner ones.
    private const int PuffSequenceCount = 3;

    private static float GetPuffSlotElapsed(in StationCore core, int puffIndex) => puffIndex switch
    {
        0 => core.Puff0ElapsedSeconds,
        1 => core.Puff1ElapsedSeconds,
        _ => core.Puff2ElapsedSeconds
    };

    private static void SetPuffSlotElapsed(ref StationCore core, int puffIndex, float value)
    {
        switch (puffIndex)
        {
            case 0: core.Puff0ElapsedSeconds = value; break;
            case 1: core.Puff1ElapsedSeconds = value; break;
            default: core.Puff2ElapsedSeconds = value; break;
        }
    }

    /// <summary>
    /// Emits one frame's worth of particles for puff slot puffIndex — 0 is the linear one (mounted
    /// on the square's edge opposite pushDirection, exhaust firing further that same opposite way
    /// — a real thruster mounted there would fire backward to push the core the way the impulse
    /// did), 1/2 are the diagonal corner pair firing tangentially for angularSign, same "exhaust
    /// opposes the push it causes" convention RotationJetSystem already uses for the ship's own
    /// turning jets. Mount points sit at the fully-grown build-effect square's own half-width
    /// (Build.MaxSizePixels), matching the physics body's actual footprint. Called every frame a
    /// slot is active in Run — slots START Drift.Puffs.StaggerSeconds apart (that's what reads as
    /// three quick puffs one after another instead of one simultaneous cluster) and each keeps
    /// being called like this for its own Drift.Puffs.DurationSeconds once started.
    /// </summary>
    private static void SpawnSequencedDriftPuff(World world, Texture2D driftPuffTexture, Microsoft.Xna.Framework.Color driftPuffColor, StationCoreConfig config,
        Random random, Vector2 positionMeters, float rotationRadians, Vector2 pushDirection, float angularSign, int puffIndex)
    {
        var halfWidthMeters = PhysicsWorld.PixelsToMeters(config.Build.MaxSizePixels) / 2f;

        if (puffIndex == 0)
        {
            if (pushDirection == Vector2.Zero) return;
            var mountWorld = positionMeters - pushDirection * halfWidthMeters;
            SpawnDriftPuff(world, driftPuffTexture, mountWorld, -pushDirection, random, config.Drift.Puffs, driftPuffColor);
            return;
        }

        if (angularSign == 0f) return;

        var forward = new Vector2(MathF.Cos(rotationRadians), MathF.Sin(rotationRadians));
        var right = new Vector2(-forward.Y, forward.X);
        Vector2 ToWorldPosition(Vector2 local) => positionMeters + forward * local.X + right * local.Y;
        Vector2 ToWorldDirection(Vector2 local) => forward * local.X + right * local.Y;

        var corner = puffIndex == 1 ? new Vector2(halfWidthMeters, halfWidthMeters) : new Vector2(-halfWidthMeters, -halfWidthMeters);
        SpawnDriftPuff(world, driftPuffTexture, ToWorldPosition(corner), ToWorldDirection(AngularExhaustLocal(corner, angularSign)), random, config.Drift.Puffs, driftPuffColor);
    }

    /// <summary>Opposite of the tangential direction a point at cornerLocal would move due to an angular velocity of the given sign — see SpawnDriftPuffs.</summary>
    private static Vector2 AngularExhaustLocal(Vector2 cornerLocal, float angularSign) =>
        Vector2.Normalize(new Vector2(cornerLocal.Y, -cornerLocal.X) * angularSign);

    private static void SpawnDriftPuff(World world, Texture2D texture, Vector2 positionMeters, Vector2 outwardDirection, Random random,
        StationCoreDriftPuffConfig config, Microsoft.Xna.Framework.Color color) =>
        ParticleEffects.SpawnRotationJetPuff(world, texture, positionMeters, outwardDirection, random,
            config.ParticleCountPerFrame, config.ParticleSpeedMetersPerSecondRange, config.ParticleLifetimeSecondsRange,
            config.ParticleSizePixels, config.SpreadAngleDegrees, color);

    private static float NextInRange(Random random, FloatRange range) =>
        range.Min + (float)random.NextDouble() * (range.Max - range.Min);

    private static float WrapAngle(float angle)
    {
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        return angle;
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
    /// on the camera) and returns whichever candidate — within Flight.MaxSearchRangeMeters of
    /// originMeters (the ship's own position at the moment of detaching) but no closer than
    /// Flight.MinDistanceFromShipMeters to it — maximizes its distance to the nearest asteroid's
    /// own edge (distance to center minus that asteroid's RadiusMeters): the biggest gap currently
    /// visible, not just the biggest gap between centers. Falls back to a point exactly
    /// Flight.MinDistanceFromShipMeters from originMeters (an arbitrary fixed direction) if no
    /// candidate qualifies at all, so the fallback itself never violates the minimum-distance
    /// failsafe.
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

        var resolution = Math.Max(1, config.Flight.OpenSpotSearchResolution);
        var bestPositionMeters = originMeters + new Vector2(config.Flight.MinDistanceFromShipMeters, 0f);
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

                var distanceFromShip = Vector2.Distance(candidateMeters, originMeters);
                if (distanceFromShip > config.Flight.MaxSearchRangeMeters || distanceFromShip < config.Flight.MinDistanceFromShipMeters) continue;

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
