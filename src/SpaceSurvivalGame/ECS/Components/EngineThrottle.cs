namespace SpaceSurvivalGame.ECS.Components;

/// <summary>
/// The ship's actual applied thrust, decomposed onto its own local axes for EngineJetRenderer.
/// Current (0-1) is the forward component — how much of the thrust points out of the nose,
/// driving the main tail jet. LeftStrafe/RightStrafe (each 0-1) drive the two strafe jets near
/// the nose: a purely sideways push lights up just the one on the opposite side (reaction
/// thrust), while a backward push — no dedicated rear jet exists — lights up both equally, so
/// reversing still reads as the engines firing. Both stay zero whenever thrust itself isn't
/// firing; see ShipInputSystem for how they're derived and eased.
/// </summary>
public struct EngineThrottle
{
    public float Current;
    public float LeftStrafe;
    public float RightStrafe;
}
