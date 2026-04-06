using System;
using System.Collections.Generic;
using ReactUI.Style;

namespace ReactUI.Jsx;

/// <summary>
/// Converts JavaScript-style objects (camelCase property names, JS values)
/// into C# Style instances. Used by the Jint bridge when processing
/// the style prop on JSX elements.
///
/// Supports the standard React inline style conventions:
///   { backgroundColor: '#1a1a2e', padding: 20, borderRadius: 16 }
///   { padding: [8, 16] }  (EdgeValues shorthand)
///   { hover: { opacity: 0.8 } }  (pseudo-states)
///   { transition: 'opacity 0.15s ease' }
/// </summary>
public static class StyleConverter
{
    /// <summary>
    /// Convert a dictionary of JS property names → values into a Style.
    /// This is the main entry point called from JintBridge when a createElement
    /// call includes a style prop.
    /// </summary>
    public static ReactUI.Style.Style Convert(IDictionary<string, object?> jsProps)
    {
        var style = new ReactUI.Style.Style();

        foreach (var (key, value) in jsProps)
        {
            if (value == null) continue;
            if (_setters.TryGetValue(key, out var setter))
                setter(style, value);
        }

        return style;
    }

    // ─── Property setter registry ────────────────────────

    private static readonly Dictionary<string, Action<ReactUI.Style.Style, object>> _setters = new(StringComparer.OrdinalIgnoreCase)
    {
        // Layout
        ["flexDirection"] = (s, v) => s.FlexDirection = ParseEnum<FlexDirection>(v),
        ["justifyContent"] = (s, v) => s.JustifyContent = ParseEnum<JustifyContent>(v),
        ["alignItems"] = (s, v) => s.AlignItems = ParseEnum<AlignItems>(v),
        ["alignSelf"] = (s, v) => s.AlignSelf = ParseEnum<AlignSelf>(v),
        ["flexWrap"] = (s, v) => s.FlexWrap = ParseEnum<FlexWrap>(v),
        ["flexGrow"] = (s, v) => s.FlexGrow = ToFloat(v),
        ["flexShrink"] = (s, v) => s.FlexShrink = ToFloat(v),
        ["flexBasis"] = (s, v) => s.FlexBasis = ParseStyleValue(v),
        ["gap"] = (s, v) => s.Gap = ToFloat(v),
        ["position"] = (s, v) => s.Position = ParseEnum<PositionType>(v),
        ["overflow"] = (s, v) => s.Overflow = ParseEnum<Overflow>(v),
        ["aspectRatio"] = (s, v) => s.AspectRatio = ToFloat(v),

        // Dimensions
        ["width"] = (s, v) => s.Width = ParseStyleValue(v),
        ["height"] = (s, v) => s.Height = ParseStyleValue(v),
        ["minWidth"] = (s, v) => s.MinWidth = ParseStyleValue(v),
        ["minHeight"] = (s, v) => s.MinHeight = ParseStyleValue(v),
        ["maxWidth"] = (s, v) => s.MaxWidth = ParseStyleValue(v),
        ["maxHeight"] = (s, v) => s.MaxHeight = ParseStyleValue(v),

        // Spacing
        ["padding"] = (s, v) => s.Padding = ParseEdgeValues(v),
        ["margin"] = (s, v) => s.Margin = ParseEdgeValues(v),
        ["inset"] = (s, v) => s.Inset = ParseEdgeValues(v),

        // Visual
        ["background"] = (s, v) => SetBackground(s, v),
        ["backgroundColor"] = (s, v) => s.Background = ParseColor(v),
        ["backgroundGradient"] = (s, v) => s.BackgroundGradient = ParseGradient(v),
        ["borderRadius"] = (s, v) => SetBorderRadius(s, v),
        ["borderColor"] = (s, v) => s.BorderColor = ParseColor(v),
        ["borderWidth"] = (s, v) => s.BorderWidth = ToFloat(v),
        ["boxShadow"] = (s, v) => s.BoxShadow = StyleParser.ParseBoxShadow(v.ToString()!),
        ["opacity"] = (s, v) => s.Opacity = ToFloat(v),
        ["backdropBlur"] = (s, v) => s.BackdropBlur = ToFloat(v),
        ["zIndex"] = (s, v) => s.ZIndex = (int)ToFloat(v),

        // Text
        ["color"] = (s, v) => s.Color = ParseColor(v),
        ["fontSize"] = (s, v) => s.FontSize = ToFloat(v),
        ["fontWeight"] = (s, v) => s.FontWeight = (int)ToFloat(v),
        ["textAlign"] = (s, v) => s.TextAlign = ParseEnum<TextAlign>(v),
        ["lineHeight"] = (s, v) => s.LineHeight = ToFloat(v),
        ["fontFamily"] = (s, v) => s.FontFamily = v.ToString(),

        // Image
        ["objectFit"] = (s, v) => s.ObjectFit = ParseEnum<ObjectFit>(v),

        // Interaction
        ["cursor"] = (s, v) => s.Cursor = ParseEnum<CursorType>(v),
        ["pointerEvents"] = (s, v) => s.PointerEvents = ToBool(v),

        // Pseudo-states
        ["hover"] = (s, v) => s.Hover = ConvertNested(v),
        [":hover"] = (s, v) => s.Hover = ConvertNested(v),
        ["active"] = (s, v) => s.Active = ConvertNested(v),
        [":active"] = (s, v) => s.Active = ConvertNested(v),
        ["focus"] = (s, v) => s.Focus = ConvertNested(v),
        [":focus"] = (s, v) => s.Focus = ConvertNested(v),

        // Animation
        ["transition"] = (s, v) => s.Transitions = Transition.Parse(v.ToString()!),
    };

