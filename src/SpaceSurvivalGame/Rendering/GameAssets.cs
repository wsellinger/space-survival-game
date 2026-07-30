using System;
using Microsoft.Xna.Framework.Graphics;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Every texture baked by ProceduralTextures and every color parsed from config at startup,
/// collected into one bag so MainGame doesn't carry two dozen individual asset fields. Owns
/// disposal of everything it bakes. Deliberately excludes the pickup fields' own PickupAssets
/// (OxygenPickupField/IronPickupField.Create also populate the world, so those stay entangled
/// with world-init in MainGame rather than pure asset baking) and the render-target/shader
/// machinery (_sceneRenderTarget, _suffocationEffect), which isn't config-driven.
/// </summary>
public class GameAssets
{
    public Texture2D HudBarFill { get; private init; }
    public Texture2D HudBarOutline { get; private init; }
    public Texture2D ScreenWarningOutline { get; private init; }
    public Texture2D ScreenWarningVignette { get; private init; }
    public Texture2D[] ShipFragmentTextures { get; private init; }
    public Texture2D Flame { get; private init; }
    public Texture2D RotationJet { get; private init; }
    public Microsoft.Xna.Framework.Color RotationJetColor { get; private init; }
    public Microsoft.Xna.Framework.Color EngineJetOuterColor { get; private init; }
    public Microsoft.Xna.Framework.Color EngineJetInnerColor { get; private init; }
    public Texture2D Crosshair { get; private init; }
    public Texture2D StrafeModeIndicator { get; private init; }
    public Microsoft.Xna.Framework.Color StrafeModeIndicatorColor { get; private init; }
    public Texture2D Spark { get; private init; }
    public Microsoft.Xna.Framework.Color IronSparkleColor { get; private init; }
    public Texture2D StationCore { get; private init; }
    public Texture2D StationCoreBuildEffect { get; private init; }
    public Texture2D StationCoreShockwave { get; private init; }
    public Texture2D StationCoreDriftPuff { get; private init; }
    public Microsoft.Xna.Framework.Color StationCoreDriftPuffColor { get; private init; }
    public Microsoft.Xna.Framework.Color StationCoreShockwaveColor { get; private init; }
    public Texture2D ButtonFill { get; private init; }
    public Texture2D ButtonOutline { get; private init; }
    public Texture2D SolidPixel { get; private init; }

