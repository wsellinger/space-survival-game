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

    // OxygenRich only: while > 0, OxygenCrystalReleaseSystem won't roll a fresh crystal spawn
    // for this asteroid even on a qualifying hit — a single jittery real-world contact can
    // register as several distinct Box2D hit events across consecutive frames, which without
    // this would otherwise read as one collision but pop loose a dozen-plus crystals at once.
    public float CrystalReleaseCooldownSecondsRemaining;
}
