using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LastLight.Client.Core;

public class Camera
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; } = 1.0f;
    public float Rotation { get; set; } = 0.0f;
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    public Camera(Viewport viewport)
    {
        ViewportWidth = viewport.Width;
        ViewportHeight = viewport.Height;
    }

    public Matrix GetTransformationMatrix()
    {
        return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
               Matrix.CreateRotationZ(Rotation) *
               Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
               Matrix.CreateTranslation(new Vector3(ViewportWidth * 0.5f, ViewportHeight * 0.5f, 0));
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Matrix.Invert(GetTransformationMatrix()));
    }
}
