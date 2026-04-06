using System;
using System.Collections.Generic;
using System.Globalization;
using ReactUI.Style;

namespace ReactUI.Jsx;

/// <summary>
/// Parses CSS class definitions into a StyleSheet.
/// Supports simple class selectors (.className) with inline-style-like properties.
///
/// Example CSS:
///   .panel {
///       padding: 20;
///       background: #1a1a2e;
///       border-radius: 16;
///       gap: 12;
///   }
///   .title {
///       font-size: 22;
///       font-weight: 700;
///       color: #e0e0e0;
///   }
///   .btn-primary {
///       background: #7c3aed;
///       color: #fff;
///       border-radius: 6;
///       padding: 8 16;
///       cursor: pointer;
///   }
///   .btn-primary:hover {
///       opacity: 0.8;
///   }
///
/// Also usable from the C# API via StyleSheet + GlobalStyles.
/// </summary>
public static class CssParser
{
    /// <summary>
    /// Parse a CSS string into a StyleSheet.
    /// </summary>
    public static StyleSheet Parse(string css)
    {
        var sheet = new StyleSheet();
        if (string.IsNullOrWhiteSpace(css)) return sheet;

        int pos = 0;
        while (pos < css.Length)
        {
            // Skip whitespace and comments
            pos = SkipWhitespaceAndComments(css, pos);
            if (pos >= css.Length) break;

            // Read selector
            int selectorStart = pos;
            while (pos < css.Length && css[pos] != '{')
                pos++;
            if (pos >= css.Length) break;

            string selector = css.Substring(selectorStart, pos - selectorStart).Trim();
            pos++; // skip {

            // Read properties block
            int blockStart = pos;
            int depth = 1;
            while (pos < css.Length && depth > 0)
            {
                if (css[pos] == '{') depth++;
                else if (css[pos] == '}') depth--;
                if (depth > 0) pos++;
            }

            string block = css.Substring(blockStart, pos - blockStart).Trim();
            if (pos < css.Length) pos++; // skip }

            // Parse selector — handle pseudo-states
            if (string.IsNullOrEmpty(selector)) continue;

            // Handle .class:hover, .class:active, .class:focus
            string className;
            string? pseudoState = null;

            int colonIdx = selector.IndexOf(':');
            if (colonIdx > 0)
            {
                className = selector.Substring(0, colonIdx).Trim();
                pseudoState = selector.Substring(colonIdx + 1).Trim();
            }
            else
            {
                className = selector;
            }

            // Parse the properties into a Style
            var props = ParseProperties(block);
            var style = StyleConverter.Convert(props);

            if (pseudoState != null)
            {
                // Merge pseudo-state into existing class
                var existing = sheet[className];
                switch (pseudoState.ToLowerInvariant())
                {
                    case "hover":
                        existing.Hover = existing.Hover?.Merge(style) ?? style;
                        break;
                    case "active":
                        existing.Active = existing.Active?.Merge(style) ?? style;
                        break;
                    case "focus":
                        existing.Focus = existing.Focus?.Merge(style) ?? style;
                        break;
                }
                sheet[className] = existing;
            }
            else
            {
                // Merge with existing (in case pseudo-states were parsed first)
                var existing = sheet.Contains(className) ? sheet[className] : null;
                if (existing != null)
                {
                    // Preserve any pseudo-states already set
                    var merged = style;
                    if (existing.Hover != null) merged.Hover = existing.Hover;
                    if (existing.Active != null) merged.Active = existing.Active;
                    if (existing.Focus != null) merged.Focus = existing.Focus;
                    sheet[className] = merged;
                }
                else
                {
                    sheet[className] = style;
                }
            }
        }

        return sheet;
    }

    /// <summary>
    /// Parse a CSS properties block (semicolon-separated key:value pairs)
    /// into a dictionary suitable for StyleConverter.Convert().
    /// Maps CSS kebab-case property names to camelCase JS-style names.
    /// </summary>
    internal static Dictionary<string, object?> ParseProperties(string block)
    {
        var props = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(block)) return props;

        var declarations = SplitDeclarations(block);

        foreach (var decl in declarations)
        {
            int colonIdx = decl.IndexOf(':');
            if (colonIdx <= 0) continue;

            string property = decl.Substring(0, colonIdx).Trim();
            string value = decl.Substring(colonIdx + 1).Trim();
            if (value.EndsWith(";")) value = value.Substring(0, value.Length - 1).Trim();

            // Convert CSS kebab-case to JS camelCase
            string camelProp = KebabToCamel(property);

            // Handle special property values
            props[camelProp] = ConvertCssValue(camelProp, value);
        }

        return props;
    }

    /// <summary>
    /// Parse an inline style string (like "padding: 20; background: #1a1a2e") into a Style.
    /// Used by MarkupStyleParser for the JSX style attribute in XML-based markup.
    /// </summary>
    public static ReactUI.Style.Style ParseInlineStyle(string inlineStyle)
    {
        var props = ParseProperties(inlineStyle);
        return StyleConverter.Convert(props);
    }

    // ─── Private helpers ────────────────────────

    private static List<string> SplitDeclarations(string block)
    {
        var result = new List<string>();
        int start = 0;
        int depth = 0;

        for (int i = 0; i < block.Length; i++)
        {
            char c = block[i];
            if (c == '(' || c == '{') depth++;
            else if (c == ')' || c == '}') depth--;
            else if (c == ';' && depth == 0)
            {
                var decl = block.Substring(start, i - start).Trim();
                if (decl.Length > 0) result.Add(decl);
                start = i + 1;
            }
        }

        // Last declaration (may not end with ;)
        var last = block.Substring(start).Trim();
        if (last.Length > 0) result.Add(last);

        return result;
    }

    private static string KebabToCamel(string s)
    {
        if (!s.Contains('-')) return s;
        var parts = s.Split('-');
        var sb = new System.Text.StringBuilder(parts[0]);
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                sb.Append(char.ToUpperInvariant(parts[i][0]));
                sb.Append(parts[i], 1, parts[i].Length - 1);
            }
        }
        return sb.ToString();
    }

    private static object ConvertCssValue(string property, string value)
    {
        // Try to parse as number first for numeric properties
        if (IsNumericProperty(property) &&
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
        {
            return f;
        }

        // Padding/margin: "8 16" or "8 16 8 16" stays as string (StyleConverter handles it)
        return value;
    }

    private static bool IsNumericProperty(string prop) => prop switch
    {
        "fontSize" or "fontWeight" or "lineHeight" or
        "gap" or "opacity" or "borderRadius" or "borderWidth" or
        "flexGrow" or "flexShrink" or "aspectRatio" or
        "zIndex" or "backdropBlur" => true,
        _ => false
    };

    private static int SkipWhitespaceAndComments(string s, int pos)
    {
        while (pos < s.Length)
        {
            if (char.IsWhiteSpace(s[pos]))
            {
                pos++;
                continue;
            }

            // Block comment: /* ... */
            if (pos + 1 < s.Length && s[pos] == '/' && s[pos + 1] == '*')
            {
                pos += 2;
                while (pos + 1 < s.Length && !(s[pos] == '*' && s[pos + 1] == '/'))
                    pos++;
                if (pos + 1 < s.Length) pos += 2;
                continue;
            }

            // Line comment: // ...
            if (pos + 1 < s.Length && s[pos] == '/' && s[pos + 1] == '/')
            {
                while (pos < s.Length && s[pos] != '\n')
                    pos++;
                continue;
            }

            break;
        }
        return pos;
    }
}
