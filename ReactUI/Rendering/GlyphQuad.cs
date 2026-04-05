namespace ReactUI.Rendering;

public struct GlyphQuad
{
    public Core.Rect Rect;      // screen position
    public Core.Rect UVRect;    // atlas UV coordinates
    public Style.UIColor Color;
}

public struct GlyphMetrics
{
    public float Advance;       // horizontal advance
    public float BearingX;      // left side bearing
    public float BearingY;      // top bearing
    public float Width;         // glyph bitmap width
    public float Height;        // glyph bitmap height
    public Core.Rect UVRect;   // position in atlas
}
