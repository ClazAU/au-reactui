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
        Tag,        // JSX tag names: <div>, </div>
        Attribute,  // JSX attribute names
        Operator,
        Punctuation,
        Function,
        Constant,   // true, false, null, undefined
    }

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

    /// <summary>Get the color for a token type (VS Code dark+ inspired).</summary>
    public static UIColor GetColor(TokenType type) => type switch
    {
        TokenType.Keyword    => UIColor.FromHex("#c586c0"), // purple
        TokenType.String     => UIColor.FromHex("#ce9178"), // orange
        TokenType.Number     => UIColor.FromHex("#b5cea8"), // green
        TokenType.Comment    => UIColor.FromHex("#6a9955"), // green-gray
        TokenType.Tag        => UIColor.FromHex("#569cd6"), // blue
        TokenType.Attribute  => UIColor.FromHex("#9cdcfe"), // light blue
        TokenType.Operator   => UIColor.FromHex("#d4d4d4"), // white-ish
        TokenType.Function   => UIColor.FromHex("#dcdcaa"), // yellow
        TokenType.Constant   => UIColor.FromHex("#569cd6"), // blue
        TokenType.Punctuation=> UIColor.FromHex("#808080"), // gray
        _                    => UIColor.FromHex("#d4d4d4"), // default
    };
}
