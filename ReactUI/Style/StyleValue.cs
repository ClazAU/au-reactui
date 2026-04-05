namespace ReactUI.Style;

public enum StyleUnit { Undefined, Px, Percent, Auto }

public struct StyleValue
{
    public float Value;
    public StyleUnit Unit;

    public static readonly StyleValue Undefined = new() { Unit = StyleUnit.Undefined };
    public static readonly StyleValue Auto = new() { Unit = StyleUnit.Auto };

    public static StyleValue Px(float v) => new() { Value = v, Unit = StyleUnit.Px };
    public static StyleValue Pct(float v) => new() { Value = v, Unit = StyleUnit.Percent };
    public static StyleValue Percent(float v) => Pct(v);

    public static implicit operator StyleValue(float v) => Px(v);
    public static implicit operator StyleValue(int v) => Px(v);
    public static implicit operator StyleValue(string s) => StyleParser.ParseStyleValue(s);
    /// <summary>Implicit conversion to float — returns pixel value (0 for non-px units).</summary>
    public static implicit operator float(StyleValue sv) => sv.Unit == StyleUnit.Px ? sv.Value : 0f;

    public bool IsDefined => Unit != StyleUnit.Undefined;

    /// <summary>Lerp between two StyleValues. If units differ, snaps at t=0.5.</summary>
    public static StyleValue Lerp(StyleValue a, StyleValue b, float t)
    {
        if (a.Unit == b.Unit && (a.Unit == StyleUnit.Px || a.Unit == StyleUnit.Percent))
            return new StyleValue { Value = a.Value + (b.Value - a.Value) * t, Unit = a.Unit };
        return t >= 0.5f ? b : a;
    }

    public override bool Equals(object? obj) =>
        obj is StyleValue other && Value == other.Value && Unit == other.Unit;

    public override int GetHashCode() => (Value, Unit).GetHashCode();

    public override string ToString() => Unit switch
    {
        StyleUnit.Px => $"{Value}px",
        StyleUnit.Percent => $"{Value}%",
        StyleUnit.Auto => "auto",
        _ => "undefined",
    };
}
