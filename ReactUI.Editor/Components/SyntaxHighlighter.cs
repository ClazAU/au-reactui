using System.Collections.Generic;
using ReactUI.Style;

namespace ReactUI.Editor.Components;

/// <summary>
/// Lightweight JSX/JS syntax tokenizer for syntax highlighting.
/// Splits a line of code into colored tokens.
/// </summary>
public static class SyntaxHighlighter
{
    public enum TokenType
    {
        Plain,
        Keyword,
        String,
        Number,
        Comment,
        Tag,         // JSX tag names / CSS selectors
        Attribute,   // JSX attribute names / CSS property names
        Operator,
        Punctuation,
        Function,
        Constant,    // true, false, null, undefined
        CssValue,    // CSS property values
        CssUnit,     // px, %, em, etc.
    }

    public enum Language { Jsx, Css }

    public readonly record struct Token(string Text, TokenType Type);

    private static readonly HashSet<string> Keywords = new()
    {
        "const", "let", "var", "function", "return", "if", "else", "for",
        "while", "do", "switch", "case", "break", "continue", "new",
        "typeof", "instanceof", "in", "of", "class", "extends", "import",
        "export", "default", "from", "async", "await", "try", "catch",
        "finally", "throw", "yield", "this", "super"
    };

    private static readonly HashSet<string> Constants = new()
    {
        "true", "false", "null", "undefined", "NaN", "Infinity"
    };

    private static readonly HashSet<string> HookNames = new()
    {
        "useState", "useEffect", "useMemo", "useRef", "useCallback",
        "useContext", "useShared", "createElement"
    };

