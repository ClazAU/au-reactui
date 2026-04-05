using System.Collections.Generic;

namespace ReactUI.Rendering;

public static class TextRenderer
{
    /// <summary>
    /// Measure text dimensions with word wrapping within maxWidth.
    /// Returns the bounding width and height of the text block.
    /// </summary>
    public static (float width, float height) Measure(
        string text, float fontSize, float maxWidth,
        float lineHeight = 0, string? fontFamily = null, int fontWeight = 400)
    {
        if (string.IsNullOrEmpty(text))
            return (0, 0);

        if (lineHeight <= 0)
            lineHeight = FontManager.GetLineHeight(fontSize, fontFamily, fontWeight);

        var lines = WrapLines(text, fontSize, maxWidth, fontFamily, fontWeight);

        float maxLineWidth = 0;
        foreach (var line in lines)
        {
            float w = MeasureLine(line, fontSize, fontFamily, fontWeight);
            if (w > maxLineWidth) maxLineWidth = w;
        }

        float totalHeight = lines.Count * lineHeight;
        return (maxLineWidth, totalHeight);
    }

    /// <summary>
    /// Generate positioned glyph quads for rendering.
    /// Performs word wrapping and alignment within the given bounds.
    /// </summary>
    public static List<GlyphQuad> Layout(
        string text, float fontSize, float maxWidth,
        Style.TextAlign align, float lineHeight,
        Core.Rect bounds, Style.UIColor color,
        string? fontFamily = null, int fontWeight = 400)
    {
        var quads = new List<GlyphQuad>();

        if (string.IsNullOrEmpty(text))
            return quads;

        if (lineHeight <= 0)
            lineHeight = FontManager.GetLineHeight(fontSize, fontFamily, fontWeight);

        var lines = WrapLines(text, fontSize, maxWidth, fontFamily, fontWeight);

        float cursorY = bounds.Y;

        for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
        {
            var line = lines[lineIdx];
            float lineWidth = MeasureLine(line, fontSize, fontFamily, fontWeight);

            // Compute horizontal offset based on alignment
            float offsetX = align switch
            {
                Style.TextAlign.Center => bounds.X + (bounds.Width - lineWidth) * 0.5f,
                Style.TextAlign.Right => bounds.X + bounds.Width - lineWidth,
                Style.TextAlign.Justify => bounds.X, // justify handled separately
                _ => bounds.X,
            };

            // For justified text (not last line), compute extra spacing
            float extraSpace = 0;
            int spaceCount = 0;
            if (align == Style.TextAlign.Justify && lineIdx < lines.Count - 1)
            {
                foreach (char c in line)
                    if (c == ' ') spaceCount++;
                if (spaceCount > 0)
                    extraSpace = (bounds.Width - lineWidth) / spaceCount;
            }

            float cursorX = offsetX;

            foreach (char c in line)
            {
                var glyph = FontManager.GetGlyph(c, fontSize, fontFamily, fontWeight);

                if (c != ' ')
                {
                    float glyphX = cursorX + glyph.BearingX;
                    float glyphY = cursorY + (lineHeight - glyph.BearingY);

                    quads.Add(new GlyphQuad
                    {
                        Rect = new Core.Rect(glyphX, glyphY, glyph.Width, glyph.Height),
                        UVRect = glyph.UVRect,
                        Color = color,
                    });
                }

                cursorX += glyph.Advance;

                // Add extra space for justified text
                if (c == ' ' && extraSpace > 0)
                    cursorX += extraSpace;
            }

            cursorY += lineHeight;
        }

        return quads;
    }

    /// <summary>
    /// Word-wrap text into lines that fit within maxWidth.
    /// </summary>
    private static List<string> WrapLines(
        string text, float fontSize, float maxWidth,
        string? fontFamily, int fontWeight)
    {
        var lines = new List<string>();

        // First split by explicit newlines
        var paragraphs = text.Split('\n');

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                lines.Add("");
                continue;
            }

            if (maxWidth <= 0 || float.IsInfinity(maxWidth) || float.IsNaN(maxWidth))
            {
                lines.Add(paragraph);
                continue;
            }

            var words = paragraph.Split(' ');
            var currentLine = "";
            float currentWidth = 0;
            float spaceWidth = FontManager.GetGlyph(' ', fontSize, fontFamily, fontWeight).Advance;

            foreach (var word in words)
            {
                float wordWidth = MeasureLine(word, fontSize, fontFamily, fontWeight);

                if (currentLine.Length == 0)
                {
                    // First word on the line always goes on, even if it overflows
                    currentLine = word;
                    currentWidth = wordWidth;
                }
                else if (currentWidth + spaceWidth + wordWidth <= maxWidth)
                {
                    // Fits on current line
                    currentLine += " " + word;
                    currentWidth += spaceWidth + wordWidth;
                }
                else
                {
                    // Doesn't fit; start a new line
                    lines.Add(currentLine);
                    currentLine = word;
                    currentWidth = wordWidth;
                }
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine);
        }

        return lines;
    }

    /// <summary>
    /// Measure the pixel width of a single line of text (no wrapping).
    /// </summary>
    private static float MeasureLine(string line, float fontSize, string? fontFamily, int fontWeight)
    {
        float w = 0;
        foreach (char c in line)
        {
            var glyph = FontManager.GetGlyph(c, fontSize, fontFamily, fontWeight);
            w += glyph.Advance;
        }
        return w;
    }
}
