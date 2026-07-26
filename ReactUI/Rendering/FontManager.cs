using System;
using System.Collections.Generic;

namespace ReactUI.Rendering;

using UnityEngine;

public static class FontManager
{
    private static readonly Dictionary<(string family, int weight, int size), FontAtlas> _atlases = new();
    private static Font? _defaultFont;
    private static bool _initialized;

    // Default font size used when building atlases for measurement
    private const int DefaultAtlasSize = 32;

    public static void Initialize()
    {
        if (_initialized) return;
        _defaultFont = Font.GetDefault();
        _initialized = true;
    }

    /// <summary>
    /// Get or create a font atlas for the given font family, weight, and size.
    /// Falls back to Unity's default font.
    /// </summary>
    public static FontAtlas GetAtlas(string? fontFamily, int fontWeight = 400, int fontSize = DefaultAtlasSize)
    {
        if (!_initialized) Initialize();

        var key = (fontFamily ?? "default", fontWeight, fontSize);
        if (_atlases.TryGetValue(key, out var existing))
            return existing;

        Font? font = null;

        // Try loading the requested font family
        if (!string.IsNullOrEmpty(fontFamily) && fontFamily != "default")
        {
            // Try loading from system fonts
            var names = Font.GetOSInstalledFontNames();
            foreach (var name in names)
            {
                if (name.Contains(fontFamily!, System.StringComparison.OrdinalIgnoreCase))
                {
                    var paths = Font.GetPathsToOSFonts();
                    for (int i = 0; i < names.Length && i < paths.Length; i++)
                    {
                        if (names[i] == name)
                        {
                            font = new Font(paths[i]);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        // Fallback to Unity default
        font ??= _defaultFont ?? Font.GetDefault();

        var atlas = FontAtlas.BuildFromUnityFont(font, fontSize);
        _atlases[key] = atlas;
        return atlas;
    }

    /// <summary>
    /// Get glyph metrics for a character at a specific font size.
    /// Metrics are scaled from the atlas size to the requested size.
    /// </summary>
    public static GlyphMetrics GetGlyph(char c, float fontSize, string? fontFamily = null, int fontWeight = 400)
    {
        // Use the atlas size closest to the requested size (clamped to reasonable range)
        int atlasSize = Math.Clamp((int)fontSize, 8, 128);
        var atlas = GetAtlas(fontFamily, fontWeight, atlasSize);

        if (atlas.Glyphs.TryGetValue(c, out var metrics))
            return metrics;

        // Fallback: return a space-like glyph
        return new GlyphMetrics
        {
            Advance = fontSize * 0.5f,
            BearingX = 0,
            BearingY = fontSize * 0.8f,
            Width = fontSize * 0.5f,
            Height = fontSize,
            UVRect = Core.Rect.Zero,
        };
    }

    /// <summary>
    /// Measure the width of a string at the given font size.
    /// Does not account for line wrapping.
    /// </summary>
    public static float MeasureString(string text, float fontSize, string? fontFamily = null, int fontWeight = 400)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        float width = 0;
        foreach (char c in text)
        {
            var glyph = GetGlyph(c, fontSize, fontFamily, fontWeight);
            width += glyph.Advance;
        }
        return width;
    }

    /// <summary>
    /// Get the line height for a given font size.
    /// </summary>
    public static float GetLineHeight(float fontSize, string? fontFamily = null, int fontWeight = 400)
    {
        int atlasSize = Math.Clamp((int)fontSize, 8, 128);
        var atlas = GetAtlas(fontFamily, fontWeight, atlasSize);
        return atlas.LineHeight > 0 ? atlas.LineHeight : fontSize * 1.2f;
    }

    /// <summary>
    /// Get the ascent (baseline distance from the line top) for a given font size.
    /// </summary>
    public static float GetAscent(float fontSize, string? fontFamily = null, int fontWeight = 400)
    {
        int atlasSize = Math.Clamp((int)fontSize, 8, 128);
        var atlas = GetAtlas(fontFamily, fontWeight, atlasSize);
        return atlas.Ascent > 0 ? atlas.Ascent : fontSize * 0.8f;
    }

    public static void ClearCache() => _atlases.Clear();
}