    /// <summary>Tokenize a single line of code.</summary>
    public static List<Token> Tokenize(string line)
    {
        var tokens = new List<Token>();
        int pos = 0;

        while (pos < line.Length)
        {
            char c = line[pos];

            // Whitespace
            if (char.IsWhiteSpace(c))
            {
                int start = pos;
                while (pos < line.Length && char.IsWhiteSpace(line[pos]))
                    pos++;
                tokens.Add(new Token(line[start..pos], TokenType.Plain));
                continue;
            }

            // Line comment
            if (c == '/' && pos + 1 < line.Length && line[pos + 1] == '/')
            {
                tokens.Add(new Token(line[pos..], TokenType.Comment));
                pos = line.Length;
                continue;
            }

            // Block comment start (won't handle multi-line, just color what's on this line)
            if (c == '/' && pos + 1 < line.Length && line[pos + 1] == '*')
            {
                int end = line.IndexOf("*/", pos + 2);
                if (end >= 0)
                {
                    end += 2;
                    tokens.Add(new Token(line[pos..end], TokenType.Comment));
                    pos = end;
                }
                else
                {
                    tokens.Add(new Token(line[pos..], TokenType.Comment));
                    pos = line.Length;
                }
                continue;
            }

            // String literals
            if (c == '\'' || c == '"' || c == '`')
            {
                int start = pos;
                pos++;
                while (pos < line.Length && line[pos] != c)
                {
                    if (line[pos] == '\\') pos++; // skip escaped char
                    pos++;
                }
                if (pos < line.Length) pos++; // closing quote
                tokens.Add(new Token(line[start..pos], TokenType.String));
                continue;
            }

            // Numbers
            if (char.IsDigit(c) || (c == '.' && pos + 1 < line.Length && char.IsDigit(line[pos + 1])))
            {
                int start = pos;
                while (pos < line.Length && (char.IsDigit(line[pos]) || line[pos] == '.' || line[pos] == 'x' ||
                       (line[pos] >= 'a' && line[pos] <= 'f') || (line[pos] >= 'A' && line[pos] <= 'F')))
                    pos++;
                tokens.Add(new Token(line[start..pos], TokenType.Number));
                continue;
            }

            // JSX tags: < or </
            if (c == '<' && pos + 1 < line.Length && (char.IsLetter(line[pos + 1]) || line[pos + 1] == '/'))
            {
                int start = pos;
                tokens.Add(new Token("<", TokenType.Punctuation));
                pos++;
                if (pos < line.Length && line[pos] == '/')
                {
                    tokens.Add(new Token("/", TokenType.Punctuation));
                    pos++;
                }
                // Tag name
                int nameStart = pos;
                while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '-'))
                    pos++;
                if (pos > nameStart)
                    tokens.Add(new Token(line[nameStart..pos], TokenType.Tag));

                // Attributes until > or />
                while (pos < line.Length && line[pos] != '>' && !(line[pos] == '/' && pos + 1 < line.Length && line[pos + 1] == '>'))
                {
                    if (char.IsWhiteSpace(line[pos]))
                    {
                        int ws = pos;
                        while (pos < line.Length && char.IsWhiteSpace(line[pos])) pos++;
                        tokens.Add(new Token(line[ws..pos], TokenType.Plain));
                    }
                    else if (char.IsLetter(line[pos]) || line[pos] == '_')
                    {
                        int attrStart = pos;
                        while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '-' || line[pos] == '_'))
                            pos++;
                        tokens.Add(new Token(line[attrStart..pos], TokenType.Attribute));
                    }
                    else if (line[pos] == '=')
                    {
                        tokens.Add(new Token("=", TokenType.Operator));
                        pos++;
                    }
                    else if (line[pos] == '{')
                    {
                        // Expression in JSX attribute — just grab the rest as plain until matching }
                        // (simplified: don't try to deeply parse)
                        break;
                    }
                    else if (line[pos] == '"' || line[pos] == '\'')
                    {
                        char q = line[pos];
                        int sStart = pos;
                        pos++;
                        while (pos < line.Length && line[pos] != q) pos++;
                        if (pos < line.Length) pos++;
                        tokens.Add(new Token(line[sStart..pos], TokenType.String));
                    }
                    else
                    {
                        tokens.Add(new Token(line[pos].ToString(), TokenType.Plain));
                        pos++;
                    }
                }

                // Closing > or />
                if (pos < line.Length && line[pos] == '/')
                {
                    tokens.Add(new Token("/", TokenType.Punctuation));
                    pos++;
                }
                if (pos < line.Length && line[pos] == '>')
                {
                    tokens.Add(new Token(">", TokenType.Punctuation));
                    pos++;
                }
                continue;
            }

            // Identifiers and keywords
            if (char.IsLetter(c) || c == '_' || c == '$')
            {
                int start = pos;
                while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_' || line[pos] == '$'))
                    pos++;
                string word = line[start..pos];

                if (Keywords.Contains(word))
                    tokens.Add(new Token(word, TokenType.Keyword));
                else if (Constants.Contains(word))
                    tokens.Add(new Token(word, TokenType.Constant));
                else if (HookNames.Contains(word))
                    tokens.Add(new Token(word, TokenType.Function));
                else if (pos < line.Length && line[pos] == '(')
                    tokens.Add(new Token(word, TokenType.Function));
                else
                    tokens.Add(new Token(word, TokenType.Plain));
                continue;
            }

            // Operators
            if ("=!<>+-*/%&|^~?:".IndexOf(c) >= 0)
            {
                int start = pos;
                // Consume multi-char operators: ===, !==, =>, >=, <=, &&, ||, etc.
                pos++;
                while (pos < line.Length && "=!<>+-*/%&|^~?:".IndexOf(line[pos]) >= 0 && pos - start < 3)
                    pos++;
                tokens.Add(new Token(line[start..pos], TokenType.Operator));
                continue;
            }

            // Punctuation: braces, parens, brackets, semicolons, commas, dots
            if ("{}[]();,.".IndexOf(c) >= 0)
            {
                tokens.Add(new Token(c.ToString(), TokenType.Punctuation));
                pos++;
                continue;
            }

            // Anything else
            tokens.Add(new Token(c.ToString(), TokenType.Plain));
            pos++;
        }

        return tokens;
    }

    /// <summary>Tokenize a line with language detection.</summary>
    public static List<Token> Tokenize(string line, Language language) => language switch
    {
        Language.Css => TokenizeCss(line),
        _ => Tokenize(line),
    };

    /// <summary>Detect language from file extension.</summary>
    public static Language DetectLanguage(string? filePath)
    {
        if (filePath == null) return Language.Jsx;
        if (filePath.EndsWith(".css", System.StringComparison.OrdinalIgnoreCase)) return Language.Css;
        return Language.Jsx;
    }

    // ─── CSS Tokenizer ────────────────────────

    private static readonly HashSet<string> CssProperties = new()
    {
        "padding", "margin", "background", "color", "border", "border-radius",
        "border-width", "border-color", "font-size", "font-weight", "font-family",
        "line-height", "text-align", "width", "height", "min-width", "min-height",
        "max-width", "max-height", "flex-direction", "flex-grow", "flex-shrink",
        "flex-basis", "flex-wrap", "justify-content", "align-items", "align-self",
        "gap", "position", "top", "right", "bottom", "left", "inset", "overflow",
        "opacity", "cursor", "display", "z-index", "box-shadow", "transition",
        "aspect-ratio", "object-fit", "pointer-events", "backdrop-blur",
    };

    private static readonly HashSet<string> CssValueKeywords = new()
    {
        "absolute", "relative", "fixed", "row", "column", "center", "stretch",
        "flex-start", "flex-end", "space-between", "space-around", "space-evenly",
        "hidden", "scroll", "visible", "wrap", "nowrap", "pointer", "auto",
        "none", "bold", "normal", "inherit", "initial", "ease", "ease-in",
        "ease-out", "ease-in-out", "linear",
    };

    /// <summary>Tokenize a single line of CSS.</summary>
    public static List<Token> TokenizeCss(string line)
    {
        var tokens = new List<Token>();
        int pos = 0;

        while (pos < line.Length)
        {
            char c = line[pos];

            // Whitespace
            if (char.IsWhiteSpace(c))
            {
                int start = pos;
                while (pos < line.Length && char.IsWhiteSpace(line[pos])) pos++;
                tokens.Add(new Token(line[start..pos], TokenType.Plain));
                continue;
            }

            // Comments
            if (c == '/' && pos + 1 < line.Length && line[pos + 1] == '/')
            {
                tokens.Add(new Token(line[pos..], TokenType.Comment));
                pos = line.Length;
                continue;
            }
            if (c == '/' && pos + 1 < line.Length && line[pos + 1] == '*')
            {
                int end = line.IndexOf("*/", pos + 2);
                if (end >= 0) { end += 2; } else { end = line.Length; }
                tokens.Add(new Token(line[pos..end], TokenType.Comment));
                pos = end;
                continue;
            }

            // Strings
            if (c == '\'' || c == '"')
            {
                int start = pos;
                pos++;
                while (pos < line.Length && line[pos] != c)
                {
                    if (line[pos] == '\\') pos++;
                    pos++;
                }
                if (pos < line.Length) pos++;
                tokens.Add(new Token(line[start..pos], TokenType.String));
                continue;
            }

            // Selectors: . or # at start or after whitespace/brace
            if ((c == '.' || c == '#') && pos + 1 < line.Length && char.IsLetter(line[pos + 1]))
            {
                int start = pos;
                pos++; // skip . or #
                while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '-' || line[pos] == '_'))
                    pos++;
                tokens.Add(new Token(line[start..pos], TokenType.Tag));
                continue;
            }

            // Pseudo-selectors: :hover, :active, :focus
            if (c == ':' && pos + 1 < line.Length && char.IsLetter(line[pos + 1]))
            {
                int start = pos;
                pos++;
                while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '-'))
                    pos++;
                tokens.Add(new Token(line[start..pos], TokenType.Function));
                continue;
            }

            // Numbers (including with units)
            if (char.IsDigit(c) || (c == '.' && pos + 1 < line.Length && char.IsDigit(line[pos + 1])))
            {
                int start = pos;
                while (pos < line.Length && (char.IsDigit(line[pos]) || line[pos] == '.'))
                    pos++;
                tokens.Add(new Token(line[start..pos], TokenType.Number));
                // Units: px, %, em, rem, s, ms, deg, etc.
                if (pos < line.Length && (char.IsLetter(line[pos]) || line[pos] == '%'))
                {
                    int uStart = pos;
                    while (pos < line.Length && (char.IsLetter(line[pos]) || line[pos] == '%'))
                        pos++;
                    tokens.Add(new Token(line[uStart..pos], TokenType.CssUnit));
                }
                continue;
            }

            // Hex colors: #fff, #1a1a2e, #ffffff80
            if (c == '#' && pos + 1 < line.Length && IsHexChar(line[pos + 1]))
            {
                int start = pos;
                pos++;
                while (pos < line.Length && IsHexChar(line[pos]))
                    pos++;
                tokens.Add(new Token(line[start..pos], TokenType.String));
                continue;
            }

            // Identifiers: property names or value keywords
            if (char.IsLetter(c) || c == '-' || c == '_')
            {
                int start = pos;
                while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '-' || line[pos] == '_'))
                    pos++;
                string word = line[start..pos];

                if (CssProperties.Contains(word))
                    tokens.Add(new Token(word, TokenType.Attribute));
                else if (CssValueKeywords.Contains(word))
                    tokens.Add(new Token(word, TokenType.CssValue));
                else if (word.StartsWith("rgba") || word.StartsWith("rgb") ||
                         word.StartsWith("linear-gradient") || word.StartsWith("radial-gradient"))
                    tokens.Add(new Token(word, TokenType.Function));
                else
                    tokens.Add(new Token(word, TokenType.Plain));
                continue;
            }

            // Braces and punctuation
            if ("{}();,".IndexOf(c) >= 0)
            {
                tokens.Add(new Token(c.ToString(), TokenType.Punctuation));
                pos++;
                continue;
            }

            // Colon (property separator)
            if (c == ':')
            {
                tokens.Add(new Token(":", TokenType.Operator));
                pos++;
                continue;
            }

            tokens.Add(new Token(c.ToString(), TokenType.Plain));
            pos++;
        }

        return tokens;
    }

    private static bool IsHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    /// <summary>Get the color for a token type (VS Code dark+ inspired).</summary>
    public static UIColor GetColor(TokenType type) => type switch
    {
        TokenType.Keyword    => UIColor.FromHex("#c586c0"), // purple
        TokenType.String     => UIColor.FromHex("#ce9178"), // orange
        TokenType.Number     => UIColor.FromHex("#b5cea8"), // green
        TokenType.Comment    => UIColor.FromHex("#6a9955"), // green-gray
        TokenType.Tag        => UIColor.FromHex("#d7ba7d"), // gold (CSS selectors)
        TokenType.Attribute  => UIColor.FromHex("#9cdcfe"), // light blue (CSS properties)
        TokenType.Operator   => UIColor.FromHex("#d4d4d4"), // white-ish
        TokenType.Function   => UIColor.FromHex("#dcdcaa"), // yellow
        TokenType.Constant   => UIColor.FromHex("#569cd6"), // blue
        TokenType.Punctuation=> UIColor.FromHex("#808080"), // gray
        TokenType.CssValue   => UIColor.FromHex("#ce9178"), // orange (CSS value keywords)
        TokenType.CssUnit    => UIColor.FromHex("#b5cea8"), // green (px, %, etc.)
        _                    => UIColor.FromHex("#d4d4d4"), // default
    };
}
