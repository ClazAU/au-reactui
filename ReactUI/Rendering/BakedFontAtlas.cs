using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReactUI.Rendering;

/// <summary>
/// Bakes OS font glyphs into a texture atlas at runtime using GUI.Label,
/// then provides GL quad rendering for text. Avoids stripped Font API methods.
/// </summary>
public class BakedFontAtlas
{
    const string Charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    public Texture2D Atlas { get; private set; } = null!;
    public int FontSize { get; }
    public bool IsBold { get; }

    readonly GlyphInfo[] _glyphs = new GlyphInfo[128]; // ASCII range
    Material? _material;

    public struct GlyphInfo
    {
        public float U0, V0, U1, V1;
        public float Width, Height;
        public float Advance;
        public bool Valid;
    }

    BakedFontAtlas(int fontSize, bool bold)
    {
        FontSize = fontSize;
        IsBold = bold;
    }

    // ── Cache of baked atlases by (fontSize, bold) ──────────────────

    static readonly Dictionary<(int size, bool bold), BakedFontAtlas> _cache = new();

    public static BakedFontAtlas GetOrCreate(int fontSize, bool bold)
    {
        var key = (fontSize, bold);
        if (_cache.TryGetValue(key, out var existing))
            return existing;

        var atlas = new BakedFontAtlas(fontSize, bold);
        atlas.Bake();
        _cache[key] = atlas;
        return atlas;
    }

    // ── Bake glyphs into atlas ──────────────────────────────────────

    void Bake()
    {
        var style = new GUIStyle();
        style.fontSize = FontSize;
        style.normal.textColor = Color.white;
        style.fontStyle = IsBold ? FontStyle.Bold : FontStyle.Normal;
        style.alignment = TextAnchor.UpperLeft;

        // Measure all characters to determine cell and atlas size
        float maxW = 0, maxH = 0;
        var sizes = new Vector2[Charset.Length];
        for (int i = 0; i < Charset.Length; i++)
        {
            var s = style.CalcSize(new GUIContent(Charset[i].ToString()));
            sizes[i] = s;
            if (s.x > maxW) maxW = s.x;
            if (s.y > maxH) maxH = s.y;
        }

        int cellW = Mathf.CeilToInt(maxW) + 2; // 1px padding each side
        int cellH = Mathf.CeilToInt(maxH) + 2;
        int cols = Mathf.CeilToInt(Mathf.Sqrt(Charset.Length));
        int rows = (Charset.Length + cols - 1) / cols;
        int atlasW = cols * cellW;
        int atlasH = rows * cellH;

        // Render to RenderTexture
        var rt = RenderTexture.GetTemporary(atlasW, atlasH, 0, RenderTextureFormat.ARGB32);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);

        for (int i = 0; i < Charset.Length; i++)
        {
            char c = Charset[i];
            int col = i % cols;
            int row = i / cols;
            float x = col * cellW + 1; // 1px left padding
            float y = row * cellH + 1;

            GUI.Label(new Rect(x, y, cellW, cellH), c.ToString(), style);

            if (c < 128)
            {
                _glyphs[c] = new GlyphInfo
                {
                    U0 = x / atlasW,
                    V0 = 1f - (y + sizes[i].y) / atlasH,
                    U1 = (x + sizes[i].x) / atlasW,
                    V1 = 1f - y / atlasH,
                    Width = sizes[i].x,
                    Height = sizes[i].y,
                    Advance = sizes[i].x,
                    Valid = true,
                };
            }
        }

        // Read into Texture2D
        Atlas = new Texture2D(atlasW, atlasH, TextureFormat.ARGB32, false);
        Atlas.filterMode = FilterMode.Bilinear;
        Atlas.ReadPixels(new Rect(0, 0, atlasW, atlasH), 0, 0);
        Atlas.Apply();

        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        Plugin.ReactUIPlugin.Logger.LogInfo(
            $"[ReactUI] BakedFontAtlas: size={FontSize} bold={IsBold} atlas={atlasW}x{atlasH} chars={Charset.Length}");
    }

    // ── GL text rendering ───────────────────────────────────────────

    Material EnsureMaterial()
    {
        if (_material != null) return _material;
        // Simple alpha-blended texture shader for bitmap font atlas
        var shader = Shader.Find("GUI/Text Shader")
                  ?? Shader.Find("Sprites/Default")
                  ?? Shader.Find("UI/Default");
        _material = new Material(shader!);
        _material.hideFlags = HideFlags.HideAndDontSave;
        return _material;
    }

    /// <summary>
    /// Draw text at the given position using GL quads. Must be called within GL.PushMatrix/PopMatrix.
    /// </summary>
    public void DrawText(string text, float x, float y, float scale, Color color, float opacity)
    {
        if (Atlas == null || string.IsNullOrEmpty(text)) return;

        var mat = EnsureMaterial();
        mat.mainTexture = Atlas;
        mat.SetPass(0);

        GL.Begin(7); // GL.QUADS
        GL.Color(new Color(color.r, color.g, color.b, color.a * opacity));

        float cx = x;
        foreach (char c in text)
        {
            if (c >= 128 || !_glyphs[c].Valid)
            {
                cx += FontSize * 0.5f * scale;
                continue;
            }

            var g = _glyphs[c];
            float w = g.Width * scale;
            float h = g.Height * scale;

            // Quad: top-left origin (matching GL.LoadPixelMatrix(0, screenW, screenH, 0))
            GL.TexCoord2(g.U0, g.V1); GL.Vertex3(cx, y, 0);         // top-left
            GL.TexCoord2(g.U1, g.V1); GL.Vertex3(cx + w, y, 0);     // top-right
            GL.TexCoord2(g.U1, g.V0); GL.Vertex3(cx + w, y + h, 0); // bottom-right
            GL.TexCoord2(g.U0, g.V0); GL.Vertex3(cx, y + h, 0);     // bottom-left

            cx += g.Advance * scale;
        }

        GL.End();
    }

    /// <summary>
    /// Measure text width at the given scale.
    /// </summary>
    public float MeasureWidth(string text, float scale)
    {
        float w = 0;
        foreach (char c in text)
        {
            if (c >= 128 || !_glyphs[c].Valid)
                w += FontSize * 0.5f * scale;
            else
                w += _glyphs[c].Advance * scale;
        }
        return w;
    }
}
