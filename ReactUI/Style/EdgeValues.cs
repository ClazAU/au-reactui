namespace ReactUI.Style;

public struct EdgeValues
{
    public float Top, Right, Bottom, Left;

    public EdgeValues(float all)
    {
        Top = Right = Bottom = Left = all;
    }

    public EdgeValues(float vertical, float horizontal)
    {
        Top = Bottom = vertical;
        Right = Left = horizontal;
    }

    public EdgeValues(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public static implicit operator EdgeValues(float v) => new(v);
    public static implicit operator EdgeValues((float v, float h) t) => new(t.v, t.h);
    public static implicit operator EdgeValues((float t, float r, float b, float l) t) => new(t.t, t.r, t.b, t.l);

    public static EdgeValues Lerp(EdgeValues a, EdgeValues b, float t) => new(
        a.Top + (b.Top - a.Top) * t,
        a.Right + (b.Right - a.Right) * t,
        a.Bottom + (b.Bottom - a.Bottom) * t,
        a.Left + (b.Left - a.Left) * t);

    public override string ToString() =>
        Top == Right && Right == Bottom && Bottom == Left
            ? $"{Top}"
            : Top == Bottom && Right == Left
                ? $"{Top} {Right}"
                : $"{Top} {Right} {Bottom} {Left}";
}
