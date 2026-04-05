using System;
using System.Globalization;

namespace ReactUI.Style;

/// <summary>
/// Static parsing methods for CSS-like style values.
/// All float parsing uses InvariantCulture to avoid locale issues.
/// IL2CPP safe: no Reflection.Emit, no dynamic.
/// </summary>
public static class StyleParser
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>
    /// Parses a color string. Supports:
    /// - Hex: #RGB, #RGBA, #RRGGBB, #RRGGBBAA
    /// - Functional: rgb(r,g,b), rgba(r,g,b,a)
    /// - Named: "transparent", "white", "black"
    /// </summary>
    public static UIColor ParseColor(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return UIColor.Transparent;

        s = s.Trim();

        // Named colors
        switch (s.ToLowerInvariant())
        {
            case "transparent": return UIColor.Transparent;
            case "white": return UIColor.White;
            case "black": return UIColor.Black;
            case "red": return new UIColor(1, 0, 0, 1);
            case "green": return new UIColor(0, 0.5f, 0, 1);
            case "blue": return new UIColor(0, 0, 1, 1);
            case "yellow": return new UIColor(1, 1, 0, 1);
            case "cyan": return new UIColor(0, 1, 1, 1);
            case "magenta": return new UIColor(1, 0, 1, 1);
            case "gray": case "grey": return new UIColor(0.5f, 0.5f, 0.5f, 1);
        }

        // Hex
        if (s.StartsWith('#'))
            return UIColor.FromHex(s);

        // rgb(...) / rgba(...)
        if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            return UIColor.FromRgba(s);

        return UIColor.Transparent;
    }

    /// <summary>
    /// Parses a style value string. Supports:
    /// - "auto" -> StyleValue.Auto
    /// - "50%" -> StyleValue.Pct(50)
    /// - "16px" -> StyleValue.Px(16)
    /// - "16" -> StyleValue.Px(16)
    /// </summary>
    public static StyleValue ParseStyleValue(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return StyleValue.Undefined;

        s = s.Trim();

        if (s.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return StyleValue.Auto;

        if (s.EndsWith('%'))
        {
            if (float.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Float, Inv, out var pct))
                return StyleValue.Pct(pct);
            return StyleValue.Undefined;
        }

        if (s.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            if (float.TryParse(s.Substring(0, s.Length - 2), NumberStyles.Float, Inv, out var px))
                return StyleValue.Px(px);
            return StyleValue.Undefined;
        }

        // Plain number -> Px
        if (float.TryParse(s, NumberStyles.Float, Inv, out var val))
            return StyleValue.Px(val);

        return StyleValue.Undefined;
    }

    /// <summary>
    /// Parses a box-shadow string.
    /// Format: [inset] offsetX offsetY [blur [spread]] [color]
    /// Examples:
    ///   "0 4px 24px rgba(0,0,0,0.3)"
    ///   "inset 0 2px 4px 0 #000000"
    ///   "2px 2px 8px #333"
    /// </summary>
    public static BoxShadow ParseBoxShadow(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return default;

        s = s.Trim();
        var result = new BoxShadow();

        // Check for inset
        if (s.StartsWith("inset", StringComparison.OrdinalIgnoreCase))
        {
            result.Inset = true;
            s = s.Substring(5).TrimStart();
        }

        // Extract color portion at the end (could be rgba(...) or #hex)
        string colorStr = "";
        int rgbaStart = s.IndexOf("rgba(", StringComparison.OrdinalIgnoreCase);
        if (rgbaStart < 0)
            rgbaStart = s.IndexOf("rgb(", StringComparison.OrdinalIgnoreCase);

        if (rgbaStart >= 0)
        {
            colorStr = s.Substring(rgbaStart);
            s = s.Substring(0, rgbaStart).Trim();
        }

        // Split remaining into tokens
        var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Find numeric tokens and color token
        var numericValues = new float[4]; // offsetX, offsetY, blur, spread
        int numCount = 0;

        for (int i = 0; i < tokens.Length && numCount < 4; i++)
        {
            var token = tokens[i];
            if (token.StartsWith('#'))
            {
                // Hex color at end
                if (string.IsNullOrEmpty(colorStr))
                    colorStr = token;
            }
            else if (TryParseLength(token, out var v))
            {
                numericValues[numCount++] = v;
            }
            else if (string.IsNullOrEmpty(colorStr))
            {
                // Might be a named color
                colorStr = token;
            }
        }

        if (numCount >= 1) result.OffsetX = numericValues[0];
        if (numCount >= 2) result.OffsetY = numericValues[1];
        if (numCount >= 3) result.Blur = numericValues[2];
        if (numCount >= 4) result.Spread = numericValues[3];

        result.Color = !string.IsNullOrEmpty(colorStr)
            ? ParseColor(colorStr)
            : new UIColor(0, 0, 0, 0.5f);

        return result;
    }

    /// <summary>
    /// Parses a gradient string.
    /// Supports: "linear-gradient(135deg, #667eea, #764ba2)"
    ///           "radial-gradient(#667eea, #764ba2)"
    /// </summary>
    public static Gradient ParseGradient(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return new Gradient { Type = GradientType.None };

        s = s.Trim();

        if (s.StartsWith("linear-gradient(", StringComparison.OrdinalIgnoreCase))
        {
            var inner = ExtractParenContents(s);
            return ParseLinearGradient(inner);
        }

        if (s.StartsWith("radial-gradient(", StringComparison.OrdinalIgnoreCase))
        {
            var inner = ExtractParenContents(s);
            return ParseRadialGradient(inner);
        }

        return new Gradient { Type = GradientType.None };
    }

    /// <summary>
    /// Parses a transition string into an array of Transition structs.
    /// Delegates to Transition.Parse.
    /// Example: "background 0.15s ease, color 0.15s ease"
    /// </summary>
    public static Transition[] ParseTransitions(string s) => Transition.Parse(s);

    // ---- Private helpers ----

    private static Gradient ParseLinearGradient(string inner)
    {
        var result = new Gradient { Type = GradientType.Linear, Angle = 180f };

        // Split by commas, but respect nested parentheses (for rgba)
        var parts = SplitTopLevel(inner, ',');
        if (parts.Length == 0) return result;

        int colorStart = 0;

        // First part might be an angle
        var first = parts[0].Trim();
        if (first.EndsWith("deg", StringComparison.OrdinalIgnoreCase))
        {
            if (float.TryParse(first.Substring(0, first.Length - 3), NumberStyles.Float, Inv, out var angle))
                result.Angle = angle;
            colorStart = 1;
        }
        else if (first.StartsWith("to ", StringComparison.OrdinalIgnoreCase))
        {
            result.Angle = ParseDirectionToAngle(first);
            colorStart = 1;
        }

        if (colorStart < parts.Length)
            result.ColorA = ParseColor(parts[colorStart].Trim());
        if (colorStart + 1 < parts.Length)
            result.ColorB = ParseColor(parts[colorStart + 1].Trim());

        return result;
    }

    private static Gradient ParseRadialGradient(string inner)
    {
        var result = new Gradient { Type = GradientType.Radial };
        var parts = SplitTopLevel(inner, ',');

        if (parts.Length >= 1)
            result.ColorA = ParseColor(parts[0].Trim());
        if (parts.Length >= 2)
            result.ColorB = ParseColor(parts[1].Trim());

        return result;
    }

    private static float ParseDirectionToAngle(string dir)
    {
        dir = dir.Trim().ToLowerInvariant();
        return dir switch
        {
            "to top" => 0f,
            "to right" => 90f,
            "to bottom" => 180f,
            "to left" => 270f,
            "to top right" => 45f,
            "to bottom right" => 135f,
            "to bottom left" => 225f,
            "to top left" => 315f,
            _ => 180f,
        };
    }

    private static string ExtractParenContents(string s)
    {
        int start = s.IndexOf('(');
        int end = s.LastIndexOf(')');
        if (start >= 0 && end > start)
            return s.Substring(start + 1, end - start - 1);
        return s;
    }

    /// <summary>
    /// Splits a string by a delimiter but respects nested parentheses.
    /// E.g., "rgba(0,0,0,0.3), #fff" splits into ["rgba(0,0,0,0.3)", " #fff"]
    /// </summary>
    private static string[] SplitTopLevel(string s, char delimiter)
    {
        var results = new System.Collections.Generic.List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == delimiter && depth == 0)
            {
                results.Add(s.Substring(start, i - start));
                start = i + 1;
            }
        }

        results.Add(s.Substring(start));
        return results.ToArray();
    }

    /// <summary>
    /// Tries to parse a length token like "4px", "24", "0".
    /// </summary>
    private static bool TryParseLength(string token, out float value)
    {
        token = token.Trim();
        if (token.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            return float.TryParse(token.Substring(0, token.Length - 2), NumberStyles.Float, Inv, out value);
        return float.TryParse(token, NumberStyles.Float, Inv, out value);
    }
}
