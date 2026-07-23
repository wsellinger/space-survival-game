using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.ECS.Components;

/// <summary>A short-lived, non-physics visual effect (e.g. a collision spark). Fades linearly to transparent over its lifetime; ParticleSystem integrates its Transform directly from Velocity since it has no PhysicsBody.</summary>
public struct Particle
{
    public float RemainingSeconds;
    public float TotalSeconds;
    public Color BaseColor;
}
