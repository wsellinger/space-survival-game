using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSurvivalGame.ECS.Components;

public struct Sprite
{
    public Texture2D Texture;
    public Color Color;
    public int Size; // width/height in pixels; used to center the draw origin
    public float Scale; // must be set explicitly at construction — default(float) is 0, not 1
    public float LayerDepth; // 0 = front, 1 = back (SpriteBatch convention); default(float) 0 is correct for everything except background layers
    public float Parallax; // scales how much camera movement affects this entity's screen position; must be set explicitly — default(float) is 0, not 1. 1 = normal, <1 = appears farther away (distant background layers), >1 = appears closer than the camera plane
}
