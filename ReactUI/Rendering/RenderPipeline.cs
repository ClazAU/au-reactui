using System;
using System.Collections.Generic;

namespace ReactUI.Rendering;

using UnityEngine;

/// <summary>
/// Orchestrates the full render pass: traverses the committed UI tree to build
/// DrawCommands, sorts by Z-order, then executes them using GL immediate mode.
/// Call Initialize() once, then BuildDrawCommands() + Execute() each frame from OnGUI.
/// </summary>
public class RenderPipeline
{
    private readonly List<DrawCommand> _commands = new();
    private readonly MeshBuilder _meshBuilder = new();
    private ClipStack _clipStack = new();

    private Material? _sdfRectMaterial;
    private Material? _sdfTextMaterial;
    private Material? _imageMaterial;
    private Material? _fallbackMaterial;

    private bool _useSdfShaders;

    // Reusable 1x1 solid white texture for fallback colored rect rendering
    private static Texture2D? _whiteTexture;

    public void Initialize()
    {
        ShaderCache.Initialize();
        FontManager.Initialize();

        _sdfRectMaterial = ShaderCache.GetMaterial("ReactUI/SDFRect");
        _sdfTextMaterial = ShaderCache.GetMaterial("ReactUI/SDFText");
        _imageMaterial = ShaderCache.GetMaterial("ReactUI/Image");
        _fallbackMaterial = ShaderCache.GetFallbackMaterial();

        _useSdfShaders = ShaderCache.HasShader("ReactUI/SDFRect");

        if (_whiteTexture == null)
        {
            _whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
            _whiteTexture.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    /// <summary>
    /// Build draw commands from the committed UI tree.
    /// Call once per frame before Execute().
    /// </summary>
    public void BuildDrawCommands(Core.UINode root)
    {
        _commands.Clear();
        _clipStack = new ClipStack();
        TraverseTree(root, 0);
        _commands.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
    }

    private void TraverseTree(Core.UINode node, int depth)
    {
        var style = node.ComputedStyle;
        var rect = node.ScreenRect;

        // Skip zero-size nodes
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        // Resolve border radii
        float radiusTL, radiusTR, radiusBR, radiusBL;
        if (style.BorderRadii.HasValue)
        {
            var r = style.BorderRadii.Value;
            radiusTL = r.TopLeft;
            radiusTR = r.TopRight;
            radiusBR = r.BottomRight;
            radiusBL = r.BottomLeft;
        }
        else
        {
            float r = style.BorderRadius ?? 0;
            radiusTL = radiusTR = radiusBR = radiusBL = r;
        }

        float opacity = style.Opacity ?? 1f;
        int zOrder = style.ZIndex ?? depth;

        // Push clip rect if overflow is hidden or scroll
        bool pushClip = style.Overflow == Style.Overflow.Hidden
                     || style.Overflow == Style.Overflow.Scroll;
        if (pushClip)
            _clipStack.Push(rect);

        var clipRect = _clipStack.Current;

        // Backdrop blur pass (rendered before the element itself)
        float backdropBlur = style.BackdropBlur ?? 0;
        if (backdropBlur > 0)
        {
            _commands.Add(new DrawCommand
            {
                Type = DrawType.BlurRect,
                Rect = rect,
                ClipRect = clipRect,
                ZOrder = zOrder,
                BackdropBlur = backdropBlur,
                BorderRadiusTL = radiusTL,
                BorderRadiusTR = radiusTR,
                BorderRadiusBR = radiusBR,
                BorderRadiusBL = radiusBL,
                Opacity = opacity,
            });
        }

        // Background / box shadow / border — emit SdfRect command
        bool hasBackground = style.Background.HasValue && style.Background.Value.A > 0;
        bool hasGradient = style.BackgroundGradient.HasValue
                        && style.BackgroundGradient.Value.Type != Style.GradientType.None;
        bool hasBorder = (style.BorderWidth ?? 0) > 0
                      && style.BorderColor.HasValue
                      && style.BorderColor.Value.A > 0;
        bool hasShadow = style.BoxShadow.HasValue && style.BoxShadow.Value.Blur > 0;
        bool hasRadius = radiusTL > 0 || radiusTR > 0 || radiusBR > 0 || radiusBL > 0;

        if (hasBackground || hasGradient || hasBorder || hasShadow || hasRadius)
        {
            // For shadows, expand the rect to accommodate blur + spread + offset
            var cmdRect = rect;
            if (hasShadow)
            {
                var s = style.BoxShadow!.Value;
                float expand = s.Blur + s.Spread;
                cmdRect = new Core.Rect(
                    rect.X + s.OffsetX - expand,
                    rect.Y + s.OffsetY - expand,
                    rect.Width + expand * 2,
                    rect.Height + expand * 2
                );
            }

            _commands.Add(new DrawCommand
            {
                Type = DrawType.SdfRect,
                Rect = hasShadow ? cmdRect : rect,
                ClipRect = clipRect,
                ZOrder = zOrder,
                BackgroundColor = style.Background ?? Style.UIColor.Transparent,
                Gradient = hasGradient ? style.BackgroundGradient : null,
                BorderRadiusTL = radiusTL,
                BorderRadiusTR = radiusTR,
                BorderRadiusBR = radiusBR,
                BorderRadiusBL = radiusBL,
                BorderColor = style.BorderColor ?? Style.UIColor.Transparent,
                BorderWidth = style.BorderWidth ?? 0,
                Shadow = hasShadow ? style.BoxShadow : null,
                Opacity = opacity,
            });
        }

        // Text content
        if (node.Type == "text" && !string.IsNullOrEmpty(node.LastVNode?.TextContent))
        {
            _commands.Add(new DrawCommand
            {
                Type = DrawType.Text,
                Rect = rect,
                ClipRect = clipRect,
                ZOrder = zOrder + 1, // text renders above background
                Text = node.LastVNode.TextContent,
                TextColor = style.Color ?? Style.UIColor.White,
                FontSize = style.FontSize ?? 14f,
                FontWeight = style.FontWeight ?? 400,
                TextAlign = style.TextAlign ?? Style.TextAlign.Left,
                LineHeight = style.LineHeight ?? 0,
                Opacity = opacity,
            });
        }

        // Image content
        if (node.Type == "image" && node.LastVNode?.Props != null)
        {
            Texture2D? tex = null;
            if (node.LastVNode.Props.TryGetValue("texture", out var texObj) && texObj is Texture2D t)
                tex = t;

            if (tex != null)
            {
                _commands.Add(new DrawCommand
                {
                    Type = DrawType.Image,
                    Rect = rect,
                    ClipRect = clipRect,
                    ZOrder = zOrder + 1,
                    Texture = tex,
                    ObjectFit = style.ObjectFit ?? Style.ObjectFit.Fill,
                    BorderRadiusTL = radiusTL,
                    BorderRadiusTR = radiusTR,
                    BorderRadiusBR = radiusBR,
                    BorderRadiusBL = radiusBL,
                    Opacity = opacity,
                });
            }
        }

        // Recurse children
        for (int i = 0; i < node.Children.Count; i++)
        {
            TraverseTree(node.Children[i], depth + i + 1);
        }

        if (pushClip)
            _clipStack.Pop();
    }

    /// <summary>
    /// Execute all queued draw commands. Must be called during OnGUI Repaint event.
    /// Uses GL immediate mode for maximum compatibility with IL2CPP builds.
    /// </summary>
    public void Execute()
    {
        if (_commands.Count == 0) return;

        GL.PushMatrix();
        // Orthographic projection: pixel coordinates, origin at top-left
        GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

        foreach (var cmd in _commands)
        {
            // Skip commands with zero-area clip rects
            if (cmd.ClipRect.Width <= 0 || cmd.ClipRect.Height <= 0)
                continue;

            switch (cmd.Type)
            {
                case DrawType.SdfRect:
                    if (_useSdfShaders)
                        DrawSdfRect(cmd);
                    else
                        DrawFallbackRect(cmd);
                    break;

                case DrawType.BlurRect:
                    DrawBlurRect(cmd);
                    break;

                case DrawType.Text:
                    DrawText(cmd);
                    break;

                case DrawType.Image:
                    DrawImage(cmd);
                    break;
            }
        }

        GL.PopMatrix();
    }

    /// <summary>
    /// Draw a rounded rectangle using the SDF shader with full border/shadow/gradient support.
    /// </summary>
    private void DrawSdfRect(DrawCommand cmd)
    {
        if (_sdfRectMaterial == null) return;

        _sdfRectMaterial.SetPass(0);

        // Set shader uniforms
        var rect = cmd.Rect;
        _sdfRectMaterial.SetVector("_RectSize", new Vector4(rect.Width, rect.Height, 0, 0));
        _sdfRectMaterial.SetVector("_RectPos", new Vector4(rect.X, rect.Y, 0, 0));
        _sdfRectMaterial.SetVector("_Radii", new Vector4(
            cmd.BorderRadiusTL, cmd.BorderRadiusTR, cmd.BorderRadiusBR, cmd.BorderRadiusBL));
        _sdfRectMaterial.SetColor("_BgColor", cmd.BackgroundColor.ToUnityColor() * new Color(1, 1, 1, cmd.Opacity));
        _sdfRectMaterial.SetColor("_BorderColor", cmd.BorderColor.ToUnityColor() * new Color(1, 1, 1, cmd.Opacity));
        _sdfRectMaterial.SetFloat("_BorderWidth", cmd.BorderWidth);

        // Gradient
        if (cmd.Gradient.HasValue && cmd.Gradient.Value.Type != Style.GradientType.None)
        {
            var g = cmd.Gradient.Value;
            _sdfRectMaterial.SetFloat("_GradientEnabled", 1);
            _sdfRectMaterial.SetFloat("_GradientAngle", g.Angle * Mathf.Deg2Rad);
            _sdfRectMaterial.SetColor("_GradientColorA", g.ColorA.ToUnityColor());
            _sdfRectMaterial.SetColor("_GradientColorB", g.ColorB.ToUnityColor());
            _sdfRectMaterial.SetFloat("_GradientRadial", g.Type == Style.GradientType.Radial ? 1 : 0);
        }
        else
        {
            _sdfRectMaterial.SetFloat("_GradientEnabled", 0);
        }

        // Box shadow
        if (cmd.Shadow.HasValue)
        {
            var s = cmd.Shadow.Value;
            _sdfRectMaterial.SetFloat("_ShadowEnabled", 1);
            _sdfRectMaterial.SetVector("_ShadowOffset", new Vector4(s.OffsetX, s.OffsetY, 0, 0));
            _sdfRectMaterial.SetFloat("_ShadowBlur", s.Blur);
            _sdfRectMaterial.SetFloat("_ShadowSpread", s.Spread);
            _sdfRectMaterial.SetColor("_ShadowColor", s.Color.ToUnityColor() * new Color(1, 1, 1, cmd.Opacity));
            _sdfRectMaterial.SetFloat("_ShadowInset", s.Inset ? 1 : 0);
        }
        else
        {
            _sdfRectMaterial.SetFloat("_ShadowEnabled", 0);
        }

        // Draw quad
        DrawGLQuad(cmd.Rect);
    }

    /// <summary>
    /// Fallback renderer: generates an SDF texture on CPU (cached) and draws via GUI.DrawTexture.
    /// Supports rounded corners, borders, box shadows, gradients, and opacity.
    /// </summary>
    private void DrawFallbackRect(DrawCommand cmd)
    {
        int w = Mathf.CeilToInt(cmd.Rect.Width);
        int h = Mathf.CeilToInt(cmd.Rect.Height);
        if (w <= 0 || h <= 0) return;

        var generated = SdfTextureGenerator.GetOrCreate(
            w, h,
            cmd.BackgroundColor,
            cmd.Gradient,
            cmd.BorderRadiusTL, cmd.BorderRadiusTR, cmd.BorderRadiusBR, cmd.BorderRadiusBL,
            cmd.BorderColor, cmd.BorderWidth,
            cmd.Shadow,
            cmd.Opacity
        );

        if (generated == null || generated.Value.Texture == null) return;

        var tex = generated.Value;

        // Pop GL matrix to use GUI drawing, then push back after
        GL.PopMatrix();

        // Account for shadow padding — texture is larger than the element rect
        var drawRect = new UnityEngine.Rect(
            cmd.Rect.X - tex.PaddingLeft,
            cmd.Rect.Y - tex.PaddingTop,
            tex.Texture.width,
            tex.Texture.height
        );

        var style = new GUIStyle();
        style.normal.background = tex.Texture;
        GUI.Box(drawRect, GUIContent.none, style);

        // Re-push GL matrix for subsequent commands
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
    }

    /// <summary>
    /// Draw backdrop blur effect. Uses the Kawase blur shader if available,
    /// otherwise this is a no-op (backdrop blur requires render texture capture).
    /// </summary>
    private void DrawBlurRect(DrawCommand cmd)
    {
        // Backdrop blur requires capturing the current framebuffer into a RenderTexture,
        // applying blur passes, then drawing the blurred result masked to the rect.
        // This is expensive and only works when the KawaseBlur shader is available.
        // For now, draw a semi-transparent overlay as a visual approximation.
        if (!ShaderCache.HasShader("ReactUI/KawaseBlur"))
        {
            // Fallback: draw a frosted-glass approximation with a semi-transparent white overlay
            var frostedColor = new Color(1, 1, 1, 0.1f * cmd.Opacity);
            DrawSolidRect(cmd.Rect, frostedColor);
        }
    }

    /// <summary>
    /// Draw text using glyph quads from the font atlas.
    /// </summary>
    private void DrawText(DrawCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.Text)) return;

        // Must pop GL matrix before using GUI.Label, then push again after
        GL.PopMatrix();

        var guiStyle = new GUIStyle(GUI.skin.label);
        guiStyle.fontSize = (int)cmd.FontSize;
        guiStyle.normal.textColor = cmd.TextColor.ToUnityColor() * new Color(1, 1, 1, cmd.Opacity);
        guiStyle.alignment = cmd.TextAlign switch
        {
            Style.TextAlign.Center => TextAnchor.MiddleCenter,
            Style.TextAlign.Right => TextAnchor.MiddleRight,
            _ => TextAnchor.UpperLeft,
        };
        guiStyle.fontStyle = cmd.FontWeight >= 700 ? FontStyle.Bold : FontStyle.Normal;
        guiStyle.wordWrap = true;
        guiStyle.clipping = TextClipping.Clip;

        // Use actual measured size so text never clips due to layout width estimates
        var content = new GUIContent(cmd.Text);
        var measured = guiStyle.CalcSize(content);
        float w = System.Math.Max(cmd.Rect.Width, measured.x);
        float h = cmd.Rect.Height > 0 ? cmd.Rect.Height : measured.y;

        GUI.Label(
            new UnityEngine.Rect(cmd.Rect.X, cmd.Rect.Y, w, h),
            cmd.Text,
            guiStyle
        );

        // Re-push GL matrix for subsequent draw commands
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
    }

