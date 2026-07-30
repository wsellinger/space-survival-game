namespace SpaceSurvivalGame;

/// <summary>
/// Shared mutable elapsed-time counter for the Dying cutscene. Crosses handler boundaries:
/// PlayingStateHandler resets it when a death triggers, DyingStateHandler advances it every
/// frame, and MainGame.Draw reads it to drive the death-fade overlay.
/// </summary>
public class DeathTimer
{
    public float ElapsedSeconds { get; set; }
}
