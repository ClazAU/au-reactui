using System;
using System.Collections.Generic;
using System.Text;

namespace ReactUI.Jsx;

/// <summary>
/// Transforms JSX source into plain JavaScript with createElement() calls.
/// This is a lightweight equivalent of Babel's @babel/plugin-transform-react-jsx.
///
/// Example:
///   <div style={{padding: 20}}>Hello</div>
/// becomes:
///   createElement('div', {style: {padding: 20}}, 'Hello')
/// </summary>
public static class JsxTransformer
{
    /// <summary>
    /// Transform a JSX source string into plain JS that Jint can evaluate.
    /// </summary>
    public static string Transform(string source)
    {
        var ctx = new TransformContext(source);
        TransformRegion(ctx, 0, source.Length);
        return ctx.Output.ToString();
    }

    private sealed class TransformContext
    {
        public readonly string Source;
        public readonly StringBuilder Output = new();
        public int Pos;

        public TransformContext(string source)
        {
            Source = source;
            Pos = 0;
        }

        public char Current => Pos < Source.Length ? Source[Pos] : '\0';
        public char Peek(int offset = 1) => Pos + offset < Source.Length ? Source[Pos + offset] : '\0';
        public bool AtEnd => Pos >= Source.Length;
        public bool Match(string s) => Source.AsSpan(Pos).StartsWith(s.AsSpan());
    }

    private static void TransformRegion(TransformContext ctx, int start, int end)
    {
        ctx.Pos = start;
        while (ctx.Pos < end)
        {
            // Skip string literals
            if (ctx.Current == '\'' || ctx.Current == '"' || ctx.Current == '`')
            {
                CopyStringLiteral(ctx);
                continue;
            }

            // Skip line comments
            if (ctx.Current == '/' && ctx.Peek() == '/')
            {
                CopyLineComment(ctx);
                continue;
            }

            // Skip block comments
            if (ctx.Current == '/' && ctx.Peek() == '*')
            {
                CopyBlockComment(ctx);
                continue;
            }

            // Check for JSX
            if (ctx.Current == '<' && IsJsxStart(ctx))
            {
                TransformJsxElement(ctx);
                continue;
            }

            // Regular character — copy through
            ctx.Output.Append(ctx.Current);
            ctx.Pos++;
        }
    }

