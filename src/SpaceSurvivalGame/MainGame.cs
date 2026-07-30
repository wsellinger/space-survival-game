using System;
using System.Collections.Generic;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.ECS;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.ECS.Systems;
using SpaceSurvivalGame.Input;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Platform;
using SpaceSurvivalGame.Rendering;

using SpaceSurvivalGame.Configuration;

namespace SpaceSurvivalGame;

public class MainGame : Game, IGameHost
{
    private const int WindowWidth = 1920;
    private const int WindowHeight = 1080;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _uiFont;
#if DEBUG
    private float _fpsTimerSeconds;
    private int _fpsFrameCount;
    private int _fps;
#endif
    private GameState _gameState = GameState.StartScreen;
    private UiButton _startButton;
    private UiButton _restartButton;
    private MouseState _previousMenuMouseState;
    private PhysicsWorld _physicsWorld;
    private World _world;
    private Camera _camera;
    private GameConfigs _configs;
    private GameAssets _assets;
    private OxygenPickupField.PickupAssets _pickupAssets;
    private IronPickupField.PickupAssets _ironAssets;
    private RenderTarget2D _sceneRenderTarget;
    private Effect _suffocationEffect;
    private readonly Random _random = new();
    private System.Numerics.Vector2 _shipSpawnPositionMeters;
    private KeyboardState _previousKeyboardState;
    private readonly InputModeTracker _inputMode = new();
    private readonly DeathTimer _deathTimer = new();
    private Dictionary<GameState, IGameStateHandler> _stateHandlers;

    public Rectangle ClientBounds => Window.ClientBounds;

    public MainGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = WindowWidth,
            PreferredBackBufferHeight = WindowHeight
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _physicsWorld = new PhysicsWorld();
        _world = World.Create();
        _camera = new Camera { ViewportWidth = WindowWidth, ViewportHeight = WindowHeight };

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _uiFont = Content.Load<SpriteFont>("Fonts/DebugFont");

        _configs = GameConfigs.Load();
        _assets = GameAssets.Load(GraphicsDevice, _configs, WindowWidth, WindowHeight, _random);

