using System;
using System.IO;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.ECS;
using SpaceSurvivalGame.ECS.Components;
using SpaceSurvivalGame.ECS.Systems;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Platform;
using SpaceSurvivalGame.Rendering;

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
    private Texture2D _buttonFillTexture;
    private Texture2D _buttonOutlineTexture;
    private Texture2D _solidPixelTexture;
    private UiButton _startButton;
    private UiButton _restartButton;
    private MouseState _previousMenuMouseState;
    private PhysicsWorld _physicsWorld;
    private World _world;
    private Camera _camera;
    private ShipConfig _shipConfig;
    private CameraConfig _cameraConfig;
    private PlayerConfig _playerConfig;
    private HudConfig _hudConfig;
    private ParticleConfig _particleConfig;
    private HitFlashConfig _hitFlashConfig;
    private ScreenShakeConfig _screenShakeConfig;
    private HudFeedbackConfig _hudFeedbackConfig;
    private OxygenWarningConfig _oxygenWarningConfig;
    private SuffocationEffectConfig _suffocationConfig;
    private DeathSequenceConfig _deathSequenceConfig;
    private float _deathElapsedSeconds;
    private Texture2D _hudBarFillTexture;
    private Texture2D _hudBarOutlineTexture;
    private Texture2D _sparkTexture;
    private RenderTarget2D _sceneRenderTarget;
    private Effect _suffocationEffect;
    private readonly Random _random = new();
    private System.Numerics.Vector2 _shipSpawnPositionMeters;
    private KeyboardState _previousKeyboardState;
    private Point _previousMousePosition;
    private bool _useController;
    private bool _isFirstUpdate = true;
    private bool _hasReceivedInput;

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

        var shipConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "ship-config.json");
        _shipConfig = ShipConfig.Load(shipConfigPath);

        var cameraConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "camera-config.json");
        _cameraConfig = CameraConfig.Load(cameraConfigPath);

        var worldConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "world-config.json");
        var worldConfig = WorldConfig.Load(worldConfigPath);

        var starfieldConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "starfield-config.json");
        var starfieldConfig = StarfieldConfig.Load(starfieldConfigPath);

        var playerConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "player-config.json");
        _playerConfig = PlayerConfig.Load(playerConfigPath);

        var hudConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "hud-config.json");
        _hudConfig = HudConfig.Load(hudConfigPath);
        var barCornerRadius = _hudConfig.BarThicknessPixels / 2f;
        _hudBarFillTexture = ProceduralTextures.CreateRoundedRect(GraphicsDevice, _hudConfig.BarLengthPixels, _hudConfig.BarThicknessPixels, barCornerRadius, Microsoft.Xna.Framework.Color.White);
        _hudBarOutlineTexture = ProceduralTextures.CreateRoundedRectOutline(GraphicsDevice, _hudConfig.BarLengthPixels, _hudConfig.BarThicknessPixels, barCornerRadius, _hudConfig.BarOutlineThicknessPixels, Microsoft.Xna.Framework.Color.White);
        var particleConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "particle-config.json");
        _particleConfig = ParticleConfig.Load(particleConfigPath);
        _sparkTexture = ProceduralTextures.CreateCircle(GraphicsDevice, _particleConfig.SparkTextureSizePixels, Microsoft.Xna.Framework.Color.White);

        var hitFlashConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "hit-flash-config.json");
        _hitFlashConfig = HitFlashConfig.Load(hitFlashConfigPath);

        var screenShakeConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "screen-shake-config.json");
        _screenShakeConfig = ScreenShakeConfig.Load(screenShakeConfigPath);

        var hudFeedbackConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "hud-feedback-config.json");
        _hudFeedbackConfig = HudFeedbackConfig.Load(hudFeedbackConfigPath);

        var oxygenWarningConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "oxygen-warning-config.json");
        _oxygenWarningConfig = OxygenWarningConfig.Load(oxygenWarningConfigPath);

        var suffocationConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "suffocation-effect-config.json");
        _suffocationConfig = SuffocationEffectConfig.Load(suffocationConfigPath);
        _suffocationEffect = Content.Load<Effect>("Shaders/SuffocationEffect");
        _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, WindowWidth, WindowHeight);

        var deathSequenceConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "death-sequence-config.json");
        _deathSequenceConfig = DeathSequenceConfig.Load(deathSequenceConfigPath);

        _shipSpawnPositionMeters = PhysicsWorld.PixelsToMeters(new System.Numerics.Vector2(WindowWidth / 2f, WindowHeight / 2f));
        _camera.PositionMeters = _shipSpawnPositionMeters;
        _camera.TargetPositionMeters = _shipSpawnPositionMeters;
        ShipEntity.Create(_world, _physicsWorld, GraphicsDevice, _shipSpawnPositionMeters, _shipConfig, _playerConfig);

        foreach (var layer in starfieldConfig.Layers)
        {
            var color = Microsoft.Xna.Framework.Color.White * layer.Brightness;
            Starfield.Create(_world, GraphicsDevice, _shipSpawnPositionMeters, layer.HalfExtentMeters, layer.StarCount, layer.Parallax, color,
                starfieldConfig.MinTintStrength, starfieldConfig.MaxTintStrength);
        }

        AsteroidField.Create(_world, _physicsWorld, GraphicsDevice, _shipSpawnPositionMeters, worldConfig);

        const int buttonWidth = 220;
        const int buttonHeight = 60;
        const float buttonCornerRadius = 14f;
        _buttonFillTexture = ProceduralTextures.CreateRoundedRect(GraphicsDevice, buttonWidth, buttonHeight, buttonCornerRadius, Microsoft.Xna.Framework.Color.White);
        _buttonOutlineTexture = ProceduralTextures.CreateRoundedRectOutline(GraphicsDevice, buttonWidth, buttonHeight, buttonCornerRadius, 2f, Microsoft.Xna.Framework.Color.White);
        _solidPixelTexture = ProceduralTextures.CreateSolidSquare(GraphicsDevice, 1, Microsoft.Xna.Framework.Color.White);

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
                    _camera.PositionMeters = _shipSpawnPositionMeters;
                    _camera.TargetPositionMeters = _shipSpawnPositionMeters;
                }

                _hasReceivedInput = true; // clicking/confirming counts as the real input that unlocks the cursor for Playing
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

            _deathElapsedSeconds += dyingDeltaSeconds;
            if (_deathElapsedSeconds >= _deathSequenceConfig.ExplosionDurationSeconds + _deathSequenceConfig.FadeDurationSeconds)
                _gameState = GameState.GameOver;

            _previousMenuMouseState = mouse;
            _previousKeyboardState = keyboard;
            base.Update(gameTime);
            return;
        }

        if (keyboard.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R))
            ShipEntity.Respawn(_world, _shipSpawnPositionMeters);

        var mousePosition = mouse.Position;

        // The OS can place the cursor anywhere at launch, and _previousMousePosition starts
        // at (0,0) — without this, frame one would almost always read as "the mouse moved",
        // spuriously flipping into mouse mode before the player has touched anything.
        if (_isFirstUpdate)
        {
            _previousMousePosition = mousePosition;
            _isFirstUpdate = false;
        }

        // Don't lock the cursor or react to the mouse at all until the window has actually
        // been focused and used at least once — otherwise we'd start locking/steering the
        // camera from wherever the OS happens to place the cursor before the player's done
        // anything, which reads as a spurious jump/lock right at startup.
        if (!_hasReceivedInput && IsActive &&
            (IsControllerInputActive(gamePad) || IsKeyboardMouseInputActive(keyboard, mouse, mousePosition, _previousMousePosition)))
        {
            _hasReceivedInput = true;
        }

        // True OS-level cursor confinement (Win32 ClipCursor) rather than a software clamp —
        // clamping after the fact still lets a fast mouse movement's raw position genuinely
        // leave the window for a frame, which can defocus the game or click into whatever's
        // behind it. Only while focused; release the clip when not, so alt-tabbing away
        // doesn't leave the OS cursor stuck to a window that no longer has focus.
        if (_hasReceivedInput && IsActive)
        {
            IsMouseVisible = false;
            WindowsCursorLock.Lock(Window.ClientBounds);
        }
        else
        {
            IsMouseVisible = true;
            WindowsCursorLock.Release();
        }

        // Keyboard/mouse and controller are mutually exclusive: whichever one
        // produced input this frame becomes (or stays) active, and the other is
        // ignored entirely until it's the one being used.
        if (IsControllerInputActive(gamePad))
            _useController = true;
        else if (IsKeyboardMouseInputActive(keyboard, mouse, mousePosition, _previousMousePosition))
            _useController = false;

        // The cursor's direction from the ship's on-screen position — used both as a mouse
        // facing override (while LMB is held, mirroring the right stick) and for the camera
        // look-ahead below. Uses last frame's synced Transform (one frame stale, imperceptible).
        // Only while focused — unfocused input shouldn't affect facing/camera at all.
        System.Numerics.Vector2? cursorDirectionFromShip = null;
        if (_hasReceivedInput && IsActive && !_useController && CameraFollowSystem.TryGetShipPositionMeters(_world, out var shipPositionForAim))
        {
            var shipScreenPixels = _camera.WorldToScreen(shipPositionForAim).ToNumerics();
            var cursorScreenPixels = new System.Numerics.Vector2(mousePosition.X, mousePosition.Y);
            cursorDirectionFromShip = cursorScreenPixels - shipScreenPixels;
        }

        var mouseFacingDirection = mouse.LeftButton == ButtonState.Pressed ? cursorDirectionFromShip : null;

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

        ShipInputSystem.Run(_world, keyboard, gamePad, _useController, mouseFacingDirection, deltaSeconds);
        _physicsWorld.Step(deltaSeconds);
        CollisionDamageSystem.Run(_world, _physicsWorld, _playerConfig, _sparkTexture, _random, _particleConfig, _camera, _screenShakeConfig, _hitFlashConfig, _hudFeedbackConfig); // must read hit events before the next Step overwrites them

        var shipHealth = float.MaxValue;
        _world.Query(in HealthQuery, (ref Health health) => shipHealth = health.Current);
        if (shipHealth <= 0f && CameraFollowSystem.TryGetShipPositionMeters(_world, out var deathPositionMeters))
        {
            for (var i = 0; i < _deathSequenceConfig.ExplosionBurstCount; i++)
                ParticleEffects.SpawnSparkBurst(_world, _sparkTexture, deathPositionMeters, _random, _particleConfig);

            _gameState = GameState.Dying;
            _deathElapsedSeconds = 0f;
        }

        VitalsSystem.Run(_world, deltaSeconds, _playerConfig, _suffocationConfig);

        // Suffocation kills once its post-process effect has fully played out. No explosion
        // and no extra fade here — the screen's already fully black from the vignette by
        // this point, so jump straight to GameOver instead of the collision-death sequence.
        if (_gameState == GameState.Playing)
        {
            var suffocationElapsedSeconds = 0f;
            _world.Query(in SuffocationQuery, (ref Suffocation suffocation) => suffocationElapsedSeconds = suffocation.ElapsedSeconds);
            if (suffocationElapsedSeconds >= _suffocationConfig.EffectDurationSeconds)
            {
                _world.Query(in HealthQuery, (ref Health health) => health.Current = 0f);
                _gameState = GameState.GameOver;
            }
        }

        ParticleSystem.Run(_world, deltaSeconds);
        HitFlashSystem.Run(_world, deltaSeconds, _hitFlashConfig);
        HudFeedbackSystem.Run(_world, deltaSeconds, _hudFeedbackConfig, _random);
        SpeedCapSystem.Run(_world);
        PhysicsSyncSystem.Run(_world);

        // Camera casts out toward wherever the aim input points, not the ship's facing
        // (which lags behind at a capped turn rate): the right stick's own direction in
        // controller mode; in mouse mode, a point MouseFocusRatio of the way from the
        // ship's on-screen position to the cursor's.
        System.Numerics.Vector2 lookAheadOffsetMeters;
        if (_useController)
        {
            var rightStick = new System.Numerics.Vector2(gamePad.ThumbSticks.Right.X, -gamePad.ThumbSticks.Right.Y);
            if (rightStick.LengthSquared() > 1f) rightStick = System.Numerics.Vector2.Normalize(rightStick);
            lookAheadOffsetMeters = rightStick * _cameraConfig.MaxDistanceMeters;
        }
        else if (cursorDirectionFromShip.HasValue)
        {
            lookAheadOffsetMeters = PhysicsWorld.PixelsToMeters(cursorDirectionFromShip.Value * _cameraConfig.MouseFocusRatio);
        }
        else
        {
            lookAheadOffsetMeters = System.Numerics.Vector2.Zero;
        }

        // Tweening only applies in controller mode; mouse aiming snaps straight to target
        // since the mouse's own movement is already the direct input, and easing on top of
        // that felt disconnected from the cursor.
        var cameraSmoothingSpeed = _useController ? _cameraConfig.TweenSpeed : 0f;
        CameraFollowSystem.Run(_world, _camera, lookAheadOffsetMeters, deltaSeconds, cameraSmoothingSpeed);
        _camera.UpdateShake(deltaSeconds, _screenShakeConfig.ShakeDecaySpeed);

        _previousKeyboardState = keyboard;
        _previousMousePosition = mousePosition;
        _previousMenuMouseState = mouse;
        base.Update(gameTime);
    }

    private static readonly QueryDescription HealthQuery = new QueryDescription().WithAll<Health, PlayerControlled>();

    private static bool IsKeyboardMouseInputActive(KeyboardState keyboard, MouseState mouse, Point mousePosition, Point previousMousePosition)
    {
        return keyboard.GetPressedKeys().Length > 0
               || mousePosition != previousMousePosition
               || mouse.LeftButton == ButtonState.Pressed
               || mouse.RightButton == ButtonState.Pressed
               || mouse.MiddleButton == ButtonState.Pressed;
    }

    private bool IsControllerInputActive(GamePadState gamePad)
    {
        return gamePad.ThumbSticks.Left != Microsoft.Xna.Framework.Vector2.Zero
               || gamePad.ThumbSticks.Right != Microsoft.Xna.Framework.Vector2.Zero
               || gamePad.Triggers.Left > 0.1f
               || gamePad.Triggers.Right > 0.1f
               || gamePad.Buttons.A == ButtonState.Pressed
               || gamePad.Buttons.B == ButtonState.Pressed
               || gamePad.Buttons.X == ButtonState.Pressed
               || gamePad.Buttons.Y == ButtonState.Pressed
               || gamePad.Buttons.LeftShoulder == ButtonState.Pressed
               || gamePad.Buttons.RightShoulder == ButtonState.Pressed
               || gamePad.Buttons.Start == ButtonState.Pressed
               || gamePad.DPad.Up == ButtonState.Pressed
               || gamePad.DPad.Down == ButtonState.Pressed
               || gamePad.DPad.Left == ButtonState.Pressed
               || gamePad.DPad.Right == ButtonState.Pressed;
    }

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
        _spriteBatch.End();

        // Separate screen-space pass (no camera transform) for HUD/debug text.
        _spriteBatch.Begin();
        HudRenderer.Run(_world, _spriteBatch, WindowHeight, _hudConfig, _hudFeedbackConfig, _oxygenWarningConfig,
            (float)gameTime.TotalGameTime.TotalSeconds, _hudBarFillTexture, _hudBarOutlineTexture);
