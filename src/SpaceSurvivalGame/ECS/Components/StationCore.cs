using System.Numerics;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// A station core — spawns already riding at the ship's exact center (Attached = true; its own
/// Transform is copied from the ship's every frame by StationCoreSystem) until the player's
/// Iron.Current first reaches StationCoreConfig.IronAmountRequired, at which point it detaches
/// (Attached flips false, that amount is spent). Once detached it eases from
/// FlightStartPositionMeters (wherever it was at that instant) to TargetPositionMeters — the
/// on-screen point farthest from any asteroid within StationCoreConfig.MaxSearchRangeMeters,
/// computed once and never re-evaluated — over FlightDurationSeconds, then stops there for good,
/// becoming an independent, stationary object. No physics/collision yet.
/// </summary>
public struct StationCore
{
    public bool Attached;
    public Vector2 TargetPositionMeters;
    public Vector2 FlightStartPositionMeters;
    public float FlightElapsedSeconds;
    public float FlightDurationSeconds; // <= 0 once arrived (or if it detached already at its target) — a done flag, not just a duration
}
