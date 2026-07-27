using System.Numerics;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// A station core — spawns already riding at the ship's exact center (Attached = true; its own
/// Transform is copied from the ship's every frame by StationCoreSystem) until the player's
/// Iron.Current first reaches StationCoreConfig.IronAmountRequired, at which point it detaches
/// (Attached flips false, that amount is spent). Once detached it eases from
/// FlightStartPositionMeters (wherever it was at that instant) to TargetPositionMeters — the
/// on-screen point farthest from any asteroid within StationCoreConfig.MaxSearchRangeMeters,
/// computed once and never re-evaluated — over FlightDurationSeconds, then stops there and gains
/// a real dynamic Box2D body (StationCoreSystem.CreatePhysicsBody).
///
/// Arrival also fires a one-time shockwave (StationCoreSystem): an outward physics impulse on
/// every nearby PhysicsBody, plus a fading expanding-ring visual tracked by
/// ShockwaveElapsedSeconds, which starts at -1 (never triggered) and counts up from 0 once the
/// shockwave fires.
///
/// From then on it's left to gently drift: every DriftTimerSeconds (re-rolled from
/// DriftImpulseIntervalSecondsRange after each one fires) StationCoreSystem gives it a small
/// random linear + angular impulse nudged to pull it back toward HomePositionMeters/
/// HomeRotationRadians — the exact spot/orientation it landed at — so it wanders in place rather
/// than drifting off or spinning up over time.
/// </summary>
public struct StationCore
{
    public bool Attached;
    public Vector2 TargetPositionMeters;
    public Vector2 FlightStartPositionMeters;
    public float FlightElapsedSeconds;
    public float FlightDurationSeconds; // <= 0 once arrived (or if it detached already at its target) — a done flag, not just a duration
    public float ShockwaveElapsedSeconds; // -1 = not yet triggered
    public Vector2 HomePositionMeters; // where it landed — every drift impulse is nudged back toward this
    public float HomeRotationRadians; // the rotation it landed at — every drift angular impulse is nudged back toward this
    public float DriftTimerSeconds; // counts down to the next drift impulse; re-rolled from DriftImpulseIntervalSecondsRange after each one fires
}
