using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.Rendering;

/// <summary>A simple screen-space clickable rectangle with a label; MainGame owns the actual click/hover detection against Bounds.</summary>
public readonly struct UiButton
{
    public Rectangle Bounds { get; }
    public string Label { get; }

    public UiButton(Rectangle bounds, string label)
    {
        Bounds = bounds;
        Label = label;
    }

    public bool IsHovered(Point mousePosition) => Bounds.Contains(mousePosition);
}
