using System;
using System.Collections.Generic;

namespace ReactUI.Style;

/// <summary>
/// A named collection of Style objects, like CSS classes.
/// Register styles globally or per-component, then reference by className.
///
/// Usage:
///   var styles = new StyleSheet {
///       [".panel"] = new Style { Background = "#1a1a2e", Padding = new EdgeValues(20), BorderRadius = 16 },
///       [".title"] = new Style { FontSize = 22, FontWeight = 700, Color = "#e0e0e0" },
///       [".btn-primary"] = new Style {
///           Background = "#7c3aed", Color = "#fff", BorderRadius = 6,
///           Padding = new EdgeValues(8, 16), Cursor = CursorType.Pointer,
///           Hover = new Style { Opacity = 0.8f },
///           Transitions = Transition.Parse("opacity 0.15s ease"),
///       },
///   };
///
///   // Register globally
///   UI.RegisterStyles(styles);
///
///   // Use in components
///   UI.Div(UI.ClassName("panel"),
///       UI.Text("Hello", UI.ClassName("title")),
///       UI.Button("Click", onClick, UI.ClassName("btn-primary"))
///   );
/// </summary>
public class StyleSheet
{
    private readonly Dictionary<string, Style> _styles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Get or set a named style. Names typically start with '.' but don't have to.</summary>
    public Style this[string name]
    {
        get => _styles.TryGetValue(Normalize(name), out var s) ? s : new Style();
        set => _styles[Normalize(name)] = value;
    }

    /// <summary>Check if a class name is defined.</summary>
    public bool Contains(string name) => _styles.ContainsKey(Normalize(name));

    /// <summary>
    /// Resolve a className string (space-separated class names) into a merged Style.
    /// Classes are merged left-to-right, later classes override earlier ones.
    /// </summary>
    public Style Resolve(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return new Style();

        var names = className.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 1)
            return ResolveOne(names[0]);

        var result = new Style();
        foreach (var name in names)
        {
            var s = ResolveOne(name);
            result = result.Merge(s);
        }
        return result;
    }

    /// <summary>
    /// Resolve a className and merge with an inline style override.
    /// Inline style wins over class style (same as CSS specificity).
    /// </summary>
    public Style Resolve(string? className, Style? inlineStyle)
    {
        if (string.IsNullOrWhiteSpace(className))
            return inlineStyle ?? new Style();
        if (inlineStyle == null)
            return Resolve(className);

        return Resolve(className).Merge(inlineStyle);
    }

    /// <summary>Merge another stylesheet into this one. The other sheet's styles override on conflict.</summary>
    public void Merge(StyleSheet other)
    {
        foreach (var (name, style) in other._styles)
            _styles[name] = style;
    }

    /// <summary>Remove a named style.</summary>
    public void Remove(string name) => _styles.Remove(Normalize(name));

    /// <summary>Clear all styles.</summary>
    public void Clear() => _styles.Clear();

    /// <summary>Get all defined class names.</summary>
    public IEnumerable<string> ClassNames => _styles.Keys;

    private Style ResolveOne(string name)
    {
        var key = Normalize(name);
        return _styles.TryGetValue(key, out var s) ? s : new Style();
    }

    private static string Normalize(string name) =>
        name.StartsWith('.') ? name : "." + name;
}

/// <summary>
/// Global stylesheet registry. Styles registered here are available to all components.
/// </summary>
public static class GlobalStyles
{
    private static readonly StyleSheet _global = new();

    /// <summary>The global stylesheet instance.</summary>
    public static StyleSheet Sheet => _global;

    /// <summary>Register styles into the global sheet.</summary>
    public static void Register(StyleSheet sheet) => _global.Merge(sheet);

    /// <summary>Register a single named style.</summary>
    public static void Register(string className, Style style) => _global[className] = style;

    /// <summary>Resolve a className against the global sheet.</summary>
    public static Style Resolve(string className) => _global.Resolve(className);

    /// <summary>Resolve a className + inline style against the global sheet.</summary>
    public static Style Resolve(string? className, Style? inlineStyle) =>
        _global.Resolve(className, inlineStyle);

    /// <summary>Clear all global styles.</summary>
    public static void Clear() => _global.Clear();
}
