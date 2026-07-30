using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame;

/// <summary>
/// Narrow seam exposing just the Game-level host state that state handlers need
/// (cursor visibility, focus, window bounds) without giving them a direct MainGame/Game
/// back-reference.
/// </summary>
public interface IGameHost
{
    bool IsMouseVisible { get; set; }
    bool IsActive { get; }
    Rectangle ClientBounds { get; }
}