    /// <summary>
    /// Draw a textured image quad with optional border-radius clipping.
    /// </summary>
    private void DrawImage(DrawCommand cmd)
    {
        if (cmd.Texture == null) return;

        // Compute source UV rect based on ObjectFit
        var uvRect = ComputeObjectFitUV(cmd.Texture, cmd.Rect, cmd.ObjectFit);

        Material? mat;
        if (_useSdfShaders && ShaderCache.HasShader("ReactUI/Image"))
        {
            mat = ShaderCache.GetMaterial("ReactUI/Image");
            mat.SetTexture("_MainTex", cmd.Texture);
            mat.SetVector("_RectSize", new Vector4(cmd.Rect.Width, cmd.Rect.Height, 0, 0));
            mat.SetVector("_Radii", new Vector4(
                cmd.BorderRadiusTL, cmd.BorderRadiusTR, cmd.BorderRadiusBR, cmd.BorderRadiusBL));
            mat.SetFloat("_Opacity", cmd.Opacity);
        }
        else
        {
            mat = _fallbackMaterial;
            mat?.SetTexture("_MainTex", cmd.Texture);
        }

        mat?.SetPass(0);

        GL.Begin(7 /* GL.QUADS */);
        GL.Color(new Color(1, 1, 1, cmd.Opacity));

        GL.TexCoord2(uvRect.X, uvRect.Y + uvRect.Height);
        GL.Vertex3(cmd.Rect.X, cmd.Rect.Y, 0);

        GL.TexCoord2(uvRect.Right, uvRect.Y + uvRect.Height);
        GL.Vertex3(cmd.Rect.Right, cmd.Rect.Y, 0);

        GL.TexCoord2(uvRect.Right, uvRect.Y);
        GL.Vertex3(cmd.Rect.Right, cmd.Rect.Bottom, 0);

        GL.TexCoord2(uvRect.X, uvRect.Y);
        GL.Vertex3(cmd.Rect.X, cmd.Rect.Bottom, 0);

        GL.End();
    }