    // ─── Value converters ────────────────────────

    private static float ToFloat(object v) => v switch
    {
        float f => f,
        double d => (float)d,
        int i => i,
        long l => l,
        string s => float.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0,
        _ => 0
    };

    private static bool ToBool(object v) => v switch
    {
        bool b => b,
        string s => s == "true" || s == "auto" || s != "none",
        _ => true
    };

    private static T ParseEnum<T>(object v) where T : struct, Enum
    {
        var s = v.ToString()!;

        // Handle kebab-case CSS values: "flex-start" → "FlexStart", "space-between" → "SpaceBetween"
        var pascal = KebabToPascal(s);
        if (Enum.TryParse<T>(pascal, true, out var result))
            return result;

        // Try direct parse
        if (Enum.TryParse<T>(s, true, out result))
            return result;

        return default;
    }

    private static string KebabToPascal(string s)
    {
        if (!s.Contains('-')) return s;
        var parts = s.Split('-');
        var sb = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                sb.Append(part, 1, part.Length - 1);
            }
        }
        return sb.ToString();
    }

    private static UIColor ParseColor(object v)
    {
        if (v is string s) return StyleParser.ParseColor(s);
        return UIColor.Transparent;
    }

    private static Gradient ParseGradient(object v)
    {
        if (v is string s) return StyleParser.ParseGradient(s);
        return default;
    }

    private static StyleValue ParseStyleValue(object v) => v switch
    {
        float f => StyleValue.Px(f),
        double d => StyleValue.Px((float)d),
        int i => StyleValue.Px(i),
        string s => StyleParser.ParseStyleValue(s),
        _ => StyleValue.Undefined
    };

    private static EdgeValues ParseEdgeValues(object v)
    {
        // Number → all sides
        if (v is float f) return new EdgeValues(f);
        if (v is double d) return new EdgeValues((float)d);
        if (v is int i) return new EdgeValues(i);

        // Array → [vertical, horizontal] or [top, right, bottom, left]
        // Handle both IList<object?> and object[] (Jint returns object[])
        IList<object?>? list = v as IList<object?>;
        if (list == null && v is object[] arr)
            list = arr;
        if (list == null && v is System.Collections.IList rawList)
        {
            var converted = new List<object?>();
            foreach (var item in rawList)
                converted.Add(item);
            list = converted;
        }

        if (list != null)
        {
            return list.Count switch
            {
                1 => new EdgeValues(ToFloatOrNaN(list[0])),
                2 => new EdgeValues(ToFloatOrNaN(list[0]), ToFloatOrNaN(list[1])),
                3 => new EdgeValues(
                    StyleValue.Px(ToFloatOrNaN(list[0])), StyleValue.Px(ToFloatOrNaN(list[1])),
                    StyleValue.Px(ToFloatOrNaN(list[2])), StyleValue.Px(ToFloatOrNaN(list[1]))),
                >= 4 => new EdgeValues(
                    StyleValue.Px(ToFloatOrNaN(list[0])), StyleValue.Px(ToFloatOrNaN(list[1])),
                    StyleValue.Px(ToFloatOrNaN(list[2])), StyleValue.Px(ToFloatOrNaN(list[3]))),
                _ => new EdgeValues(0)
            };
        }

        // String → parse "8 16" or "8"
        if (v is string s)
        {
            var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                1 => new EdgeValues(ParseFloat(parts[0])),
                2 => new EdgeValues(ParseFloat(parts[0]), ParseFloat(parts[1])),
                3 => new EdgeValues(ParseFloat(parts[0]), ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[1])),
                >= 4 => new EdgeValues(ParseFloat(parts[0]), ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3])),
                _ => new EdgeValues(0)
            };
        }

        return new EdgeValues(0);
    }

    /// <summary>Convert to float, treating null as NaN (undefined/not-set).</summary>
    private static float ToFloatOrNaN(object? v) => v == null ? float.NaN : ToFloat(v);

    private static float ParseFloat(string s) =>
        float.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0;

    private static void SetBackground(ReactUI.Style.Style s, object v)
    {
        var str = v.ToString()!;
        if (str.Contains("gradient"))
            s.BackgroundGradient = StyleParser.ParseGradient(str);
        else
            s.Background = StyleParser.ParseColor(str);
    }

    private static void SetBorderRadius(ReactUI.Style.Style s, object v)
    {
        if (v is float f || v is double || v is int)
        {
            s.BorderRadius = ToFloat(v);
            return;
        }

        if (v is IList<object?> list && list.Count == 4)
        {
            s.BorderRadii = new CornerRadius(
                ToFloat(list[0]!), ToFloat(list[1]!),
                ToFloat(list[2]!), ToFloat(list[3]!)
            );
            return;
        }

        s.BorderRadius = ToFloat(v);
    }

    private static ReactUI.Style.Style? ConvertNested(object v)
    {
        if (v is IDictionary<string, object?> dict)
            return Convert(dict);
        return null;
    }
}
