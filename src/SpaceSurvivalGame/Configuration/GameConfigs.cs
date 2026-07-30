using System;
using System.IO;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Every config file the game loads at startup, collected into one bag so MainGame doesn't
/// carry 20+ individual config fields plus their individual Path.Combine+Load call sites.
/// </summary>
public class GameConfigs
{
    public ShipConfig Ship { get; private init; }
    public CameraConfig Camera { get; private init; }
    public WorldConfig World { get; private init; }
    public StarfieldConfig Starfield { get; private init; }
    public PlayerConfig Player { get; private init; }
    public HudConfig Hud { get; private init; }
    public SparkConfig Spark { get; private init; }
    public HitFlashConfig HitFlash { get; private init; }
    public ScreenShakeConfig ScreenShake { get; private init; }
    public HudFeedbackConfig HudFeedback { get; private init; }
    public HealthWarningConfig HealthWarning { get; private init; }
    public OxygenWarningConfig OxygenWarning { get; private init; }
    public SuffocationEffectConfig Suffocation { get; private init; }
    public DeathSequenceConfig DeathSequence { get; private init; }
    public OxygenPickupConfig OxygenPickup { get; private init; }
    public IronPickupConfig IronPickup { get; private init; }
    public FloatingTextConfig FloatingText { get; private init; }
    public StationCoreConfig StationCore { get; private init; }
    public ScreenWarningConfig ScreenWarning { get; private init; }
    public EngineConfig Engine { get; private init; }
    public CrosshairConfig Crosshair { get; private init; }
    public StrafeModeIndicatorConfig StrafeModeIndicator { get; private init; }

    public static GameConfigs Load()
    {
        string ConfigPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "config", fileName);

        return new GameConfigs
        {
            Ship = ShipConfig.Load(ConfigPath("ship-config.json")),
            Camera = CameraConfig.Load(ConfigPath("camera-config.json")),
            World = WorldConfig.Load(ConfigPath("world-config.json")),
            Starfield = StarfieldConfig.Load(ConfigPath("starfield-config.json")),
            Player = PlayerConfig.Load(ConfigPath("player-config.json")),
            Hud = HudConfig.Load(ConfigPath("hud-config.json")),
            Spark = SparkConfig.Load(ConfigPath("spark-config.json")),
            HitFlash = HitFlashConfig.Load(ConfigPath("hit-flash-config.json")),
            ScreenShake = ScreenShakeConfig.Load(ConfigPath("screen-shake-config.json")),
            HudFeedback = HudFeedbackConfig.Load(ConfigPath("hud-feedback-config.json")),
            HealthWarning = HealthWarningConfig.Load(ConfigPath("health-warning-config.json")),
            OxygenWarning = OxygenWarningConfig.Load(ConfigPath("oxygen-warning-config.json")),
            Suffocation = SuffocationEffectConfig.Load(ConfigPath("suffocation-effect-config.json")),
            DeathSequence = DeathSequenceConfig.Load(ConfigPath("death-sequence-config.json")),
            OxygenPickup = OxygenPickupConfig.Load(ConfigPath("oxygen-pickup-config.json")),
            IronPickup = IronPickupConfig.Load(ConfigPath("iron-pickup-config.json")),
            FloatingText = FloatingTextConfig.Load(ConfigPath("floating-text-config.json")),
            StationCore = StationCoreConfig.Load(ConfigPath("station-core-config.json")),
            ScreenWarning = ScreenWarningConfig.Load(ConfigPath("screen-warning-config.json")),
            Engine = EngineConfig.Load(ConfigPath("engine-config.json")),
            Crosshair = CrosshairConfig.Load(ConfigPath("crosshair-config.json")),
            StrafeModeIndicator = StrafeModeIndicatorConfig.Load(ConfigPath("strafe-mode-indicator-config.json"))
        };
    }
}
