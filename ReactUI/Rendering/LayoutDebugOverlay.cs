using UnityEngine;

namespace ReactUI.Rendering;

/// <summary>
/// Draws browser-style layout debug overlay: wireframe outlines around every element's
/// ScreenRect showing content box, padding, and margin. Toggle with F11.
/// </summary>
public static class LayoutDebugOverlay
{
    static Material? _lineMaterial;
    static bool _enabled;
    public static bool Enabled => _enabled;

    public static void Toggle()
    {
        _enabled = !_enabled;
        Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] Layout debug overlay: {_enabled}");
    }

    public static void Draw(Core.UINode root)
    {
        if (!_enabled || root == null) return;

        EnsureMaterial();
        _lineMaterial!.SetPass(0);

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

        DrawNodeRecursive(root, 0);

        GL.PopMatrix();
    }

    static void DrawNodeRecursive(Core.UINode node, int depth)
    {
        var rect = node.ScreenRect;
        if (rect.Width <= 0 || rect.Height <= 0) return;

        var style = node.ComputedStyle;

        // Content box (blue)
        float padT = 0, padR = 0, padB = 0, padL = 0;
        if (style.Padding.HasValue)
        {
            var p = style.Padding.Value;
            padT = p.Top; padR = p.Right; padB = p.Bottom; padL = p.Left;
        }

        // Margin
        float mT = 0, mR = 0, mB = 0, mL = 0;
        if (style.Margin.HasValue)
        {
            var m = style.Margin.Value;
            mT = m.Top; mR = m.Right; mB = m.Bottom; mL = m.Left;
        }

        // Element box = ScreenRect (includes padding, excludes margin)
        // Content box = element box inset by padding
        var contentRect = new Core.Rect(
            rect.X + padL, rect.Y + padT,
            rect.Width - padL - padR, rect.Height - padT - padB);

        // Margin box = element box expanded by margin
        var marginRect = new Core.Rect(
            rect.X - mL, rect.Y - mT,
            rect.Width + mL + mR, rect.Height + mT + mB);

        // Draw margin area (orange tint)
        if (mT > 0 || mR > 0 || mB > 0 || mL > 0)
            DrawRectOutline(marginRect, new Color(1f, 0.6f, 0.2f, 0.4f));

        // Draw padding area (green tint) — the element box outline
        if (padT > 0 || padR > 0 || padB > 0 || padL > 0)
        {
            DrawRectOutline(rect, new Color(0.3f, 0.9f, 0.3f, 0.5f));
            // Fill padding areas with translucent green
            DrawRectFill(new Core.Rect(rect.X, rect.Y, rect.Width, padT), new Color(0.3f, 0.9f, 0.3f, 0.08f)); // top
            DrawRectFill(new Core.Rect(rect.X, rect.Bottom - padB, rect.Width, padB), new Color(0.3f, 0.9f, 0.3f, 0.08f)); // bottom
            DrawRectFill(new Core.Rect(rect.X, rect.Y + padT, padL, rect.Height - padT - padB), new Color(0.3f, 0.9f, 0.3f, 0.08f)); // left
            DrawRectFill(new Core.Rect(rect.Right - padR, rect.Y + padT, padR, rect.Height - padT - padB), new Color(0.3f, 0.9f, 0.3f, 0.08f)); // right
        }

        // Draw content box (blue)
        if (contentRect.Width > 0 && contentRect.Height > 0)
            DrawRectOutline(contentRect, new Color(0.4f, 0.6f, 1f, 0.6f));

        // Draw element type label at top-left
        if (node.Type != "__component")
        {
            GL.PopMatrix();
            var labelStyle = new GUIStyle();
            labelStyle.fontSize = 9;
            labelStyle.normal.textColor = new Color(1f, 1f, 0f, 0.8f);
            labelStyle.normal.background = MakeTex(1, 1, new Color(0, 0, 0, 0.5f));
            string label = node.Type;
            if (node.Type == "text" && node.LastVNode?.TextContent != null)
            {
                string t = node.LastVNode.TextContent;
                if (t.Length > 12) t = t.Substring(0, 12) + "..";
                label = $"\"{t}\"";
            }
            GUI.Label(new Rect(rect.X, rect.Y - 12, 200, 14), label, labelStyle);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
            _lineMaterial!.SetPass(0);
        }

        // Recurse children
        for (int i = 0; i < node.Children.Count; i++)
            DrawNodeRecursive(node.Children[i], depth + 1);
    }

    static void DrawRectOutline(Core.Rect r, Color color)
    {
        GL.Begin(1); // GL.LINES
        GL.Color(color);
        // Top
        GL.Vertex3(r.X, r.Y, 0); GL.Vertex3(r.Right, r.Y, 0);
        // Right
        GL.Vertex3(r.Right, r.Y, 0); GL.Vertex3(r.Right, r.Bottom, 0);
        // Bottom
        GL.Vertex3(r.Right, r.Bottom, 0); GL.Vertex3(r.X, r.Bottom, 0);
        // Left
        GL.Vertex3(r.X, r.Bottom, 0); GL.Vertex3(r.X, r.Y, 0);
        GL.End();
    }

    static void DrawRectFill(Core.Rect r, Color color)
    {
        if (r.Width <= 0 || r.Height <= 0) return;
        GL.Begin(7); // GL.QUADS
        GL.Color(color);
        GL.Vertex3(r.X, r.Y, 0);
        GL.Vertex3(r.Right, r.Y, 0);
        GL.Vertex3(r.Right, r.Bottom, 0);
        GL.Vertex3(r.X, r.Bottom, 0);
        GL.End();
    }

    static void EnsureMaterial()
    {
        if (_lineMaterial != null) return;
        var shader = Shader.Find("Hidden/Internal-Colored");
        _lineMaterial = new Material(shader!);
        _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        _lineMaterial.SetInt("_ZWrite", 0);
    }

    static Texture2D? _bgTex;
    static Texture2D MakeTex(int w, int h, Color col)
    {
        if (_bgTex != null) return _bgTex;
        _bgTex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        _bgTex.SetPixels(pixels);
        _bgTex.Apply();
        return _bgTex;
    }
}