    /// <summary>
    /// Determines whether a '<' at the current position starts a JSX element
    /// rather than a comparison operator.
    /// </summary>
    private static bool IsJsxStart(TransformContext ctx)
    {
        int pos = ctx.Pos;

        // Fragment: <>
        if (ctx.Peek() == '>')
            return true;

        char next = ctx.Peek();

        // Must be followed by a letter (tag name) or / (closing tag)
        if (!char.IsLetter(next) && next != '/')
            return false;

        // Look backwards to see if this is in an expression context
        // (after =, (, [, {, return, ?, :, ,, &&, ||, !)
        int back = pos - 1;
        while (back >= 0 && char.IsWhiteSpace(ctx.Source[back]))
            back--;

        if (back < 0)
            return true; // start of input

        char prev = ctx.Source[back];

        // These characters precede an expression (so < is JSX, not comparison)
        if (prev == '=' || prev == '(' || prev == '[' || prev == '{' ||
            prev == '?' || prev == ':' || prev == ',' || prev == '!' ||
            prev == '&' || prev == '|' || prev == ';' || prev == '>' ||
            prev == '+' || prev == '-' || prev == '*' || prev == '/')
            return true;

        // Check for 'return' keyword
        if (back >= 5)
        {
            var span = ctx.Source.AsSpan(back - 5, 6);
            if (span.SequenceEqual("return".AsSpan()))
            {
                // Make sure it's actually the keyword (preceded by non-identifier char)
                int beforeReturn = back - 6;
                if (beforeReturn < 0 || !IsIdentChar(ctx.Source[beforeReturn]))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Transform a JSX element starting at '<'. Handles:
    /// - Regular elements: <div ...>...</div>
    /// - Self-closing: <div ... />
    /// - Fragments: <>...</>
    /// </summary>
    private static void TransformJsxElement(TransformContext ctx)
    {
        // Fragment: <>...</>
        if (ctx.Current == '<' && ctx.Peek() == '>')
        {
            ctx.Pos += 2; // skip <>
            ctx.Output.Append("createElement(Fragment, null");
            TransformJsxChildren(ctx, "");
            ctx.Output.Append(')');
            return;
        }

        ctx.Pos++; // skip <

        // Parse tag name
        string tagName = ReadIdentifier(ctx);
        if (string.IsNullOrEmpty(tagName))
        {
            ctx.Output.Append('<');
            return;
        }

        // Determine if tag is a component (uppercase) or element (lowercase)
        bool isComponent = char.IsUpper(tagName[0]);

        ctx.Output.Append("createElement(");
        if (isComponent)
            ctx.Output.Append(tagName);
        else
            ctx.Output.Append('\'').Append(tagName).Append('\'');

        // Parse attributes
        SkipWhitespace(ctx);
        bool hasProps = false;
        var propsBuilder = new StringBuilder();

        while (!ctx.AtEnd && ctx.Current != '>' && ctx.Current != '/')
        {
            SkipWhitespace(ctx);
            if (ctx.Current == '>' || ctx.Current == '/')
                break;

            if (!hasProps)
            {
                propsBuilder.Append('{');
                hasProps = true;
            }
            else
            {
                propsBuilder.Append(", ");
            }

            // Spread: {...expr}
            if (ctx.Current == '{' && ctx.Peek() == '.' && ctx.Peek(2) == '.' && ctx.Peek(3) == '.')
            {
                // Not fully supported but don't crash — copy as Object.assign
                ctx.Pos++; // {
                ctx.Pos += 3; // ...
                string spreadExpr = ReadJsExpression(ctx);
                ctx.Pos++; // }
                propsBuilder.Append("...(").Append(spreadExpr).Append(')');
                SkipWhitespace(ctx);
                continue;
            }

            // Attribute name
            string attrName = ReadIdentifier(ctx);
            if (string.IsNullOrEmpty(attrName))
            {
                ctx.Pos++;
                continue;
            }

            SkipWhitespace(ctx);

            if (ctx.Current == '=')
            {
                ctx.Pos++; // skip =
                SkipWhitespace(ctx);

                propsBuilder.Append(QuotePropertyName(attrName)).Append(": ");

                if (ctx.Current == '{')
                {
                    // Expression value: onClick={() => ...}
                    ctx.Pos++; // skip {
                    string expr = ReadJsExpression(ctx);
                    ctx.Pos++; // skip }
                    propsBuilder.Append(expr);
                }
                else if (ctx.Current == '"' || ctx.Current == '\'')
                {
                    // String value: placeholder="hello"
                    char quote = ctx.Current;
                    ctx.Pos++;
                    int strStart = ctx.Pos;
                    while (!ctx.AtEnd && ctx.Current != quote)
                        ctx.Pos++;
                    string strVal = ctx.Source.Substring(strStart, ctx.Pos - strStart);
                    ctx.Pos++; // skip closing quote
                    propsBuilder.Append('\'').Append(EscapeJsString(strVal)).Append('\'');
                }
                else
                {
                    // Bare value (shouldn't happen in valid JSX but handle gracefully)
                    string val = ReadUntilWhitespaceOrSpecial(ctx);
                    propsBuilder.Append(val);
                }
            }
            else
            {
                // Boolean shorthand: <input disabled /> → disabled: true
                propsBuilder.Append(QuotePropertyName(attrName)).Append(": true");
            }

            SkipWhitespace(ctx);
        }

        if (hasProps)
        {
            propsBuilder.Append('}');
            ctx.Output.Append(", ").Append(propsBuilder);
        }
        else
        {
            ctx.Output.Append(", null");
        }

        // Self-closing tag: />
        if (ctx.Current == '/')
        {
            ctx.Pos++; // skip /
            if (ctx.Current == '>')
                ctx.Pos++; // skip >
            ctx.Output.Append(')');
            return;
        }

        // Opening tag close: >
        if (ctx.Current == '>')
            ctx.Pos++; // skip >

        // Parse children until closing tag
        TransformJsxChildren(ctx, tagName);
        ctx.Output.Append(')');
    }

    /// <summary>
    /// Parse JSX children between opening and closing tags.
    /// Handles text, {expressions}, and nested JSX elements.
    /// </summary>
    private static void TransformJsxChildren(TransformContext ctx, string parentTag)
    {
        while (!ctx.AtEnd)
        {
            // Check for closing tag
            if (ctx.Current == '<')
            {
                if (ctx.Peek() == '/')
                {
                    // Closing tag: </div> or </>
                    ctx.Pos += 2; // skip </
                    if (parentTag.Length == 0)
                    {
                        // Fragment close: </>
                        if (ctx.Current == '>')
                            ctx.Pos++;
                    }
                    else
                    {
                        // Named close: </tagName>
                        ReadIdentifier(ctx);
                        SkipWhitespace(ctx);
                        if (ctx.Current == '>')
                            ctx.Pos++;
                    }
                    return;
                }

                // Nested JSX element
                if (IsJsxStart(ctx))
                {
                    ctx.Output.Append(", ");
                    TransformJsxElement(ctx);
                    continue;
                }
            }

            // Expression: {count + 1}
            if (ctx.Current == '{')
            {
                ctx.Pos++; // skip {
                string expr = ReadJsExpression(ctx);
                ctx.Pos++; // skip }
                ctx.Output.Append(", ").Append(expr);
                continue;
            }

            // Text content
            int textStart = ctx.Pos;
            while (!ctx.AtEnd && ctx.Current != '<' && ctx.Current != '{')
                ctx.Pos++;

            string text = ctx.Source.Substring(textStart, ctx.Pos - textStart).Trim();
            if (text.Length > 0)
            {
                ctx.Output.Append(", '").Append(EscapeJsString(text)).Append('\'');
            }
        }
    }

    /// <summary>
    /// Read a JS expression inside { }, respecting nested braces, parens,
    /// brackets, string literals, and template literals.
    /// Stops when the matching } is found (at depth 0).
    /// Also handles JSX inside expressions: {condition && <div>yes</div>}
    /// </summary>
    private static string ReadJsExpression(TransformContext ctx)
    {
        var expr = new StringBuilder();
        int depth = 0;

        while (!ctx.AtEnd)
        {
            char c = ctx.Current;

            // Nested braces
            if (c == '{')
            {
                depth++;
                expr.Append(c);
                ctx.Pos++;
                continue;
            }

            if (c == '}')
            {
                if (depth == 0)
                    break; // end of expression
                depth--;
                expr.Append(c);
                ctx.Pos++;
                continue;
            }

            // String literals
            if (c == '\'' || c == '"' || c == '`')
            {
                int before = expr.Length;
                var tempOutput = ctx.Output;
                ctx.Output.Clear(); // temporarily hijack — actually we shouldn't do this
                // Instead, read the string manually
                expr.Append(ReadStringLiteralRaw(ctx));
                continue;
            }

            // Parens / brackets (track depth so we don't break on } inside them)
            if (c == '(' || c == '[')
            {
                depth++;
                expr.Append(c);
                ctx.Pos++;
                continue;
            }

            if (c == ')' || c == ']')
            {
                depth--;
                expr.Append(c);
                ctx.Pos++;
                continue;
            }

            // JSX inside expression: {condition && <div>yes</div>}
            if (c == '<' && IsJsxStart(ctx))
            {
                // Save the main output, transform JSX into the expression
                var savedOutput = new StringBuilder();
                savedOutput.Append(ctx.Output);
                ctx.Output.Clear();
                TransformJsxElement(ctx);
                expr.Append(ctx.Output);
                ctx.Output.Clear();
                ctx.Output.Append(savedOutput);
                continue;
            }

            // Line comment
            if (c == '/' && ctx.Peek() == '/')
            {
                while (!ctx.AtEnd && ctx.Current != '\n')
                {
                    expr.Append(ctx.Current);
                    ctx.Pos++;
                }
                continue;
            }

            // Block comment
            if (c == '/' && ctx.Peek() == '*')
            {
                expr.Append(c);
                ctx.Pos++;
                expr.Append(ctx.Current);
                ctx.Pos++;
                while (!ctx.AtEnd && !(ctx.Current == '*' && ctx.Peek() == '/'))
                {
                    expr.Append(ctx.Current);
                    ctx.Pos++;
                }
                if (!ctx.AtEnd)
                {
                    expr.Append('*');
                    ctx.Pos++;
                    expr.Append('/');
                    ctx.Pos++;
                }
                continue;
            }

            expr.Append(c);
            ctx.Pos++;
        }

        return expr.ToString();
    }

    /// <summary>
    /// Read a string literal (single-quoted, double-quoted, or template literal)
    /// and return the raw source including quotes.
    /// </summary>
    private static string ReadStringLiteralRaw(TransformContext ctx)
    {
        var sb = new StringBuilder();
        char quote = ctx.Current;
        sb.Append(quote);
        ctx.Pos++;

        if (quote == '`')
        {
            // Template literal — handle ${} interpolations
            while (!ctx.AtEnd && ctx.Current != '`')
            {
                if (ctx.Current == '\\')
                {
                    sb.Append(ctx.Current);
                    ctx.Pos++;
                    if (!ctx.AtEnd)
                    {
                        sb.Append(ctx.Current);
                        ctx.Pos++;
                    }
                }
                else if (ctx.Current == '$' && ctx.Peek() == '{')
                {
                    sb.Append("${");
                    ctx.Pos += 2;
                    int depth = 1;
                    while (!ctx.AtEnd && depth > 0)
                    {
                        if (ctx.Current == '{') depth++;
                        else if (ctx.Current == '}') depth--;
                        if (depth > 0)
                        {
                            sb.Append(ctx.Current);
                            ctx.Pos++;
                        }
                    }
                    sb.Append('}');
                    if (!ctx.AtEnd) ctx.Pos++; // skip closing }
                }
                else
                {
                    sb.Append(ctx.Current);
                    ctx.Pos++;
                }
            }
            if (!ctx.AtEnd)
            {
                sb.Append('`');
                ctx.Pos++;
            }
        }
        else
        {
            // Regular string literal
            while (!ctx.AtEnd && ctx.Current != quote)
            {
                if (ctx.Current == '\\')
                {
                    sb.Append(ctx.Current);
                    ctx.Pos++;
                    if (!ctx.AtEnd)
                    {
                        sb.Append(ctx.Current);
                        ctx.Pos++;
                    }
                }
                else
                {
                    sb.Append(ctx.Current);
                    ctx.Pos++;
                }
            }
            if (!ctx.AtEnd)
            {
                sb.Append(quote);
                ctx.Pos++;
            }
        }

        return sb.ToString();
    }

    // ─── Utility methods ─────────────────────────────────

    private static string ReadIdentifier(TransformContext ctx)
    {
        int start = ctx.Pos;
        // Allow letters, digits, underscore, dollar, hyphen (for HTML tags), dot (for namespaced)
        while (!ctx.AtEnd && (IsIdentChar(ctx.Current) || ctx.Current == '-'))
            ctx.Pos++;
        return ctx.Source.Substring(start, ctx.Pos - start);
    }

    private static bool IsIdentChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || c == '$';

    private static void SkipWhitespace(TransformContext ctx)
    {
        while (!ctx.AtEnd && char.IsWhiteSpace(ctx.Current))
            ctx.Pos++;
    }

    private static string ReadUntilWhitespaceOrSpecial(TransformContext ctx)
    {
        int start = ctx.Pos;
        while (!ctx.AtEnd && !char.IsWhiteSpace(ctx.Current) &&
               ctx.Current != '>' && ctx.Current != '/' && ctx.Current != '=')
            ctx.Pos++;
        return ctx.Source.Substring(start, ctx.Pos - start);
    }

    private static void CopyStringLiteral(TransformContext ctx)
    {
        var raw = ReadStringLiteralRaw(ctx);
        ctx.Output.Append(raw);
    }

    private static void CopyLineComment(TransformContext ctx)
    {
        while (!ctx.AtEnd && ctx.Current != '\n')
        {
            ctx.Output.Append(ctx.Current);
            ctx.Pos++;
        }
    }

    private static void CopyBlockComment(TransformContext ctx)
    {
        ctx.Output.Append(ctx.Current); ctx.Pos++; // /
        ctx.Output.Append(ctx.Current); ctx.Pos++; // *
        while (!ctx.AtEnd && !(ctx.Current == '*' && ctx.Peek() == '/'))
        {
            ctx.Output.Append(ctx.Current);
            ctx.Pos++;
        }
        if (!ctx.AtEnd)
        {
            ctx.Output.Append('*'); ctx.Pos++;
            ctx.Output.Append('/'); ctx.Pos++;
        }
    }

    private static string EscapeJsString(string s) =>
        s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");

    /// <summary>
    /// Quote a property name if it contains hyphens or is a reserved word.
    /// Most React props are camelCase identifiers and don't need quoting.
    /// </summary>
    private static string QuotePropertyName(string name) =>
        name.Contains('-') ? $"'{name}'" : name;
}
