namespace ReactUI.Style;

public enum GradientType { None, Linear, Radial }

public struct Gradient
{
    public GradientType Type;
    public float Angle;
    public UIColor ColorA;
    public UIColor ColorB;

    public static implicit operator Gradient(string s) => StyleParser.ParseGradient(s);

    public override string ToString() => Type switch
    {
        GradientType.Linear => $"linear-gradient({Angle}deg, {ColorA}, {ColorB})",
        GradientType.Radial => $"radial-gradient({ColorA}, {ColorB})",
        _ => "none",
    };
}
