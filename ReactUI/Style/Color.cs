namespace ReactUI.Style;

public struct UIColor
{
    public float R, G, B, A;

    public UIColor(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static UIColor FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Transparent;

        hex = hex.TrimStart('#');

        return hex.Length switch
        {
            // #RGB
            3 => new UIColor(
                ParseHexChar(hex[0]) / 15f,
                ParseHexChar(hex[1]) / 15f,
                ParseHexChar(hex[2]) / 15f),
            // #RGBA
            4 => new UIColor(
                ParseHexChar(hex[0]) / 15f,
                ParseHexChar(hex[1]) / 15f,
                ParseHexChar(hex[2]) / 15f,
                ParseHexChar(hex[3]) / 15f),
            // #RRGGBB
            6 => new UIColor(
                ParseHexByte(hex, 0) / 255f,
                ParseHexByte(hex, 2) / 255f,
                ParseHexByte(hex, 4) / 255f),
            // #RRGGBBAA
            8 => new UIColor(
                ParseHexByte(hex, 0) / 255f,
                ParseHexByte(hex, 2) / 255f,
                ParseHexByte(hex, 4) / 255f,
                ParseHexByte(hex, 6) / 255f),
            _ => Transparent,
        };
    }

    public static UIColor FromRgba(string rgba)
    {
        // Handles: rgba(r, g, b, a) and rgb(r, g, b)
        var inner = rgba;
        var start = rgba.IndexOf('(');
        var end = rgba.LastIndexOf(')');
        if (start >= 0 && end > start)
            inner = rgba.Substring(start + 1, end - start - 1);

        var parts = inner.Split(',');
        if (parts.Length < 3) return Transparent;

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        float r = float.Parse(parts[0].Trim(), inv) / 255f;
        float g = float.Parse(parts[1].Trim(), inv) / 255f;
        float b = float.Parse(parts[2].Trim(), inv) / 255f;
        float a = parts.Length >= 4 ? float.Parse(parts[3].Trim(), inv) : 1f;

        return new UIColor(r, g, b, a);
    }

    public static UIColor Lerp(UIColor a, UIColor b, float t) => new(
        a.R + (b.R - a.R) * t,
        a.G + (b.G - a.G) * t,
        a.B + (b.B - a.B) * t,
        a.A + (b.A - a.A) * t);

    public static implicit operator UIColor(string s) => StyleParser.ParseColor(s);

    public UnityEngine.Color ToUnityColor() => new(R, G, B, A);

    public static readonly UIColor Transparent = new(0, 0, 0, 0);
    public static readonly UIColor White = new(1, 1, 1, 1);
    public static readonly UIColor Black = new(0, 0, 0, 1);

    private static int ParseHexChar(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => 0,
    };

    private static int ParseHexByte(string hex, int offset) =>
        ParseHexChar(hex[offset]) * 16 + ParseHexChar(hex[offset + 1]);

    public override string ToString() =>
        $"rgba({(int)(R * 255)}, {(int)(G * 255)}, {(int)(B * 255)}, {A:F2})";
}
