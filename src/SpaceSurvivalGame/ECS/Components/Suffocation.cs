namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Tracks how long Oxygen has been at zero (reset to 0 the instant it rises above 0), driving the screen-wide suffocation post-process effect in MainGame.Draw.</summary>
public struct Suffocation
{
    public float ElapsedSeconds;
}
