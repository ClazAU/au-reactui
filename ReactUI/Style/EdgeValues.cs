namespace ReactUI.Style;

public struct EdgeValues
{
    public StyleValue Top, Right, Bottom, Left;

    public EdgeValues(float all)
    {
        Top = Right = Bottom = Left = StyleValue.Px(all);
    }

    public EdgeValues(float vertical, float horizontal)
    {
        Top = Bottom = StyleValue.Px(vertical);
        Right = Left = StyleValue.Px(horizontal);
    }

    public EdgeValues(StyleValue top, StyleValue right, StyleValue bottom, StyleValue left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public static implicit operator EdgeValues(float v) => new(v);
    public static implicit operator EdgeValues((float v, float h) t) => new(t.v, t.h);
    public static implicit operator EdgeValues((float t, float r, float b, float l) t) => new(t.t, t.r, t.b, t.l);

    /// <summary>Resolve a single edge to pixels given a parent size for percent resolution.</summary>
    public static float Resolve(StyleValue sv, float parentSize)
    {
        return sv.Unit switch
        {
            StyleUnit.Px => sv.Value,
            StyleUnit.Percent => parentSize * sv.Value / 100f,
            _ => 0f,
        };
    }

    public static EdgeValues Lerp(EdgeValues a, EdgeValues b, float t) => new(
        StyleValue.Lerp(a.Top, b.Top, t),
        StyleValue.Lerp(a.Right, b.Right, t),
        StyleValue.Lerp(a.Bottom, b.Bottom, t),
        StyleValue.Lerp(a.Left, b.Left, t));

    public override string ToString() =>
        $"{Top} {Right} {Bottom} {Left}";
}
