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

    public static implicit operator StyleValue(float v) => Px(v);
    public static implicit operator StyleValue(int v) => Px(v);
    public static implicit operator StyleValue(string s) => StyleParser.ParseStyleValue(s);

    public bool IsDefined => Unit != StyleUnit.Undefined;

    public override string ToString() => Unit switch
    {
        StyleUnit.Px => $"{Value}px",
        StyleUnit.Percent => $"{Value}%",
        StyleUnit.Auto => "auto",
        _ => "undefined",
    };
}
