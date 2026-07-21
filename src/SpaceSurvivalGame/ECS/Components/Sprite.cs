using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSurvivalGame.ECS.Components;

public struct Sprite
{
    public Texture2D Texture;
    public Color Color;
    public int Size; // width/height in pixels; used to center the draw origin
}
