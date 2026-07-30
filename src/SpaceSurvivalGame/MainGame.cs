using System;
using Arch.Core;
using Box2dNet.Interop;
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

public class MainGame : Game
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
    private float _deathElapsedSeconds;
    private OxygenPickupField.PickupAssets _pickupAssets;
    private IronPickupField.PickupAssets _ironAssets;
    private RenderTarget2D _sceneRenderTarget;
    private Effect _suffocationEffect;
    private readonly Random _random = new();
    private System.Numerics.Vector2 _shipSpawnPositionMeters;
    private KeyboardState _previousKeyboardState;
    private readonly InputModeTracker _inputMode = new();

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
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular);
        var mouse = Mouse.GetState();
        if (gamePad.Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        // Decay screen shake every frame regardless of game state — otherwise it freezes
        // during Dying/GameOver (since those branches return before reaching the old call
        // site further down) and then visibly resumes/jolts once Playing starts back up
        // after a Restart, even though the hit that caused it was long past.
        _camera.UpdateShake((float)gameTime.ElapsedGameTime.TotalSeconds, _configs.ScreenShake.ShakeDecaySpeed);

        if (_gameState == GameState.StartScreen || _gameState == GameState.GameOver)
        {
            // Menus always show a free, visible cursor — cursor lock/hide is a Playing-only concern.
            IsMouseVisible = true;
            WindowsCursorLock.Release();

            var button = _gameState == GameState.StartScreen ? _startButton : _restartButton;
            var clickedButton = mouse.LeftButton == ButtonState.Pressed && _previousMenuMouseState.LeftButton == ButtonState.Released
                                 && button.IsHovered(mouse.Position);
            var confirmedViaKeyboardOrPad = (keyboard.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
                                            || (keyboard.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
                                            || gamePad.Buttons.Start == ButtonState.Pressed
                                            || gamePad.Buttons.A == ButtonState.Pressed;

            if (clickedButton || confirmedViaKeyboardOrPad)
            {
                if (_gameState == GameState.GameOver)
                {
                    ShipEntity.Respawn(_world, _shipSpawnPositionMeters);
                    StationCoreEntity.Show(_world); // undoes the Hide from death, if it was still Attached
                    ParticleSystem.Clear(_world); // no leftover explosion sparks/ship fragments carrying over from the previous life
                    _camera.PositionMeters = _shipSpawnPositionMeters;
                    _camera.TargetPositionMeters = _shipSpawnPositionMeters;
                }

                _inputMode.NotifyInputReceived(); // clicking/confirming counts as the real input that unlocks the cursor for Playing
                _gameState = GameState.Playing;
            }

            _previousMenuMouseState = mouse;
            _previousKeyboardState = keyboard;
            base.Update(gameTime);
            return;
        }

        if (_gameState == GameState.Dying)
        {
            // A brief cutscene: no player input, but physics/particles/asteroids keep animating
            // (including the ship's own residual momentum from Box2D) so the explosion and the
            // dead ship drifting still read as part of the world, not a frozen snapshot.
            IsMouseVisible = true;
            WindowsCursorLock.Release();

            var dyingDeltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _physicsWorld.Step(dyingDeltaSeconds);
            ParticleSystem.Run(_world, dyingDeltaSeconds);
            PhysicsSyncSystem.Run(_world);
            ShipEntity.Hide(_world); // re-assert each frame — HitFlashSystem still runs once more in the Playing frame where death triggers (after the initial Hide() call) and clobbers it back to visible

            _deathElapsedSeconds += dyingDeltaSeconds;
            if (_deathElapsedSeconds >= _configs.DeathSequence.Fade.DelaySeconds + _configs.DeathSequence.Fade.DurationSeconds)
                _gameState = GameState.GameOver;

            _previousMenuMouseState = mouse;
            _previousKeyboardState = keyboard;
            base.Update(gameTime);
            return;
        }

        if (keyboard.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R))
        {
            ShipEntity.Respawn(_world, _shipSpawnPositionMeters);
            StationCoreEntity.Show(_world);
            ParticleSystem.Clear(_world);
        }

        var mousePosition = mouse.Position;
        IsMouseVisible = _inputMode.Update(keyboard, mouse, gamePad, IsActive, Window.ClientBounds);

        // The cursor's direction from the ship's on-screen position — used both as a mouse
        // facing override (while RMB is held, mirroring the right stick) and for the camera
        // look-ahead below. Uses last frame's synced Transform (one frame stale, imperceptible).
        // Only while focused — unfocused input shouldn't affect facing/camera at all.
        System.Numerics.Vector2? cursorDirectionFromShip = null;
        if (_inputMode.HasReceivedInput && IsActive && !_inputMode.UseController && CameraFollowSystem.TryGetShipPositionMeters(_world, out var shipPositionForAim))
        {
            var shipScreenPixels = _camera.WorldToScreen(shipPositionForAim).ToNumerics();
            var cursorScreenPixels = new System.Numerics.Vector2(mousePosition.X, mousePosition.Y);
            cursorDirectionFromShip = cursorScreenPixels - shipScreenPixels;
        }

        var mouseFacingDirection = mouse.RightButton == ButtonState.Pressed ? cursorDirectionFromShip : null;

        var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

#if DEBUG
        _fpsFrameCount++;
        _fpsTimerSeconds += deltaSeconds;
        if (_fpsTimerSeconds >= 1f)
        {
            _fps = _fpsFrameCount;
            _fpsFrameCount = 0;
            _fpsTimerSeconds -= 1f;
        }
#endif

        ShipInputSystem.Run(_world, keyboard, gamePad, _inputMode.UseController, mouseFacingDirection, deltaSeconds, _configs.Engine);
        RotationJetSystem.Run(_world, _assets.RotationJet, _configs.Engine, _configs.Ship.SpriteSizePixels, _assets.RotationJetColor, _random);
        _physicsWorld.Step(deltaSeconds);
        CollisionDamageSystem.Run(_world, _physicsWorld, _configs.Player, _assets.Spark, _random, _configs.Spark, _camera, _configs.ScreenShake, _configs.HitFlash, _configs.HudFeedback); // must read hit events before the next Step overwrites them
        OxygenCrystalReleaseSystem.Run(_world, _physicsWorld, _pickupAssets, _configs.OxygenPickup, _configs.World.Asteroid.OxygenRich, _random, deltaSeconds); // same hit-event buffer, same must-run-before-next-Step constraint
        IronOreReleaseSystem.Run(_world, _physicsWorld, _ironAssets, _configs.IronPickup, _configs.World.Asteroid.IronRich, _random, deltaSeconds); // same hit-event buffer, same must-run-before-next-Step constraint

        var shipHealth = float.MaxValue;
        _world.Query(in HealthQuery, (ref Health health) => shipHealth = health.Current);
        if (shipHealth <= 0f && CameraFollowSystem.TryGetShipPositionMeters(_world, out var deathPositionMeters))
        {
            for (var i = 0; i < _configs.DeathSequence.Explosion.BurstCount; i++)
                ParticleEffects.SpawnExplosionBurst(_world, _assets.Spark, deathPositionMeters, _random, _configs.DeathSequence);

            // Read the Box2D body directly rather than the ECS Velocity component — PhysicsSyncSystem
            // (which mirrors Box2D into Velocity) hasn't run yet this frame, so Velocity would still
            // be last frame's value; the body itself already reflects the collision this Step() just
            // resolved, so fragments fly off the way the ship itself actually bounced.
            var shipVelocity = System.Numerics.Vector2.Zero;
            _world.Query(in PlayerPhysicsBodyQuery, (ref PhysicsBody physicsBody) => shipVelocity = B2Api.b2Body_GetLinearVelocity(physicsBody.BodyId));
            ShipFragments.SpawnDebris(_world, _assets.ShipFragmentTextures, deathPositionMeters, shipVelocity, _random, _configs.DeathSequence);
            ShipEntity.Hide(_world);
            StationCoreEntity.Hide(_world); // no-op if it already detached and became its own object

            _gameState = GameState.Dying;
            _deathElapsedSeconds = 0f;
        }

        VitalsSystem.Run(_world, deltaSeconds, _configs.Player, _configs.Suffocation);
        OxygenPickupSystem.Run(_world, _configs.Ship, _configs.OxygenPickup, _configs.Spark, _assets.Spark, _configs.FloatingText, _camera, _random);
        IronPickupSystem.Run(_world, _configs.Ship, _configs.IronPickup, _configs.Spark, _assets.Spark, _configs.FloatingText, _camera, _random);

        // Suffocation kills once its post-process effect has fully played out. No explosion
        // and no extra fade here — the screen's already fully black from the vignette by
        // this point, so jump straight to GameOver instead of the collision-death sequence.
        if (_gameState == GameState.Playing)
        {
            var suffocationElapsedSeconds = 0f;
            _world.Query(in SuffocationQuery, (ref Suffocation suffocation) => suffocationElapsedSeconds = suffocation.ElapsedSeconds);
            if (suffocationElapsedSeconds >= _configs.Suffocation.EffectDurationSeconds)
            {
                _world.Query(in HealthQuery, (ref Health health) => health.Current = 0f);
                _gameState = GameState.GameOver;
            }
        }

        ParticleSystem.Run(_world, deltaSeconds);
        FloatingTextSystem.Run(_world, deltaSeconds);
        HitFlashSystem.Run(_world, deltaSeconds, _configs.HitFlash);
        InvulnerabilitySystem.Run(_world, deltaSeconds);
        HudFeedbackSystem.Run(_world, deltaSeconds, _configs.HudFeedback, _random);
        SpeedCapSystem.Run(_world, deltaSeconds, _configs.Ship.SpeedCapEaseSpeed);
        PhysicsSyncSystem.Run(_world);
        // Must run after PhysicsSyncSystem so it copies the ship's just-synced position for
        // this frame, not last frame's stale value (a one-frame lag reads as constant drift
        // while riding along).
        StationCoreSystem.Run(_world, _camera, _physicsWorld, _configs.StationCore, deltaSeconds, _random, _assets.StationCoreDriftPuff, _assets.StationCoreDriftPuffColor);

        // Camera casts out toward wherever the aim input points, not the ship's facing
        // (which lags behind at a capped turn rate): the right stick's own direction in
        // controller mode; in mouse mode, a point MouseFocusRatio of the way from the
        // ship's on-screen position to the cursor's — only while RMB is held (same gate as
        // mouseFacingDirection), so idly moving the mouse without aiming doesn't drag the
        // camera around.
        System.Numerics.Vector2 lookAheadOffsetMeters;
        if (_inputMode.UseController)
        {
            var rightStick = new System.Numerics.Vector2(gamePad.ThumbSticks.Right.X, -gamePad.ThumbSticks.Right.Y);
            if (rightStick.LengthSquared() > 1f) rightStick = System.Numerics.Vector2.Normalize(rightStick);
            lookAheadOffsetMeters = rightStick * _configs.Camera.MaxDistanceMeters;
        }
        else if (mouseFacingDirection.HasValue)
        {
            lookAheadOffsetMeters = PhysicsWorld.PixelsToMeters(mouseFacingDirection.Value * _configs.Camera.MouseFocusRatio);
        }
        else
        {
            lookAheadOffsetMeters = System.Numerics.Vector2.Zero;
        }

        // Tweens in both modes now — since the look-ahead offset itself only engages while
        // RMB is held (or the right stick is pushed), an instant snap read as an abrupt jump
        // right at the moment of pressing/releasing; easing that transition in and out feels
        // smoother without lagging behind the cursor's own live position while held.
        CameraFollowSystem.Run(_world, _camera, lookAheadOffsetMeters, deltaSeconds, _configs.Camera.TweenSpeed,
            _configs.Camera.StrafeZoomMultiplier, _configs.Camera.ZoomTweenSpeed);

        _previousKeyboardState = keyboard;
        _previousMenuMouseState = mouse;
        base.Update(gameTime);
    }

    private static readonly QueryDescription HealthQuery = new QueryDescription().WithAll<Health, PlayerControlled>();
    private static readonly QueryDescription PlayerPhysicsBodyQuery = new QueryDescription().WithAll<PhysicsBody, PlayerControlled>();

    private static readonly QueryDescription SuffocationQuery = new QueryDescription().WithAll<Suffocation>();

    protected override void Draw(GameTime gameTime)
    {
        // Everything (world + HUD) draws into an offscreen target first so the suffocation
        // effect below — grayscale/pixelation/vignette — can post-process the whole frame
        // as one composited image, rather than each piece separately.
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

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        var suffocationSeconds = 0f;
        _world.Query(in SuffocationQuery, (ref Suffocation suffocation) => suffocationSeconds = suffocation.ElapsedSeconds);
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

        // Fades to fully opaque black across Dying's FadeDelaySeconds..+FadeDurationSeconds
        // window, then stays there through GameOver (elapsed is never advanced again once
        // GameOver is reached, so this keeps evaluating to 1).
        var deathFadeAlpha = 0f;
        if (_gameState == GameState.Dying || _gameState == GameState.GameOver)
        {
            var fadeElapsed = _deathElapsedSeconds - _configs.DeathSequence.Fade.DelaySeconds;
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

        base.Draw(gameTime);
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
