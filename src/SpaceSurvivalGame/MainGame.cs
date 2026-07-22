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

namespace SpaceSurvivalGame;

public class MainGame : Game
{
    private const int WindowWidth = 1920;
    private const int WindowHeight = 1080;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private PhysicsWorld _physicsWorld;
    private World _world;
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

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var configPath = Path.Combine(AppContext.BaseDirectory, "ship-config.json");
        var shipConfig = ShipConfig.Load(configPath);

        _shipSpawnPositionMeters = PhysicsWorld.PixelsToMeters(new System.Numerics.Vector2(WindowWidth / 2f, WindowHeight / 2f));
        ShipEntity.Create(_world, _physicsWorld, GraphicsDevice, _shipSpawnPositionMeters, shipConfig);
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

        // Keyboard/mouse and controller are mutually exclusive: whichever one
        // produced input this frame becomes (or stays) active, and the other is
        // ignored entirely until it's the one being used.
        if (IsControllerInputActive(gamePad))
            _useController = true;
        else if (IsKeyboardMouseInputActive(keyboard, mouse, _previousMousePosition))
            _useController = false;

        IsMouseVisible = !_useController;

        var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        ShipInputSystem.Run(_world, keyboard, gamePad, _useController, deltaSeconds);
        _physicsWorld.Step(deltaSeconds);
        SpeedCapSystem.Run(_world);
        PhysicsSyncSystem.Run(_world);

        _previousKeyboardState = keyboard;
        _previousMousePosition = mouse.Position;
        base.Update(gameTime);
    }

    private static bool IsKeyboardMouseInputActive(KeyboardState keyboard, MouseState mouse, Point previousMousePosition)
    {
        return keyboard.GetPressedKeys().Length > 0
               || mouse.Position != previousMousePosition
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
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        RenderSystem.Run(_world, _spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private static readonly QueryDescription SpriteQuery = new QueryDescription().WithAll<Sprite>();

    protected override void UnloadContent()
    {
        _world.Query(in SpriteQuery, (ref Sprite sprite) => sprite.Texture.Dispose());
        World.Destroy(_world);
        _physicsWorld.Dispose();
        base.UnloadContent();
    }
}
