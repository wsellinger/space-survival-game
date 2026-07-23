using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Drives the HUD health bar's brief flash+shake on damage. RemainingSeconds counts down for the flash fade (read directly by HudRenderer); ShakeMagnitudePixels decays exponentially and ShakeOffsetPixels is refreshed from it each frame — both updated by HudFeedbackSystem.</summary>
public struct HealthBarFeedback
{
    public float RemainingSeconds;
    public float ShakeMagnitudePixels;
    public Vector2 ShakeOffsetPixels;
}
