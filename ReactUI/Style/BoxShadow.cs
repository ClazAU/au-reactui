namespace ReactUI.Style;

public struct BoxShadow
{
    public float OffsetX, OffsetY;
    public float Blur;
    public float Spread;
    public UIColor Color;
    public bool Inset;

    public static implicit operator BoxShadow(string s) => StyleParser.ParseBoxShadow(s);

    public static BoxShadow Lerp(BoxShadow a, BoxShadow b, float t) => new()
    {
        OffsetX = a.OffsetX + (b.OffsetX - a.OffsetX) * t,
        OffsetY = a.OffsetY + (b.OffsetY - a.OffsetY) * t,
        Blur = a.Blur + (b.Blur - a.Blur) * t,
        Spread = a.Spread + (b.Spread - a.Spread) * t,
        Color = UIColor.Lerp(a.Color, b.Color, t),
        Inset = a.Inset,
    };

    public override string ToString()
    {
        var insetStr = Inset ? "inset " : "";
        return $"{insetStr}{OffsetX}px {OffsetY}px {Blur}px {Spread}px {Color}";
    }
}
