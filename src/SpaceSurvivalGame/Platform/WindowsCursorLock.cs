using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.Platform;

/// <summary>
/// True OS-level cursor confinement via Win32's ClipCursor. A software clamp
/// (move the cursor back after it's already left) still lets fast mouse
/// movement escape the window for a frame, which can defocus the game or
/// click into whatever's behind it — ClipCursor stops the OS cursor from
/// leaving the given rectangle at all.
///
/// Windows-only, deliberately: mouse/keyboard controls in this game only ever
/// run on Windows desktop (a console or mobile port wouldn't use them at all),
/// so there's no cross-platform abstraction here. Linux/macOS would need their
/// own platform-specific cursor-lock implementation if that's ever needed.
/// </summary>
public static class WindowsCursorLock
{
    [DllImport("user32.dll")]
    private static extern bool ClipCursor(ref RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(IntPtr rectPtr);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>Confines the OS cursor to clientBoundsScreenSpace (e.g. GameWindow.ClientBounds) until Release() is called.</summary>
    public static void Lock(Rectangle clientBoundsScreenSpace)
    {
        var rect = new RECT
        {
            Left = clientBoundsScreenSpace.Left,
            Top = clientBoundsScreenSpace.Top,
            Right = clientBoundsScreenSpace.Right,
            Bottom = clientBoundsScreenSpace.Bottom
        };
        ClipCursor(ref rect);
    }

    /// <summary>Releases any active clip, letting the cursor move freely across the whole desktop again.</summary>
    public static void Release() => ClipCursor(IntPtr.Zero);
}
