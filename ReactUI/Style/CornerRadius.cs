namespace ReactUI.Style;

public struct CornerRadius
{
    public float TopLeft, TopRight, BottomRight, BottomLeft;

    public CornerRadius(float all)
    {
        TopLeft = TopRight = BottomRight = BottomLeft = all;
    }

    public CornerRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public static implicit operator CornerRadius(float v) => new(v);
    public static implicit operator CornerRadius((float tl, float tr, float br, float bl) t) => new(t.tl, t.tr, t.br, t.bl);
}
