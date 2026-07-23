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
#if DEBUG
    private SpriteFont _debugFont;
    private float _fpsTimerSeconds;
    private int _fpsFrameCount;
    private int _fps;
#endif
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
    private Texture2D _hudBarFillTexture;
    private Texture2D _hudBarOutlineTexture;
    private Texture2D _sparkTexture;
    private readonly Random _random = new();
    private System.Numerics.Vector2 _shipSpawnPositionMeters;
    private KeyboardState _previousKeyboardState;
    private Point _previousMousePosition;
    private bool _useController;

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
#if DEBUG
        _debugFont = Content.Load<SpriteFont>("Fonts/DebugFont");
#endif

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
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular);
        if (gamePad.Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        if (keyboard.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R))
            ShipEntity.Respawn(_world, _shipSpawnPositionMeters);

        var mouse = Mouse.GetState();
        var mousePosition = mouse.Position;

        // True OS-level cursor confinement (Win32 ClipCursor) rather than a software clamp —
        // clamping after the fact still lets a fast mouse movement's raw position genuinely
        // leave the window for a frame, which can defocus the game or click into whatever's
        // behind it. Only while focused; release the clip when not, so alt-tabbing away
        // doesn't leave the OS cursor stuck to a window that no longer has focus.
        if (IsActive)
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
        if (IsActive && !_useController && CameraFollowSystem.TryGetShipPositionMeters(_world, out var shipPositionForAim))
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
        CollisionDamageSystem.Run(_world, _physicsWorld, _playerConfig, _sparkTexture, _random, _particleConfig, _camera, _screenShakeConfig, _hitFlashConfig); // must read hit events before the next Step overwrites them
        VitalsSystem.Run(_world, deltaSeconds, _playerConfig);
        ParticleSystem.Run(_world, deltaSeconds);
        HitFlashSystem.Run(_world, deltaSeconds, _hitFlashConfig);
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
        base.Update(gameTime);
    }

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

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // BackToFront so LayerDepth actually controls draw order (stars behind everything);
        // PointClamp instead of the default linear filter so scaled sprites (asteroids) get
        // crisp edges instead of blurring when magnified/minified.
        _spriteBatch.Begin(SpriteSortMode.BackToFront, samplerState: SamplerState.PointClamp);
        RenderSystem.Run(_world, _spriteBatch, _camera);
        _spriteBatch.End();

        // Separate screen-space pass (no camera transform) for HUD/debug text.
        _spriteBatch.Begin();
        HudRenderer.Run(_world, _spriteBatch, WindowHeight, _hudConfig, _hudBarFillTexture, _hudBarOutlineTexture);
#if DEBUG
        _spriteBatch.DrawString(_debugFont, $"FPS: {_fps}", new Microsoft.Xna.Framework.Vector2(10, 10), Color.White);
#endif
        _spriteBatch.End();

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
        World.Destroy(_world);
        _physicsWorld.Dispose();
        base.UnloadContent();
    }
}
