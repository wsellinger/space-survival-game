using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>Independent from ParticleConfig's collision-tap sparks, so the death explosion can be made bigger/longer-lived without changing how ordinary hits look.</summary>
public class ExplosionConfig
{
    // How many times ParticleEffects.SpawnExplosionBurst fires at the ship's position (each
    // burst is SparkCountRange particles).
    public int BurstCount { get; set; } = 5;

    public IntRange SparkCountRange { get; set; } = new(8, 14);
    public FloatRange SparkSpeedMetersPerSecondRange { get; set; } = new(1.5f, 4f);
    public FloatRange SparkLifetimeSecondsRange { get; set; } = new(0.2f, 0.45f);
    public int SparkSizePixels { get; set; } = 6;
}

public class FadeConfig
{
    // Delay from the moment of death until the screen starts fading to black (during which
    // the explosion/fragments play out).
    public float DelaySeconds { get; set; } = 1.5f;

    // How long the fade to black takes once it starts.
    public float DurationSeconds { get; set; } = 1.5f;
}

/// <summary>
/// How many hull fragments the ship visibly splits into on death (its own sprite is hidden for
/// the rest of the sequence, in favor of these). Fragments live for the whole
/// Fade.DelaySeconds+Fade.DurationSeconds, fading out right as the screen goes black. They
/// inherit the ship's own velocity, plus a small kick in a direction within +/-SpreadAngleRadians
/// of the ship's own travel direction (or a random direction if the ship was essentially
/// stationary) — they drift apart gently while still generally continuing the way the ship was
/// already going, rather than exploding outward.
/// </summary>
public class FragmentsConfig
{
    public int Count { get; set; } = 3;
    public int SizePixels { get; set; } = 22;
    public FloatRange SpeedMetersPerSecondRange { get; set; } = new(0.1f, 0.4f);
    public float SpreadAngleRadians { get; set; } = 0.6f;
    public FloatRange AngularVelocityRadiansPerSecondRange { get; set; } = new(2f, 6f);
}

/// <summary>
/// Tunable death-sequence values (explosion, then fade to black, then the game-over screen),
/// loaded from a JSON file next to the executable so they can be edited without recompiling.
/// If the file is missing, a default one is written out so there's always something to open
/// and tweak.
/// </summary>
public class DeathSequenceConfig
{
    public ExplosionConfig Explosion { get; set; } = new();
    public FadeConfig Fade { get; set; } = new();
    public FragmentsConfig Fragments { get; set; } = new();

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
