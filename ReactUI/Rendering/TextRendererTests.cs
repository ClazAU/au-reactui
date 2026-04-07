using System;
using UnityEngine;

namespace ReactUI.Rendering;

/// <summary>
/// In-game diagnostic: tests 3 GL text rendering approaches to find what survives IL2CPP stripping.
/// Call RunAll() from OnGUI during Repaint to execute tests and render results.
/// Results are logged to BepInEx and drawn on screen.
/// </summary>
public static class TextRendererTests
{
    static bool _ran;
    static string _result1 = "pending";
    static string _result2 = "pending";
    static string _result3 = "pending";
    static Texture2D? _bakedAtlas;
    static CharInfo[]? _bakedChars;
    static Material? _glMaterial;

    // Baked atlas character info
    struct CharInfo
    {
        public float U0, V0, U1, V1; // UV coords in atlas
        public float Width, Height;
        public float Advance;
    }

    /// <summary>
    /// Run all 3 tests and render results at the given Y offset. Call from OnGUI Repaint.
    /// </summary>
    public static void RunAll(float yOffset = 10)
    {
        if (!_ran)
        {
            _ran = true;
            Test1_FontGetCharacterInfo();
            Test2_BakedBitmapAtlas();
            Test3_EmbeddedSDFAtlas();
        }

        // Draw test results and sample text
        float y = yOffset;
        y = DrawTestResult("Test 1: Font.GetCharacterInfo()", _result1, y);
        if (_result1 == "OK")
            y = DrawGLText_FontAPI("Hello from Font API!", 20, y);

        y = DrawTestResult("Test 2: Baked Bitmap Atlas", _result2, y + 4);
        if (_result2 == "OK" && _bakedAtlas != null)
            y = DrawGLText_BakedAtlas("Hello from Baked Atlas!", 20, y);

        y = DrawTestResult("Test 3: SDF Atlas (stub)", _result3, y + 4);
    }

    // ── Test 1: Font.GetCharacterInfo ────────────────────────────────

    static void Test1_FontGetCharacterInfo()
    {
        try
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            if (font == null)
            {
                _result1 = "FAIL: CreateDynamicFontFromOSFont returned null";
                Log(_result1);
                return;
            }

            // Request characters
            font.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 :+-!", 24);

            // Try GetCharacterInfo — this is what was stripped before
            CharacterInfo ci;
            bool got = font.GetCharacterInfo('A', out ci, 24);
            if (!got)
            {
                _result1 = "FAIL: GetCharacterInfo returned false";
                Log(_result1);
                return;
            }

            // Try reading properties that may be stripped
            try
            {
                float advance = ci.advance;
                var uv = ci.uvBottomLeft;
                int minX = ci.minX;
                int maxX = ci.maxX;
                int minY = ci.minY;
                int maxY = ci.maxY;
                _result1 = $"OK (advance={advance:F1}, uv={uv}, minX={minX}, maxX={maxX})";
            }
            catch (Exception ex)
            {
                _result1 = $"FAIL: property access threw: {ex.GetType().Name}: {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            _result1 = $"FAIL: {ex.GetType().Name}: {ex.Message}";
        }
        Log("Test1 FontGetCharacterInfo: " + _result1);
    }

    // ── Test 2: Baked Bitmap Atlas ───────────────────────────────────

