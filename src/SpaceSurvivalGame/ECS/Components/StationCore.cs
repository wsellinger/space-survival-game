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
/// than drifting off or spinning up over time. Each impulse also kicks off a staggered gas-puff
/// sequence (StationCoreSystem.SpawnSequencedDriftPuff) — the linear puff (slot 0) and the two
/// angular corner puffs (slots 1/2) each START DriftPuffs.StaggerSeconds apart (PuffNextIndex/
/// PuffNextStartSeconds), and once started each slot keeps re-emitting on its own for
/// DriftPuffs.DurationSeconds (Puff0ElapsedSeconds/Puff1ElapsedSeconds/Puff2ElapsedSeconds, -1 =
/// not yet started/already finished) so each individual puff still looks like a proper little
/// burst rather than a single-frame pop.
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
    public Vector2 PuffPushDirection; // direction of the most recent drift impulse's linear component (Vector2.Zero = none) — angles puff slot 0
    public float PuffAngularSign; // sign of the most recent drift impulse's angular component (0 = none) — angles puff slots 1/2
    public int PuffNextIndex; // 0..3 — which puff slot starts next; 3 = all three have already started
    public float PuffNextStartSeconds; // counts down to starting PuffNextIndex; re-rolled from DriftPuffs.StaggerSeconds each time
    public float Puff0ElapsedSeconds; // -1 = puff slot 0 (linear) not yet started/already finished; 0+ while it's actively re-emitting, up to DriftPuffs.DurationSeconds
    public float Puff1ElapsedSeconds; // same, for puff slot 1 (corner A, angular)
    public float Puff2ElapsedSeconds; // same, for puff slot 2 (corner B, angular)
}
