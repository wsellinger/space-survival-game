using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS;
using SpaceSurvivalGame.ECS.Systems;
using SpaceSurvivalGame.Input;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame;

/// <summary>
/// Handles the Playing state: the manual-respawn dev hotkey, input-mode tracking (via
/// InputModeTracker), mouse-facing-direction computation, and the fixed-order gameplay
/// system dispatch (GameplaySystemsPipeline). Reports collision/suffocation death back as
/// a state transition; doesn't otherwise know about the outer state machine.
/// </summary>
public class PlayingStateHandler : IGameStateHandler
{
    private readonly World _world;
    private readonly PhysicsWorld _physicsWorld;
    private readonly Camera _camera;
    private readonly GameConfigs _configs;
    private readonly GameAssets _assets;
    private readonly OxygenPickupField.PickupAssets _pickupAssets;
    private readonly IronPickupField.PickupAssets _ironAssets;
    private readonly Random _random;
    private readonly InputModeTracker _inputMode;
    private readonly DeathTimer _deathTimer;
    private readonly System.Numerics.Vector2 _shipSpawnPositionMeters;
    private readonly IGameHost _host;

    public PlayingStateHandler(World world, PhysicsWorld physicsWorld, Camera camera, GameConfigs configs, GameAssets assets,
        OxygenPickupField.PickupAssets pickupAssets, IronPickupField.PickupAssets ironAssets, Random random,
        InputModeTracker inputMode, DeathTimer deathTimer, System.Numerics.Vector2 shipSpawnPositionMeters, IGameHost host)
    {
        _world = world;
        _physicsWorld = physicsWorld;
        _camera = camera;
        _configs = configs;
        _assets = assets;
        _pickupAssets = pickupAssets;
        _ironAssets = ironAssets;
        _random = random;
        _inputMode = inputMode;
        _deathTimer = deathTimer;
        _shipSpawnPositionMeters = shipSpawnPositionMeters;
        _host = host;
    }

    public GameState? Update(GameTime gameTime, KeyboardState keyboard, GamePadState gamePad, MouseState mouse,
        KeyboardState previousKeyboard, MouseState previousMenuMouse)
    {
        if (keyboard.IsKeyDown(Keys.R) && !previousKeyboard.IsKeyDown(Keys.R))
        {
            ShipEntity.Respawn(_world, _shipSpawnPositionMeters);
            StationCoreEntity.Show(_world);
            ParticleSystem.Clear(_world);
        }

        var mousePosition = mouse.Position;
        _host.IsMouseVisible = _inputMode.Update(keyboard, mouse, gamePad, _host.IsActive, _host.ClientBounds);

        // The cursor's direction from the ship's on-screen position — used both as a mouse
        // facing override (while RMB is held, mirroring the right stick) and for the camera
        // look-ahead below. Uses last frame's synced Transform (one frame stale, imperceptible).
        // Only while focused — unfocused input shouldn't affect facing/camera at all.
        System.Numerics.Vector2? cursorDirectionFromShip = null;
        if (_inputMode.HasReceivedInput && _host.IsActive && !_inputMode.UseController && CameraFollowSystem.TryGetShipPositionMeters(_world, out var shipPositionForAim))
        {
            var shipScreenPixels = _camera.WorldToScreen(shipPositionForAim).ToNumerics();
            var cursorScreenPixels = new System.Numerics.Vector2(mousePosition.X, mousePosition.Y);
            cursorDirectionFromShip = cursorScreenPixels - shipScreenPixels;
        }

        var mouseFacingDirection = mouse.RightButton == ButtonState.Pressed ? cursorDirectionFromShip : null;
        var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        GameplaySystemsPipeline.Run(_world, _physicsWorld, _camera, _configs, _assets, _pickupAssets, _ironAssets, _random,
            keyboard, gamePad, _inputMode.UseController, mouseFacingDirection, deltaSeconds, out var shipDied, out var suffocated);

        if (shipDied)
        {
            _deathTimer.ElapsedSeconds = 0f;
            return GameState.Dying;
        }

        return suffocated ? GameState.GameOver : null;
    }
}