    // --- GL helpers ---

    private void DrawGLQuad(Core.Rect rect)
    {
        GL.Begin(7 /* GL.QUADS */);
        GL.TexCoord2(0, 1); GL.Vertex3(rect.X, rect.Y, 0);
        GL.TexCoord2(1, 1); GL.Vertex3(rect.Right, rect.Y, 0);
        GL.TexCoord2(1, 0); GL.Vertex3(rect.Right, rect.Bottom, 0);
        GL.TexCoord2(0, 0); GL.Vertex3(rect.X, rect.Bottom, 0);
        GL.End();
    }

    private void DrawSolidRect(Core.Rect rect, Color color)
    {
        _fallbackMaterial?.SetPass(0);
        GL.Begin(7 /* GL.QUADS */);
        GL.Color(color);
        GL.Vertex3(rect.X, rect.Y, 0);
        GL.Vertex3(rect.Right, rect.Y, 0);
        GL.Vertex3(rect.Right, rect.Bottom, 0);
        GL.Vertex3(rect.X, rect.Bottom, 0);
        GL.End();
    }

    private void DrawGradientRect(Core.Rect rect, float angleDeg, Color colorA, Color colorB)
    {
        // Compute per-vertex colors based on gradient angle
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        // Project each corner onto the gradient axis to get interpolation t
        float cx = 0.5f, cy = 0.5f;
        float[] ts = new float[4];
        float[][] corners = { new[] { 0f, 1f }, new[] { 1f, 1f }, new[] { 1f, 0f }, new[] { 0f, 0f } };
        float minT = float.MaxValue, maxT = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            float dx = corners[i][0] - cx;
            float dy = corners[i][1] - cy;
            ts[i] = dx * cos + dy * sin;
            minT = Mathf.Min(minT, ts[i]);
            maxT = Mathf.Max(maxT, ts[i]);
        }

