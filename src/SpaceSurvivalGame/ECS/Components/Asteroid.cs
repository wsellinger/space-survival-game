namespace SpaceSurvivalGame.ECS.Components;

/// <summary>Mutually exclusive — an asteroid is exactly one of these, never a mix.</summary>
public enum AsteroidType
{
    Ordinary,
    OxygenRich,
    AnorthiteRich
}

/// <summary>Marks an entity as an asteroid. Minimal for now — natural place to add resource yield/health once mining exists.</summary>
public struct Asteroid
{
    public float RadiusMeters;
    public AsteroidType Type;

    // OxygenRich/AnorthiteRich only: while > 0, OxygenCrystalReleaseSystem/AnorthiteCrystalReleaseSystem
    // won't roll a fresh crystal spawn for this asteroid even on a qualifying hit — a single
    // jittery real-world contact can register as several distinct Box2D hit events across
    // consecutive frames, which without this would otherwise read as one collision but pop
    // loose a dozen-plus crystals at once. One shared field is fine since Type is mutually
    // exclusive — an asteroid only ever needs the cooldown for its own one special behavior.
    public float CrystalReleaseCooldownSecondsRemaining;
}
