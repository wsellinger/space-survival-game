using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SpaceSurvivalGame;

/// <summary>
/// One GameState's per-frame Update logic. MainGame dispatches to whichever handler is
/// registered for the current state via a Dictionary&lt;GameState, IGameStateHandler&gt; —
/// adding a new state is registering a new handler, not editing existing ones.
/// </summary>
public interface IGameStateHandler
{
    /// <summary>Returns the state to transition to, or null to remain in the current state.</summary>
    GameState? Update(GameTime gameTime, KeyboardState keyboard, GamePadState gamePad, MouseState mouse,
        KeyboardState previousKeyboard, MouseState previousMenuMouse);
}