#if DEBUG
        _spriteBatch.DrawString(_uiFont, $"FPS: {_fps}", new Microsoft.Xna.Framework.Vector2(10, 10), Color.White);
#endif
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        var suffocationSeconds = 0f;
        _world.Query(in SuffocationQuery, (ref Suffocation suffocation) => suffocationSeconds = suffocation.ElapsedSeconds);
        var suffocationProgress = MathHelper.Clamp(suffocationSeconds / _suffocationConfig.EffectDurationSeconds, 0f, 1f);

        var pixelBlockSizePixels = _suffocationConfig.PixelationEnabled ? _suffocationConfig.MaxPixelationBlockSizePixels * suffocationProgress : 0f;
        _suffocationEffect.Parameters["PixelBlockSizeUV"].SetValue(new Vector2(pixelBlockSizePixels / WindowWidth, pixelBlockSizePixels / WindowHeight));
        var grayscaleIntensity = MathF.Pow(suffocationProgress, _suffocationConfig.GrayscaleEaseExponent);
        _suffocationEffect.Parameters["GrayscaleIntensity"].SetValue(grayscaleIntensity);
        var vignetteProgress = MathF.Pow(suffocationProgress, _suffocationConfig.VignetteEaseExponent);
        _suffocationEffect.Parameters["VignetteRadius"].SetValue(MathHelper.Lerp(_suffocationConfig.VignetteStartRadius, 0f, vignetteProgress));
        _suffocationEffect.Parameters["VignetteFeatherRadius"].SetValue(_suffocationConfig.VignetteFeatherRadius);
        _suffocationEffect.Parameters["AspectRatio"].SetValue(new Vector2(WindowWidth / (float)WindowHeight, 1f));
        _suffocationEffect.Parameters["NoiseCellCount"].SetValue(new Vector2(WindowWidth / _suffocationConfig.NoiseGrainSizePixels, WindowHeight / _suffocationConfig.NoiseGrainSizePixels));
        _suffocationEffect.Parameters["NoiseIntensity"].SetValue(_suffocationConfig.NoiseMaxIntensity * suffocationProgress);
        _suffocationEffect.Parameters["NoiseAdditiveBlend"].SetValue(_suffocationConfig.NoiseAdditiveBlend ? 1f : 0f);
        _suffocationEffect.Parameters["NoiseTimeSeed"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);

        _spriteBatch.Begin(effect: _suffocationEffect, samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_sceneRenderTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();

        // Fades to fully opaque black across Dying's ExplosionDurationSeconds..+FadeDurationSeconds
        // window, then stays there through GameOver (elapsed is never advanced again once
        // GameOver is reached, so this keeps evaluating to 1).
        var deathFadeAlpha = 0f;
        if (_gameState == GameState.Dying || _gameState == GameState.GameOver)
        {
            var fadeElapsed = _deathElapsedSeconds - _deathSequenceConfig.ExplosionDurationSeconds;
            deathFadeAlpha = _gameState == GameState.GameOver ? 1f : MathHelper.Clamp(fadeElapsed / _deathSequenceConfig.FadeDurationSeconds, 0f, 1f);
        }

        if (deathFadeAlpha > 0f)
        {
            _spriteBatch.Begin();
            _spriteBatch.Draw(_solidPixelTexture, new Rectangle(0, 0, WindowWidth, WindowHeight), Color.Black * deathFadeAlpha);
            _spriteBatch.End();
        }

        if (_gameState == GameState.StartScreen || _gameState == GameState.GameOver)
        {
            var button = _gameState == GameState.StartScreen ? _startButton : _restartButton;
            var title = _gameState == GameState.StartScreen ? "STATION" : "YOU DIED";
            var isHovered = button.IsHovered(Mouse.GetState().Position);

            _spriteBatch.Begin();
            MenuRenderer.Draw(_spriteBatch, _uiFont, _solidPixelTexture, _buttonFillTexture, _buttonOutlineTexture,
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
        _hudBarFillTexture.Dispose();
        _hudBarOutlineTexture.Dispose();
        _sparkTexture.Dispose();
        _buttonFillTexture.Dispose();
        _buttonOutlineTexture.Dispose();
        _solidPixelTexture.Dispose();
        _sceneRenderTarget.Dispose();
        World.Destroy(_world);
        _physicsWorld.Dispose();
        base.UnloadContent();
    }
}