    public static GameAssets Load(GraphicsDevice graphicsDevice, GameConfigs configs, int windowWidth, int windowHeight, Random random)
    {
        var barCornerRadius = configs.Hud.Bars.ThicknessPixels / 2f;

        var stationCoreConfig = configs.StationCore;
        // Baked at exactly its own final size (see StationCoreShockwaveRenderSystem) so it only
        // ever shrinks, never upscales, staying crisp out to full size. Transparent center, tinted
        // white ring — actual color/alpha applied at draw time so Shockwave.ColorHex/MaxAlpha can
        // be re-tuned without re-baking.
        var shockwaveDiameterPixels = (int)Physics.PhysicsWorld.MetersToPixels(stationCoreConfig.Shockwave.RadiusMeters * 2f);

        var crosshairColor = ColorHex.Parse(configs.Crosshair.ColorHex);
        var strafeIndicatorInset = (int)configs.StrafeModeIndicator.InsetPixels;

        const int buttonWidth = 220;
        const int buttonHeight = 60;
        const float buttonCornerRadius = 14f;

        return new GameAssets
        {
            HudBarFill = ProceduralTextures.CreateRoundedRect(graphicsDevice, configs.Hud.Bars.LengthPixels, configs.Hud.Bars.ThicknessPixels, barCornerRadius, Microsoft.Xna.Framework.Color.White),
            HudBarOutline = ProceduralTextures.CreateRoundedRectOutline(graphicsDevice, configs.Hud.Bars.LengthPixels, configs.Hud.Bars.ThicknessPixels, barCornerRadius, configs.Hud.Bars.OutlineThicknessPixels, Microsoft.Xna.Framework.Color.White),
            Spark = ProceduralTextures.CreateCircle(graphicsDevice, configs.Spark.Burst.SizePixels, Microsoft.Xna.Framework.Color.White),
            IronSparkleColor = ColorHex.Parse(configs.IronPickup.Sparkle.ColorHex),

            StationCore = ProceduralTextures.CreateRingedCircle(graphicsDevice, stationCoreConfig.CoreDot.SpriteSizePixels,
                ColorHex.Parse(stationCoreConfig.CoreDot.ColorHex), ColorHex.Parse(stationCoreConfig.CoreDot.RingColorHex), stationCoreConfig.CoreDot.InnerRadiusFraction),
            StationCoreBuildEffect = ProceduralTextures.CreateCircuitSquare(graphicsDevice, stationCoreConfig.Build.MaxSizePixels,
                ColorHex.Parse(stationCoreConfig.Build.ColorHex), ColorHex.Parse(stationCoreConfig.Circuit.ColorHex), stationCoreConfig.Circuit.ThicknessFraction),
            StationCoreShockwave = ProceduralTextures.CreateRingedCircle(graphicsDevice, shockwaveDiameterPixels,
                Microsoft.Xna.Framework.Color.Transparent, Microsoft.Xna.Framework.Color.White, stationCoreConfig.Shockwave.RingInnerRadiusFraction),
            StationCoreDriftPuff = ProceduralTextures.CreateCircle(graphicsDevice, stationCoreConfig.Drift.Puffs.Puff.ParticleSizePixels, Microsoft.Xna.Framework.Color.White),
            StationCoreDriftPuffColor = ColorHex.Parse(stationCoreConfig.Drift.Puffs.Puff.ColorHex),
            StationCoreShockwaveColor = ColorHex.Parse(stationCoreConfig.Shockwave.ColorHex),

            ScreenWarningOutline = ProceduralTextures.CreateRoundedRectOutline(graphicsDevice, windowWidth, windowHeight, 0f, configs.ScreenWarning.OutlineThicknessPixels, Microsoft.Xna.Framework.Color.White),
            ScreenWarningVignette = ProceduralTextures.CreateEdgeVignette(graphicsDevice, windowWidth, windowHeight, configs.ScreenWarning.VignetteDepthPixels, Microsoft.Xna.Framework.Color.White),
            ShipFragmentTextures = ECS.ShipFragments.CreateFragmentTextures(graphicsDevice, random),

            // Baked white and tinted per-layer at draw time (see EngineJetRenderer) — shared by
            // both the outer and inner flame layers, which have independent colors.
            Flame = ProceduralTextures.CreateRightFacingTriangle(graphicsDevice, configs.Engine.FlameTextureSizePixels, Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.White),
            RotationJet = ProceduralTextures.CreateCircle(graphicsDevice, configs.Engine.RotationJets.Puff.ParticleSizePixels, Microsoft.Xna.Framework.Color.White),
            RotationJetColor = ColorHex.Parse(configs.Engine.RotationJets.Puff.ColorHex),
            EngineJetOuterColor = ColorHex.Parse(configs.Engine.MainJet.Outer.ColorHex),
            EngineJetInnerColor = ColorHex.Parse(configs.Engine.MainJet.Inner.ColorHex),

            Crosshair = ProceduralTextures.CreateCrosshair(graphicsDevice, configs.Crosshair.SizePixels, configs.Crosshair.GapRadiusPixels, configs.Crosshair.TickLengthPixels, configs.Crosshair.ThicknessPixels, crosshairColor),

            StrafeModeIndicatorColor = ColorHex.Parse(configs.StrafeModeIndicator.ColorHex),
            StrafeModeIndicator = ProceduralTextures.CreateCornerBrackets(graphicsDevice, windowWidth - strafeIndicatorInset * 2, windowHeight - strafeIndicatorInset * 2,
                configs.StrafeModeIndicator.CornerRadiusPixels, configs.StrafeModeIndicator.OutlineThicknessPixels, configs.StrafeModeIndicator.ArmLengthPixels, Microsoft.Xna.Framework.Color.White),

            ButtonFill = ProceduralTextures.CreateRoundedRect(graphicsDevice, buttonWidth, buttonHeight, buttonCornerRadius, Microsoft.Xna.Framework.Color.White),
            ButtonOutline = ProceduralTextures.CreateRoundedRectOutline(graphicsDevice, buttonWidth, buttonHeight, buttonCornerRadius, 2f, Microsoft.Xna.Framework.Color.White),
            SolidPixel = ProceduralTextures.CreateSolidSquare(graphicsDevice, 1, Microsoft.Xna.Framework.Color.White)
        };
    }

    public void Dispose()
    {
        HudBarFill.Dispose();
        HudBarOutline.Dispose();
        ScreenWarningOutline.Dispose();
        ScreenWarningVignette.Dispose();
        foreach (var texture in ShipFragmentTextures) texture.Dispose();
        Flame.Dispose();
        RotationJet.Dispose();
        Crosshair.Dispose();
        StrafeModeIndicator.Dispose();
        Spark.Dispose();
        StationCoreBuildEffect.Dispose();
        StationCoreShockwave.Dispose();
        StationCoreDriftPuff.Dispose();
        ButtonFill.Dispose();
        ButtonOutline.Dispose();
        SolidPixel.Dispose();
    }
}