        // Normalize to 0-1 range
        float range = maxT - minT;
        if (range < 0.001f) range = 1f;

        Color[] vertColors = new Color[4];
        for (int i = 0; i < 4; i++)
        {
            float t = (ts[i] - minT) / range;
            vertColors[i] = Color.Lerp(colorA, colorB, t);
        }

        _fallbackMaterial?.SetPass(0);
        GL.Begin(7 /* GL.QUADS */);
        GL.Color(vertColors[0]); GL.Vertex3(rect.X, rect.Y, 0);
        GL.Color(vertColors[1]); GL.Vertex3(rect.Right, rect.Y, 0);
        GL.Color(vertColors[2]); GL.Vertex3(rect.Right, rect.Bottom, 0);
        GL.Color(vertColors[3]); GL.Vertex3(rect.X, rect.Bottom, 0);
        GL.End();
    }

    private void DrawBorder(Core.Rect rect, float width, Color color)
    {
        // Top edge
        DrawSolidRect(new Core.Rect(rect.X, rect.Y, rect.Width, width), color);
        // Bottom edge
        DrawSolidRect(new Core.Rect(rect.X, rect.Bottom - width, rect.Width, width), color);
        // Left edge
        DrawSolidRect(new Core.Rect(rect.X, rect.Y + width, width, rect.Height - width * 2), color);
        // Right edge
        DrawSolidRect(new Core.Rect(rect.Right - width, rect.Y + width, width, rect.Height - width * 2), color);
    }

    /// <summary>
    /// Compute UV coordinates for an image based on the ObjectFit mode.
    /// </summary>
    private static Core.Rect ComputeObjectFitUV(Texture2D tex, Core.Rect destRect, Style.ObjectFit fit)
    {
        float texAspect = (float)tex.width / tex.height;
        float destAspect = destRect.Width / destRect.Height;

        switch (fit)
        {
            case Style.ObjectFit.Fill:
                return new Core.Rect(0, 0, 1, 1);

            case Style.ObjectFit.Contain:
            {
                // Show the full image, letterboxed
                if (texAspect > destAspect)
                {
                    // Image is wider — vertical letterbox
                    float visibleHeight = destAspect / texAspect;
                    float offset = (1f - visibleHeight) * 0.5f;
                    return new Core.Rect(0, offset, 1, visibleHeight);
                }
                else
                {
                    float visibleWidth = texAspect / destAspect;
                    float offset = (1f - visibleWidth) * 0.5f;
                    return new Core.Rect(offset, 0, visibleWidth, 1);
                }
            }

            case Style.ObjectFit.Cover:
            {
                // Fill the rect, cropping the excess
                if (texAspect > destAspect)
                {
                    float visibleWidth = destAspect / texAspect;
                    float offset = (1f - visibleWidth) * 0.5f;
                    return new Core.Rect(offset, 0, visibleWidth, 1);
                }
                else
                {
                    float visibleHeight = texAspect / destAspect;
                    float offset = (1f - visibleHeight) * 0.5f;
                    return new Core.Rect(0, offset, 1, visibleHeight);
                }
            }

            case Style.ObjectFit.None:
            {
                // Display at natural size, centered, cropping if needed
                float uWidth = destRect.Width / tex.width;
                float uHeight = destRect.Height / tex.height;
                float offsetX = (1f - uWidth) * 0.5f;
                float offsetY = (1f - uHeight) * 0.5f;
                return new Core.Rect(offsetX, offsetY, uWidth, uHeight);
            }

            case Style.ObjectFit.ScaleDown:
            {
                // Same as Contain if image is larger, otherwise same as None
                if (tex.width > destRect.Width || tex.height > destRect.Height)
                    return ComputeObjectFitUV(tex, destRect, Style.ObjectFit.Contain);
                return ComputeObjectFitUV(tex, destRect, Style.ObjectFit.None);
            }

            default:
                return new Core.Rect(0, 0, 1, 1);
        }
    }

    /// <summary>
    /// Get the current draw command count (for debugging/profiling).
    /// </summary>
    public int CommandCount => _commands.Count;
}