        _suffocationEffect = Content.Load<Effect>("Shaders/SuffocationEffect");
        _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, WindowWidth, WindowHeight);

        _shipSpawnPositionMeters = PhysicsWorld.PixelsToMeters(new System.Numerics.Vector2(WindowWidth / 2f, WindowHeight / 2f));
        _camera.PositionMeters = _shipSpawnPositionMeters;
        _camera.TargetPositionMeters = _shipSpawnPositionMeters;
        ShipEntity.Create(_world, _physicsWorld, GraphicsDevice, _shipSpawnPositionMeters, _configs.Ship, _configs.Player, _configs.StationCore.CoreDot.SpriteSizePixels);
        StationCoreEntity.Create(_world, _shipSpawnPositionMeters, _assets.StationCore, _configs.StationCore.CoreDot.SpriteSizePixels);

        foreach (var layer in _configs.Starfield.Layers)
        {
            var color = Microsoft.Xna.Framework.Color.White * layer.Brightness;
            Starfield.Create(_world, GraphicsDevice, _shipSpawnPositionMeters, layer.HalfExtentMeters, layer.StarCount, layer.Parallax, color,
                _configs.Starfield.TintStrengthRange.Min, _configs.Starfield.TintStrengthRange.Max);
        }

        AsteroidField.Create(_world, _physicsWorld, GraphicsDevice, _shipSpawnPositionMeters, _configs.World, _configs.OxygenPickup, _configs.IronPickup);
        _pickupAssets = OxygenPickupField.Create(_world, _physicsWorld, GraphicsDevice, _shipSpawnPositionMeters, _configs.World, _configs.OxygenPickup);
        _ironAssets = IronPickupField.Create(_world, _physicsWorld, GraphicsDevice, _shipSpawnPositionMeters, _configs.World, _configs.IronPickup);

        const int buttonWidth = 220;
        const int buttonHeight = 60;
        var buttonBounds = new Rectangle((WindowWidth - buttonWidth) / 2, (WindowHeight - buttonHeight) / 2, buttonWidth, buttonHeight);
        _startButton = new UiButton(buttonBounds, "START");
        _restartButton = new UiButton(buttonBounds, "RESTART");

        _stateHandlers = new Dictionary<GameState, IGameStateHandler>
        {
            [GameState.StartScreen] = new MenuStateHandler(_world, _camera, _shipSpawnPositionMeters, _startButton, isGameOverVariant: false, _inputMode, this),
            [GameState.GameOver] = new MenuStateHandler(_world, _camera, _shipSpawnPositionMeters, _restartButton, isGameOverVariant: true, _inputMode, this),
            [GameState.Dying] = new DyingStateHandler(_world, _physicsWorld, _configs.DeathSequence, _deathTimer, this),
            [GameState.Playing] = new PlayingStateHandler(_world, _physicsWorld, _camera, _configs, _assets, _pickupAssets, _ironAssets, _random,
                _inputMode, _deathTimer, _shipSpawnPositionMeters, this)
        };
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular);
        var mouse = Mouse.GetState();
        if (gamePad.Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        // Decay screen shake every frame regardless of game state — otherwise it freezes
        // during Dying/GameOver (since those states don't run the Playing pipeline that used
        // to reach it) and then visibly resumes/jolts once Playing starts back up after a
        // Restart, even though the hit that caused it was long past.
        _camera.UpdateShake((float)gameTime.ElapsedGameTime.TotalSeconds, _configs.ScreenShake.ShakeDecaySpeed);

#if DEBUG
        // Only advances during Playing frames, matching the original behavior from before
        // dispatch went through per-state handlers (the FPS counter used to live textually
        // inside the Playing-only branch).
        if (_gameState == GameState.Playing)
        {
            var fpsDeltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _fpsFrameCount++;
            _fpsTimerSeconds += fpsDeltaSeconds;
            if (_fpsTimerSeconds >= 1f)
            {
                _fps = _fpsFrameCount;
                _fpsFrameCount = 0;
                _fpsTimerSeconds -= 1f;
            }
        }
#endif

        var nextState = _stateHandlers[_gameState].Update(gameTime, keyboard, gamePad, mouse, _previousKeyboardState, _previousMenuMouseState);
        if (nextState.HasValue) _gameState = nextState.Value;

        _previousKeyboardState = keyboard;
        _previousMenuMouseState = mouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        DrawSceneToRenderTarget(gameTime);
        ApplySuffocationPostProcess(gameTime);
        DrawOverlays(gameTime);
        base.Draw(gameTime);
    }

    /// <summary>Everything (world + HUD) draws into an offscreen target first so ApplySuffocationPostProcess can post-process the whole frame as one composited image, rather than each piece separately.</summary>
    private void DrawSceneToRenderTarget(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
        GraphicsDevice.Clear(Color.Black);

        // BackToFront so LayerDepth actually controls draw order (stars behind everything);
        // PointClamp instead of the default linear filter so scaled sprites (asteroids) get
        // crisp edges instead of blurring when magnified/minified.
        _spriteBatch.Begin(SpriteSortMode.BackToFront, samplerState: SamplerState.PointClamp);
        RenderSystem.Run(_world, _spriteBatch, _camera);
        StationCoreBuildEffectRenderSystem.Run(_world, _spriteBatch, _camera, _configs.StationCore, _assets.StationCoreBuildEffect);
        EngineJetRenderer.Run(_world, _spriteBatch, _camera, _configs.Engine, _configs.Ship.SpriteSizePixels, _configs.Ship.NotchDepthFraction, _assets.Flame, (float)gameTime.TotalGameTime.TotalSeconds, _assets.EngineJetOuterColor, _assets.EngineJetInnerColor);
        MetallicSparkleRenderSystem.Run(_world, _spriteBatch, _camera, _configs.IronPickup, (float)gameTime.TotalGameTime.TotalSeconds, _assets.IronSparkleColor);
        StationCoreShockwaveRenderSystem.Run(_world, _spriteBatch, _camera, _configs.StationCore, _assets.StationCoreShockwave, _assets.StationCoreShockwaveColor);
        _spriteBatch.End();

        // Separate screen-space pass (no camera transform) for HUD/debug text.
        _spriteBatch.Begin();
        FloatingTextRenderSystem.Run(_world, _spriteBatch, _uiFont, _configs.FloatingText.TextScale);
        ScreenWarningRenderer.Run(_world, _spriteBatch, _configs.ScreenWarning, _configs.HealthWarning, _configs.OxygenWarning, _configs.HudFeedback,
            (float)gameTime.TotalGameTime.TotalSeconds, _assets.ScreenWarningOutline, _assets.ScreenWarningVignette);
        StrafeModeIndicatorRenderer.Run(_world, _spriteBatch, _uiFont, _configs.StrafeModeIndicator, _assets.StrafeModeIndicator, (float)gameTime.TotalGameTime.TotalSeconds, _assets.StrafeModeIndicatorColor);
        // Drawn after StrafeModeIndicatorRenderer so the bottom-left iron counter reads on top of
        // that corner's bracket instead of being drawn under it.
        HudRenderer.Run(_world, _spriteBatch, WindowWidth, WindowHeight, _configs.Hud, _configs.HudFeedback, _configs.HealthWarning, _configs.OxygenWarning,
            _uiFont, (float)gameTime.TotalGameTime.TotalSeconds, _assets.HudBarFill, _assets.HudBarOutline);
#if DEBUG
        _spriteBatch.DrawString(_uiFont, $"FPS: {_fps}", new Microsoft.Xna.Framework.Vector2(10, 10), Color.White);
#endif
        _spriteBatch.End();
    }

    /// <summary>Reads _sceneRenderTarget (only valid once DrawSceneToRenderTarget has fully completed) and blits it through the suffocation shader onto the backbuffer.</summary>
    private void ApplySuffocationPostProcess(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        var suffocationSeconds = PlayerDeathSystem.GetSuffocationElapsedSeconds(_world);
        var suffocationProgress = MathHelper.Clamp(suffocationSeconds / _configs.Suffocation.EffectDurationSeconds, 0f, 1f);

        var pixelBlockSizePixels = _configs.Suffocation.Pixelation.Enabled ? _configs.Suffocation.Pixelation.MaxBlockSizePixels * suffocationProgress : 0f;
        _suffocationEffect.Parameters["PixelBlockSizeUV"].SetValue(new Vector2(pixelBlockSizePixels / WindowWidth, pixelBlockSizePixels / WindowHeight));
        var grayscaleIntensity = MathF.Pow(suffocationProgress, _configs.Suffocation.Grayscale.EaseExponent);
        _suffocationEffect.Parameters["GrayscaleIntensity"].SetValue(grayscaleIntensity);
        var vignetteProgress = MathF.Pow(suffocationProgress, _configs.Suffocation.Vignette.EaseExponent);
        _suffocationEffect.Parameters["VignetteRadius"].SetValue(MathHelper.Lerp(_configs.Suffocation.Vignette.StartRadius, 0f, vignetteProgress));
        _suffocationEffect.Parameters["VignetteFeatherRadius"].SetValue(_configs.Suffocation.Vignette.FeatherRadius);
        _suffocationEffect.Parameters["AspectRatio"].SetValue(new Vector2(WindowWidth / (float)WindowHeight, 1f));
        _suffocationEffect.Parameters["NoiseCellCount"].SetValue(new Vector2(WindowWidth / _configs.Suffocation.Noise.GrainSizePixels, WindowHeight / _configs.Suffocation.Noise.GrainSizePixels));
        _suffocationEffect.Parameters["NoiseIntensity"].SetValue(_configs.Suffocation.Noise.MaxIntensity * suffocationProgress);
        _suffocationEffect.Parameters["NoiseAdditiveBlend"].SetValue(_configs.Suffocation.Noise.AdditiveBlend ? 1f : 0f);
        _suffocationEffect.Parameters["NoiseTimeSeed"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);

        _spriteBatch.Begin(effect: _suffocationEffect, samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_sceneRenderTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();
    }

    /// <summary>Death fade, crosshair, and start/game-over menu — screen-space overlays drawn on top of the post-processed backbuffer. Must run after ApplySuffocationPostProcess resets the render target to null.</summary>
    private void DrawOverlays(GameTime gameTime)
    {
        // Fades to fully opaque black across Dying's FadeDelaySeconds..+FadeDurationSeconds
        // window, then stays there through GameOver (elapsed is never advanced again once
        // GameOver is reached, so this keeps evaluating to 1).
        var deathFadeAlpha = 0f;
        if (_gameState == GameState.Dying || _gameState == GameState.GameOver)
        {
            var fadeElapsed = _deathTimer.ElapsedSeconds - _configs.DeathSequence.Fade.DelaySeconds;
            deathFadeAlpha = _gameState == GameState.GameOver ? 1f : MathHelper.Clamp(fadeElapsed / _configs.DeathSequence.Fade.DurationSeconds, 0f, 1f);
        }

        if (deathFadeAlpha > 0f)
        {
            _spriteBatch.Begin();
            _spriteBatch.Draw(_assets.SolidPixel, new Rectangle(0, 0, WindowWidth, WindowHeight), Color.Black * deathFadeAlpha);
            _spriteBatch.End();
        }

        // Only while the real OS cursor is actually hidden/locked (mouse mode, Playing, past
        // the first real input) — otherwise the player already has the visible system cursor.
        if (_gameState == GameState.Playing && _inputMode.HasReceivedInput && !_inputMode.UseController)
        {
            var crosshairOrigin = new Vector2(_configs.Crosshair.SizePixels / 2f, _configs.Crosshair.SizePixels / 2f);
            _spriteBatch.Begin();
            _spriteBatch.Draw(_assets.Crosshair, Mouse.GetState().Position.ToVector2(), null, Color.White, 0f, crosshairOrigin, 1f, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }

        if (_gameState == GameState.StartScreen || _gameState == GameState.GameOver)
        {
            var button = _gameState == GameState.StartScreen ? _startButton : _restartButton;
            var title = _gameState == GameState.StartScreen ? "STATION" : "YOU DIED";
            var isHovered = button.IsHovered(Mouse.GetState().Position);

            _spriteBatch.Begin();
            MenuRenderer.Draw(_spriteBatch, _uiFont, _assets.SolidPixel, _assets.ButtonFill, _assets.ButtonOutline,
                WindowWidth, WindowHeight, title, button, isHovered);
            _spriteBatch.End();
        }
    }

    private static readonly QueryDescription SpriteQuery = new QueryDescription().WithAll<Sprite>();

    protected override void UnloadContent()
    {
        WindowsCursorLock.Release();
        _world.Query(in SpriteQuery, (ref Sprite sprite) => sprite.Texture.Dispose());
        _assets.Dispose();
        _sceneRenderTarget.Dispose();
        World.Destroy(_world);
        _physicsWorld.Dispose();
        base.UnloadContent();
    }
}
