using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.ECS;
using SpaceSurvivalGame.ECS.Systems;
using SpaceSurvivalGame.Input;
using SpaceSurvivalGame.Platform;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame;

/// <summary>
/// Handles StartScreen and GameOver — both a "show a button, wait for confirm" menu,
/// differing only in whether confirming should also reset the world back to a fresh run
/// (the GameOver variant). Adding a future menu-like state (e.g. a pause screen) is just
/// another instance of this handler with different constructor args.
/// </summary>
public class MenuStateHandler : IGameStateHandler
{
    private readonly World _world;
    private readonly Camera _camera;
    private readonly System.Numerics.Vector2 _shipSpawnPositionMeters;
    private readonly UiButton _button;
    private readonly bool _isGameOverVariant;
    private readonly InputModeTracker _inputMode;
    private readonly IGameHost _host;

    public MenuStateHandler(World world, Camera camera, System.Numerics.Vector2 shipSpawnPositionMeters, UiButton button,
        bool isGameOverVariant, InputModeTracker inputMode, IGameHost host)
    {
        _world = world;
        _camera = camera;
        _shipSpawnPositionMeters = shipSpawnPositionMeters;
        _button = button;
        _isGameOverVariant = isGameOverVariant;
        _inputMode = inputMode;
        _host = host;
    }

    public GameState? Update(GameTime gameTime, KeyboardState keyboard, GamePadState gamePad, MouseState mouse,
        KeyboardState previousKeyboard, MouseState previousMenuMouse)
    {
        // Menus always show a free, visible cursor — cursor lock/hide is a Playing-only concern.
        _host.IsMouseVisible = true;
        WindowsCursorLock.Release();

        var clickedButton = mouse.LeftButton == ButtonState.Pressed && previousMenuMouse.LeftButton == ButtonState.Released
                             && _button.IsHovered(mouse.Position);
        var confirmedViaKeyboardOrPad = (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter))
                                        || (keyboard.IsKeyDown(Keys.Space) && !previousKeyboard.IsKeyDown(Keys.Space))
                                        || gamePad.Buttons.Start == ButtonState.Pressed
                                        || gamePad.Buttons.A == ButtonState.Pressed;

        if (!clickedButton && !confirmedViaKeyboardOrPad) return null;

        if (_isGameOverVariant)
        {
            ShipEntity.Respawn(_world, _shipSpawnPositionMeters);
            StationCoreEntity.Show(_world); // undoes the Hide from death, if it was still Attached
            ParticleSystem.Clear(_world); // no leftover explosion sparks/ship fragments carrying over from the previous life
            _camera.PositionMeters = _shipSpawnPositionMeters;
            _camera.TargetPositionMeters = _shipSpawnPositionMeters;
        }

        _inputMode.NotifyInputReceived(); // clicking/confirming counts as the real input that unlocks the cursor for Playing
        return GameState.Playing;
    }
}
