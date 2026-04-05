using System.Collections.Generic;

namespace ReactUI.Rendering;

using UnityEngine;

public class FontAtlas
{
    public Texture2D? AtlasTexture;
    public Dictionary<char, GlyphMetrics> Glyphs = new();
    public float LineHeight;
    public float Ascent;
    public float Descent;
    public float SpaceAdvance;

    /// <summary>
    /// Build a font atlas from a Unity Font at a given size.
    /// Requests character info for printable ASCII + common characters,
    /// then copies the font texture data into our own atlas texture.
    /// </summary>
    public static FontAtlas BuildFromUnityFont(Font font, int fontSize)
    {
        var atlas = new FontAtlas();

        // Request characters to ensure they're in the font texture
        var chars = new List<char>();
        for (char c = ' '; c <= '~'; c++)
            chars.Add(c);
        // Add some common extended characters
        chars.AddRange(new[] { '\u00A0', '\u2013', '\u2014', '\u2018', '\u2019', '\u201C', '\u201D', '\u2026' });

        font.RequestCharactersInTexture(new string(chars.ToArray()), fontSize, FontStyle.Normal);

        // Extract glyph metrics from the Unity font
        foreach (char c in chars)
        {
            if (font.GetCharacterInfo(c, out CharacterInfo info, fontSize, FontStyle.Normal))
            {
                atlas.Glyphs[c] = new GlyphMetrics
                {
                    Advance = info.advance,
                    BearingX = info.minX,
                    BearingY = info.maxY,
                    Width = info.glyphWidth,
                    Height = info.glyphHeight,
                    UVRect = new Core.Rect(
                        info.uvBottomLeft.x,
                        info.uvBottomLeft.y,
                        info.uvTopRight.x - info.uvBottomLeft.x,
                        info.uvTopRight.y - info.uvBottomLeft.y
                    ),
                };
            }
        }

        atlas.AtlasTexture = font.material?.mainTexture as Texture2D;
        atlas.LineHeight = font.lineHeight;
        atlas.Ascent = font.ascent;
        atlas.Descent = atlas.LineHeight - atlas.Ascent;
        atlas.SpaceAdvance = atlas.Glyphs.ContainsKey(' ') ? atlas.Glyphs[' '].Advance : fontSize * 0.25f;

        return atlas;
    }
}
