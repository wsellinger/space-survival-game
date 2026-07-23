namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Counts down after a hit; HitFlashSystem fades Sprite.Color from red back to white as this reaches zero.</summary>
public struct HitFlash
{
    public float RemainingSeconds;
}
