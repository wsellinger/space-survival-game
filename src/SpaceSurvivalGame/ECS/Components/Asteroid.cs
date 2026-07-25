namespace SpaceSurvivalGame.ECS.Components;

public enum AsteroidType
{
    Ordinary,
    OxygenRich
}

/// <summary>Marks an entity as an asteroid. Minimal for now — natural place to add resource yield/health once mining exists.</summary>
public struct Asteroid
{
    public float RadiusMeters;
    public AsteroidType Type;
}
