using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.Platform;

namespace SpaceSurvivalGame.Input;

/// <summary>
/// Tracks which input device is currently driving the ship (controller vs mouse/keyboard,
/// mutually exclusive) and the associated OS-level cursor lock/hide state. Doesn't know
/// anything about the ship, camera, or world — MainGame's Playing-state handler computes
/// mouse-facing-direction/camera-look-ahead itself using UseController/HasReceivedInput.
/// </summary>
public class InputModeTracker
{
    public bool UseController { get; private set; }
    public bool HasReceivedInput { get; private set; }

    private Point _previousMousePosition;
    private bool _isFirstUpdate = true;

    /// <summary>Marks input as received without requiring a real device poll — used when a menu click/confirm is itself the qualifying first input.</summary>
    public void NotifyInputReceived() => HasReceivedInput = true;

    /// <summary>
    /// Updates device-mode + cursor state for one frame. Returns the value the caller should
    /// assign to Game.IsMouseVisible; also applies/releases the Win32 cursor clip as a side effect.
    /// </summary>
    public bool Update(KeyboardState keyboard, MouseState mouse, GamePadState gamePad, bool isGameActive, Rectangle clientBoundsScreenSpace)
    {
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
        if (!HasReceivedInput && isGameActive &&
            (IsControllerInputActive(gamePad) || IsKeyboardMouseInputActive(keyboard, mouse, mousePosition, _previousMousePosition)))
        {
            HasReceivedInput = true;
        }

        // True OS-level cursor confinement (Win32 ClipCursor) rather than a software clamp —
        // clamping after the fact still lets a fast mouse movement's raw position genuinely
        // leave the window for a frame, which can defocus the game or click into whatever's
        // behind it. Only while focused; release the clip when not, so alt-tabbing away
        // doesn't leave the OS cursor stuck to a window that no longer has focus.
        bool isMouseVisible;
        if (HasReceivedInput && isGameActive)
        {
            isMouseVisible = false;
            WindowsCursorLock.Lock(clientBoundsScreenSpace);
        }
        else
        {
            isMouseVisible = true;
            WindowsCursorLock.Release();
        }

        // Keyboard/mouse and controller are mutually exclusive: whichever one
        // produced input this frame becomes (or stays) active, and the other is
        // ignored entirely until it's the one being used.
        if (IsControllerInputActive(gamePad))
            UseController = true;
        else if (IsKeyboardMouseInputActive(keyboard, mouse, mousePosition, _previousMousePosition))
            UseController = false;

        _previousMousePosition = mousePosition;
        return isMouseVisible;
    }

    private static bool IsKeyboardMouseInputActive(KeyboardState keyboard, MouseState mouse, Point mousePosition, Point previousMousePosition)
    {
        return keyboard.GetPressedKeys().Length > 0
               || mousePosition != previousMousePosition
               || mouse.LeftButton == ButtonState.Pressed
               || mouse.RightButton == ButtonState.Pressed
               || mouse.MiddleButton == ButtonState.Pressed;
    }

    private static bool IsControllerInputActive(GamePadState gamePad)
    {
        return gamePad.ThumbSticks.Left != Vector2.Zero
               || gamePad.ThumbSticks.Right != Vector2.Zero
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
}
