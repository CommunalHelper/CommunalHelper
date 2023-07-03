namespace Celeste.Mod.CommunalHelper.Utils;

public struct RectangleF
{
    public float X, Y;
    public float Width, Height;

    public readonly float Left => X;
    public readonly float Right => X + Width;
    public readonly float Top => Y;
    public readonly float Bottom => Y + Height;

    public readonly Vector2 Center => new(X + Width / 2f, Y + Height / 2f);

    public RectangleF(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }

    public RectangleF(Vector2 topleft, Vector2 bottomright)
    {
        X = topleft.X;
        Y = topleft.Y;
        Width = bottomright.X - topleft.X;
        Height = bottomright.Y - topleft.Y;
    }

    public readonly bool Intersects(RectangleF other)
        => other.Left < Right
        && Left < other.Right
        && other.Top < Bottom
        && Top < other.Bottom;

    public static RectangleF ClampTo(RectangleF rect, RectangleF bounds)
    {
        if (rect.X < bounds.X)
        {
            rect.Width -= bounds.X - rect.X;
            rect.X = bounds.X;
        }

        if (rect.Y < bounds.Y)
        {
            rect.Height -= bounds.Y - rect.Y;
            rect.Y = bounds.Y;
        }

        if (rect.Right > bounds.Right)
            rect.Width = bounds.Right - rect.X;

        if (rect.Bottom > bounds.Bottom)
            rect.Height = bounds.Bottom - rect.Y;

        return rect;
    }

    public static implicit operator RectangleF(Rectangle rect)
        => new(rect.X, rect.Y, rect.Width, rect.Height);
}
