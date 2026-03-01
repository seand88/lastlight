using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace LastLight.Client.Core;

public class VirtualJoystick
{
    public Vector2 Position { get; set; }
    public float Radius { get; set; }
    public float KnobRadius { get; set; }
    public Vector2 Value { get; private set; } // Normalized direction vector
    public bool IsActive { get; private set; }
    public int PointerId { get; private set; } = -1;

    public VirtualJoystick(Vector2 position, float radius)
    {
        Position = position;
        Radius = radius;
        KnobRadius = radius * 0.4f;
    }

    public void Update(TouchCollection touches)
    {
        bool foundPointer = false;

        foreach (var touch in touches)
        {
            if (touch.State == TouchLocationState.Pressed)
            {
                if (!IsActive && Vector2.Distance(touch.Position, Position) <= Radius * 1.5f) // Slightly larger hit area
                {
                    IsActive = true;
                    PointerId = touch.Id;
                    foundPointer = true;
                    UpdateValue(touch.Position);
                    break;
                }
            }
            else if ((touch.State == TouchLocationState.Moved || touch.State == TouchLocationState.Pressed) && touch.Id == PointerId)
            {
                foundPointer = true;
                UpdateValue(touch.Position);
                break;
            }
            else if ((touch.State == TouchLocationState.Released || touch.State == TouchLocationState.Invalid) && touch.Id == PointerId)
            {
                IsActive = false;
                PointerId = -1;
                Value = Vector2.Zero;
            }
        }

        if (IsActive && !foundPointer && PointerId != -1)
        {
            // Failsafe in case touch is lost
            IsActive = false;
            PointerId = -1;
            Value = Vector2.Zero;
        }
    }

    private void UpdateValue(Vector2 touchPosition)
    {
        Vector2 diff = touchPosition - Position;
        float length = diff.Length();

        if (length > Radius)
        {
            Value = diff / length; // Normalized
        }
        else
        {
            Value = diff / Radius; // Scaled by distance
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, float alpha = 0.5f)
    {
        // Base
        spriteBatch.Draw(pixel, new Rectangle((int)(Position.X - Radius), (int)(Position.Y - Radius), (int)(Radius * 2), (int)(Radius * 2)), Color.Gray * alpha * 0.5f);
        
        // Knob
        Vector2 knobPos = Position + (Value * Radius);
        spriteBatch.Draw(pixel, new Rectangle((int)(knobPos.X - KnobRadius), (int)(knobPos.Y - KnobRadius), (int)(KnobRadius * 2), (int)(KnobRadius * 2)), Color.White * alpha);
    }
}
