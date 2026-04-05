namespace ReactUI.Rendering;

public enum DrawType { SdfRect, BlurRect, Text, Image }

public struct DrawCommand
{
    public DrawType Type;
    public Core.Rect Rect;
    public Core.Rect ClipRect;
    public int ZOrder;

    // SDF Rect properties
    public Style.UIColor BackgroundColor;
    public Style.Gradient? Gradient;
    public float BorderRadiusTL, BorderRadiusTR, BorderRadiusBR, BorderRadiusBL;
    public Style.UIColor BorderColor;
    public float BorderWidth;
    public Style.BoxShadow? Shadow;
    public float Opacity;
    public float BackdropBlur;

    // Text properties
    public string? Text;
    public Style.UIColor TextColor;
    public float FontSize;
    public int FontWeight;
    public Style.TextAlign TextAlign;
    public float LineHeight;

    // Image properties
    public UnityEngine.Texture2D? Texture;
    public Style.ObjectFit ObjectFit;
}
