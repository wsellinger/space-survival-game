using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Tunable death-sequence values (explosion, then fade to black, then the game-over screen),
/// loaded from a JSON file next to the executable so they can be edited without recompiling.
/// If the file is missing, a default one is written out so there's always something to open
/// and tweak.
/// </summary>
public class DeathSequenceConfig
{
    // How many times ParticleEffects.SpawnExplosionBurst fires at the ship's position (each
    // burst is ExplosionSparkCountMin-Max particles).
    public int ExplosionBurstCount { get; set; } = 5;

    // Independent from ParticleConfig's collision-tap sparks, so the death explosion can be
    // made bigger/longer-lived without changing how ordinary hits look.
    public int ExplosionSparkCountMin { get; set; } = 8;
    public int ExplosionSparkCountMax { get; set; } = 14;
    public float ExplosionSparkSpeedMinMetersPerSecond { get; set; } = 1.5f;
    public float ExplosionSparkSpeedMaxMetersPerSecond { get; set; } = 4f;
    public float ExplosionSparkLifetimeMinSeconds { get; set; } = 0.2f;
    public float ExplosionSparkLifetimeMaxSeconds { get; set; } = 0.45f;
    public int ExplosionSparkSizePixels { get; set; } = 6;

    // Delay from the moment of death until the screen starts fading to black (during which
    // the explosion/fragments play out).
    public float FadeDelaySeconds { get; set; } = 1.5f;

    // How long the fade to black takes once it starts.
    public float FadeDurationSeconds { get; set; } = 1.5f;

    // How many hull fragments the ship visibly splits into on death (its own sprite is
    // hidden for the rest of the sequence, in favor of these). Fragments live for the whole
    // FadeDelaySeconds+FadeDurationSeconds, fading out right as the screen goes black.
    public int ShipFragmentCount { get; set; } = 3;
    public int FragmentSizePixels { get; set; } = 22;

    // Fragments inherit the ship's own velocity, plus a small kick in a direction within
    // +/-FragmentSpreadAngleRadians of the ship's own travel direction (or a random direction
    // if the ship was essentially stationary) — they drift apart gently while still generally
    // continuing the way the ship was already going, rather than exploding outward.
    public float FragmentMinSpeedMetersPerSecond { get; set; } = 0.1f;
    public float FragmentMaxSpeedMetersPerSecond { get; set; } = 0.4f;
    public float FragmentSpreadAngleRadians { get; set; } = 0.6f;

    public float FragmentMinAngularVelocityRadiansPerSecond { get; set; } = 2f;
    public float FragmentMaxAngularVelocityRadiansPerSecond { get; set; } = 6f;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static DeathSequenceConfig Load(string path)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<DeathSequenceConfig>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new DeathSequenceConfig();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