    static void Test2_BakedBitmapAtlas()
    {
        try
        {
            // Characters to bake
            string charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            int fontSize = 32;
            int cellW = (int)(fontSize * 0.7f); // estimated max char width
            int cellH = fontSize + 4;
            int cols = 16;
            int rows = (charset.Length + cols - 1) / cols;
            int atlasW = cols * cellW;
            int atlasH = rows * cellH;

            // Create a RenderTexture, render characters with GUI.Label
            var rt = RenderTexture.GetTemporary(atlasW, atlasH, 0, RenderTextureFormat.ARGB32);
            var prevRT = RenderTexture.active;
            RenderTexture.active = rt;

            GL.Clear(true, true, Color.clear);

            var style = new GUIStyle();
            style.fontSize = fontSize;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;
            style.clipping = TextClipping.Clip;

            _bakedChars = new CharInfo[128]; // ASCII range

            for (int i = 0; i < charset.Length; i++)
            {
                char c = charset[i];
                int col = i % cols;
                int row = i / cols;
                float x = col * cellW;
                float y = row * cellH;

                // Measure actual width
                var content = new GUIContent(c.ToString());
                var size = style.CalcSize(content);

                GUI.Label(new Rect(x, y, cellW, cellH), c.ToString(), style);

                if (c < 128)
                {
                    _bakedChars[c] = new CharInfo
                    {
                        U0 = x / atlasW,
                        V0 = 1f - (y + cellH) / atlasH, // flip Y for GL
                        U1 = (x + size.x) / atlasW,
                        V1 = 1f - y / atlasH,
                        Width = size.x,
                        Height = size.y,
                        Advance = size.x,
                    };
                }
            }

            // Read pixels into a Texture2D
            _bakedAtlas = new Texture2D(atlasW, atlasH, TextureFormat.ARGB32, false);
            _bakedAtlas.filterMode = FilterMode.Bilinear;
            _bakedAtlas.ReadPixels(new Rect(0, 0, atlasW, atlasH), 0, 0);
            _bakedAtlas.Apply();

            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);

            _result2 = $"OK (atlas {atlasW}x{atlasH}, {charset.Length} chars)";
        }
        catch (Exception ex)
        {
            _result2 = $"FAIL: {ex.GetType().Name}: {ex.Message}";
        }
        Log("Test2 BakedBitmapAtlas: " + _result2);
    }

    // ── Test 3: Embedded SDF Atlas (stub) ────────────────────────────

    static void Test3_EmbeddedSDFAtlas()
    {
        // This approach requires a pre-generated SDF font atlas shipped as an embedded resource.
        // For the test, we just verify we CAN load embedded resources and create textures from bytes.
        try
        {
            // Test: can we create a texture from raw bytes?
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            if (tex.width == 4 && tex.height == 4)
                _result3 = "OK (Texture2D from bytes works — needs actual SDF atlas asset to be useful)";
            else
                _result3 = "FAIL: texture creation produced unexpected size";

            UnityEngine.Object.Destroy(tex);
        }
        catch (Exception ex)
        {
            _result3 = $"FAIL: {ex.GetType().Name}: {ex.Message}";
        }
        Log("Test3 EmbeddedSDFAtlas: " + _result3);
    }

    // ── GL Drawing helpers ───────────────────────────────────────────

    static float DrawTestResult(string label, string result, float y)
    {
        bool ok = result.StartsWith("OK");
        var color = ok ? Color.green : (result == "pending" ? Color.yellow : Color.red);

        GL.PopMatrix();
        var s = new GUIStyle(GUI.skin.label);
        s.fontSize = 14;
        s.normal.textColor = color;
        s.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(10, y, 800, 20), $"{label}: {result}", s);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, UIScale.LogicalWidth, UIScale.LogicalHeight, 0);

        return y + 20;
    }

    /// <summary>
    /// Render text using Font API + GL quads. Only works if Test 1 passed.
    /// </summary>
    static float DrawGLText_FontAPI(string text, float fontSize, float y)
    {
        try
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", (int)fontSize);
            if (font == null) return y;

            font.RequestCharactersInTexture(text, (int)fontSize);

            EnsureGLMaterial();
            _glMaterial!.mainTexture = font.material.mainTexture;
            _glMaterial.SetPass(0);

            GL.Begin(7); // GL.QUADS
            GL.Color(new Color(0.2f, 0.8f, 1f, 1f));

            float x = 10;
            foreach (char c in text)
            {
                if (!font.GetCharacterInfo(c, out var ci, (int)fontSize)) continue;

                float x0 = x + ci.minX;
                float x1 = x + ci.maxX;
                float y0 = y - ci.maxY + fontSize;
                float y1 = y - ci.minY + fontSize;

                GL.TexCoord2(ci.uvBottomLeft.x, ci.uvBottomLeft.y);
                GL.Vertex3(x0, y1, 0);
                GL.TexCoord2(ci.uvBottomRight.x, ci.uvBottomRight.y);
                GL.Vertex3(x1, y1, 0);
                GL.TexCoord2(ci.uvTopRight.x, ci.uvTopRight.y);
                GL.Vertex3(x1, y0, 0);
                GL.TexCoord2(ci.uvTopLeft.x, ci.uvTopLeft.y);
                GL.Vertex3(x0, y0, 0);

                x += ci.advance;
            }

            GL.End();
        }
        catch (Exception ex)
        {
            Log($"DrawGLText_FontAPI error: {ex}");
        }

        return y + fontSize + 4;
    }

    /// <summary>
    /// Render text using the baked bitmap atlas + GL quads. Only works if Test 2 passed.
    /// </summary>
    static float DrawGLText_BakedAtlas(string text, float fontSize, float y)
    {
        if (_bakedAtlas == null || _bakedChars == null) return y;

        try
        {
            float scale = fontSize / 32f; // atlas was baked at 32px

            EnsureGLMaterial();
            _glMaterial!.mainTexture = _bakedAtlas;
            _glMaterial.SetPass(0);

            GL.Begin(7); // GL.QUADS
            GL.Color(Color.white);

            float x = 10;
            foreach (char c in text)
            {
                if (c >= 128 || _bakedChars[c].Advance == 0)
                {
                    x += fontSize * 0.5f;
                    continue;
                }

                var ci = _bakedChars[c];
                float w = ci.Width * scale;
                float h = ci.Height * scale;

                GL.TexCoord2(ci.U0, ci.V0);
                GL.Vertex3(x, y + h, 0);
                GL.TexCoord2(ci.U1, ci.V0);
                GL.Vertex3(x + w, y + h, 0);
                GL.TexCoord2(ci.U1, ci.V1);
                GL.Vertex3(x + w, y, 0);
                GL.TexCoord2(ci.U0, ci.V1);
                GL.Vertex3(x, y, 0);

                x += ci.Advance * scale;
            }

            GL.End();
        }
        catch (Exception ex)
        {
            Log($"DrawGLText_BakedAtlas error: {ex}");
        }

        return y + fontSize + 4;
    }

    static void EnsureGLMaterial()
    {
        if (_glMaterial != null) return;
        var shader = Shader.Find("GUI/Text Shader");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        _glMaterial = new Material(shader!);
    }

    static void Log(string msg) => Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI:TextTest] {msg}");

    /// <summary>
    /// Reset state so tests can re-run (e.g. after scene change).
    /// </summary>
    public static void Reset()
    {
        _ran = false;
        _result1 = "pending";
        _result2 = "pending";
        _result3 = "pending";
        if (_bakedAtlas != null) UnityEngine.Object.Destroy(_bakedAtlas);
        _bakedAtlas = null;
        _bakedChars = null;
    }
}
