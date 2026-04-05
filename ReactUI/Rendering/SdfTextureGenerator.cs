namespace ReactUI.Rendering;

using System;
using System.Collections.Generic;
using ReactUI.Style;
using UnityEngine;
using Gradient = ReactUI.Style.Gradient;

/// <summary>
/// Result of texture generation, including padding for shadow expansion.
/// </summary>
public struct GeneratedTexture
{
    public Texture2D Texture;
    public float PaddingLeft, PaddingTop, PaddingRight, PaddingBottom;
}

/// <summary>
/// CPU-based SDF texture generator. Produces Texture2D assets with rounded rectangles,
/// gradients, borders, and box shadows using SDF math executed on CPU and cached.
/// IL2CPP safe — no Reflection.Emit, no dynamic.
/// </summary>
public static class SdfTextureGenerator
{
    private static readonly Dictionary<long, GeneratedTexture> _cache = new();

    /// <summary>
    /// Get or generate a texture for the given visual style.
    /// Returns null texture if the element has no visual properties worth texturing.
    /// </summary>
    public static GeneratedTexture? GetOrCreate(
        int width, int height,
        UIColor bgColor,
        Gradient? gradient,
        float radiusTL, float radiusTR, float radiusBR, float radiusBL,
        UIColor borderColor, float borderWidth,
        BoxShadow? shadow,
        float opacity)
    {
        if (width <= 0 || height <= 0) return null;

        width = Math.Min(width, 1024);
        height = Math.Min(height, 1024);

        long hash = ComputeHash(width, height, bgColor, gradient,
            radiusTL, radiusTR, radiusBR, radiusBL,
            borderColor, borderWidth, shadow, opacity);

        if (_cache.TryGetValue(hash, out var cached) && cached.Texture != null)
            return cached;

        var result = Generate(width, height, bgColor, gradient,
            radiusTL, radiusTR, radiusBR, radiusBL,
            borderColor, borderWidth, shadow, opacity);

        _cache[hash] = result;
        return result;
    }

    /// <summary>
    /// Destroy all cached textures and clear the cache dictionary.
    /// </summary>
    public static void ClearCache()
    {
        foreach (var kvp in _cache)
        {
            if (kvp.Value.Texture != null)
                UnityEngine.Object.Destroy(kvp.Value.Texture);
        }
        _cache.Clear();
    }

    // ───────────────────────── Generation ─────────────────────────

    private static GeneratedTexture Generate(
        int width, int height,
        UIColor bgColor,
        Gradient? gradient,
        float rTL, float rTR, float rBR, float rBL,
        UIColor borderColor, float borderWidth,
        BoxShadow? shadow,
        float opacity)
    {
        // Compute shadow expansion padding
        float padL = 0, padT = 0, padR = 0, padB = 0;
        float shadowSigma = 0;

        if (shadow.HasValue)
        {
            var s = shadow.Value;
            float extent = s.Blur + s.Spread;
            padL = Math.Max(0, extent - s.OffsetX);
            padR = Math.Max(0, extent + s.OffsetX);
            padT = Math.Max(0, extent - s.OffsetY);
            padB = Math.Max(0, extent + s.OffsetY);
            shadowSigma = Math.Max(s.Blur * 0.5f, 0.001f);
        }

        int padLi = (int)Math.Ceiling(padL);
        int padRi = (int)Math.Ceiling(padR);
        int padTi = (int)Math.Ceiling(padT);
        int padBi = (int)Math.Ceiling(padB);

        int texW = width + padLi + padRi;
        int texH = height + padTi + padBi;
        texW = Math.Max(texW, 1);
        texH = Math.Max(texH, 1);

        var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        // Center of the logical rect within the expanded texture
        float cx = padLi + halfW;
        float cy = padBi + halfH; // texture Y=0 is bottom

        var pixels = new Color[texW * texH];

        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                // Position relative to rect center
                float px = x + 0.5f - cx;
                float py = y + 0.5f - cy;

                Color result = new Color(0, 0, 0, 0);

                // 1) Shadow layer (drawn behind everything)
                if (shadow.HasValue && !shadow.Value.Inset)
                {
                    var s = shadow.Value;
                    // Shadow rect is offset and expanded by spread
                    float spx = px - s.OffsetX;
                    float spy = py + s.OffsetY; // negate Y: offsetY positive = downward, but texture Y is up
                    float sHalfW = halfW + s.Spread;
                    float sHalfH = halfH + s.Spread;

                    float sDist = RoundedRectSDF(spx, spy, sHalfW, sHalfH, rTL, rTR, rBR, rBL);

                    if (sDist > 0)
                    {
                        float falloff = (float)Math.Exp(-(sDist * sDist) / (2f * shadowSigma * shadowSigma));
                        result = new Color(s.Color.R, s.Color.G, s.Color.B, s.Color.A * falloff);
                    }
                    else
                    {
                        // Inside shadow rect — full shadow color (will be covered by fill)
                        result = new Color(s.Color.R, s.Color.G, s.Color.B, s.Color.A);
                    }
                }

                // 2) Main rectangle SDF
                float dist = RoundedRectSDF(px, py, halfW, halfH, rTL, rTR, rBR, rBL);

                // Anti-aliased fill alpha: 1 inside, 0 outside, feathered at edge
                float fillAlpha = 1f - Smoothstep(-0.5f, 0.5f, dist);

                // 3) Determine fill color (background or gradient)
                Color fill;
                if (gradient.HasValue && gradient.Value.Type != GradientType.None)
                {
                    fill = SampleGradient(gradient.Value, px, py, halfW, halfH);
                }
                else
                {
                    fill = new Color(bgColor.R, bgColor.G, bgColor.B, bgColor.A);
                }

                // 4) Border
                float borderAlpha = 0f;
                if (borderWidth > 0)
                {
                    // Border occupies the inner edge: from dist = -borderWidth to dist = 0
                    float innerEdge = Smoothstep(-0.5f, 0.5f, dist + borderWidth);
                    float outerEdge = 1f - Smoothstep(-0.5f, 0.5f, dist);
                    borderAlpha = innerEdge * outerEdge;
                }

                // 5) Inset shadow (rendered on top of fill, inside the rect)
                float insetAlpha = 0f;
                Color insetColor = new Color(0, 0, 0, 0);
                if (shadow.HasValue && shadow.Value.Inset && fillAlpha > 0)
                {
                    var s = shadow.Value;
                    float ipx = px - s.OffsetX;
                    float ipy = py + s.OffsetY;
                    float iHalfW = halfW - s.Spread;
                    float iHalfH = halfH - s.Spread;
                    float iSigma = Math.Max(s.Blur * 0.5f, 0.001f);

                    if (iHalfW > 0 && iHalfH > 0)
                    {
                        float iDist = RoundedRectSDF(ipx, ipy, iHalfW, iHalfH, rTL, rTR, rBR, rBL);
                        // Inset shadow: visible where iDist > 0 (outside the shrunk rect = near edges)
                        if (iDist > 0)
                        {
                            float falloff = (float)Math.Exp(-(iDist * iDist) / (2f * iSigma * iSigma));
                            insetAlpha = s.Color.A * falloff;
                            insetColor = new Color(s.Color.R, s.Color.G, s.Color.B, insetAlpha);
                        }
                    }
                }

                // 6) Composite layers
                // Start with shadow (already in result from step 1)
                // Composite fill on top
                if (fillAlpha > 0)
                {
                    float fa = fill.a * fillAlpha;
                    result = AlphaBlend(result, new Color(fill.r, fill.g, fill.b, fa));
                }

                // Composite inset shadow on top of fill
                if (insetAlpha > 0)
                {
                    result = AlphaBlend(result, insetColor);
                }

                // Composite border on top
                if (borderAlpha > 0)
                {
                    float ba = borderColor.A * borderAlpha;
                    result = AlphaBlend(result, new Color(borderColor.R, borderColor.G, borderColor.B, ba));
                }

                // 7) Apply opacity
                result.a *= opacity;

                pixels[y * texW + x] = result;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);

        return new GeneratedTexture
        {
            Texture = tex,
            PaddingLeft = padLi,
            PaddingTop = padTi,
            PaddingRight = padRi,
            PaddingBottom = padBi,
        };
    }

    // ───────────────────────── SDF ─────────────────────────

    private static float RoundedRectSDF(
        float px, float py,
        float halfW, float halfH,
        float rTL, float rTR, float rBR, float rBL)
    {
        // Select radius for quadrant.
        // px > 0 = right side, py > 0 = top half (texture coords, Y-up).
        // Convention: py > 0 is top => rTR/rTL; py < 0 is bottom => rBR/rBL.
        float r;
        if (px > 0)
            r = (py > 0) ? rTR : rBR;
        else
            r = (py > 0) ? rTL : rBL;

        // Clamp radius so it doesn't exceed the half-dimensions
        r = Math.Min(r, Math.Min(halfW, halfH));

        float qx = Math.Abs(px) - halfW + r;
        float qy = Math.Abs(py) - halfH + r;

        float outerLen = Length(Math.Max(qx, 0f), Math.Max(qy, 0f));
        float innerDist = Math.Min(Math.Max(qx, qy), 0f);

        return innerDist + outerLen - r;
    }

    // ───────────────────────── Helpers ─────────────────────────

    private static float Length(float x, float y)
    {
        return (float)Math.Sqrt(x * x + y * y);
    }

    private static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Standard alpha-blend: src over dst (premultiplied-output style).
    /// </summary>
    private static Color AlphaBlend(Color dst, Color src)
    {
        float sa = src.a;
        float invSa = 1f - sa;
        float outA = sa + dst.a * invSa;

        if (outA < 0.0001f)
            return new Color(0, 0, 0, 0);

        return new Color(
            (src.r * sa + dst.r * dst.a * invSa) / outA,
            (src.g * sa + dst.g * dst.a * invSa) / outA,
            (src.b * sa + dst.b * dst.a * invSa) / outA,
            outA);
    }

    private static Color SampleGradient(Gradient grad, float px, float py, float halfW, float halfH)
    {
        float t;

        if (grad.Type == GradientType.Radial)
        {
            // Radial: distance from center normalized to corner
            float maxR = Length(halfW, halfH);
            t = (maxR > 0) ? Mathf.Clamp01(Length(px, py) / maxR) : 0f;
        }
        else
        {
            // Linear: project position onto gradient direction
            float rad = grad.Angle * Mathf.Deg2Rad;
            float dx = (float)Math.Sin(rad);  // CSS gradient angles: 0deg = bottom-to-top, 90deg = left-to-right
            float dy = (float)Math.Cos(rad);

            // Project range: the gradient line spans the full rect
            float projLen = Math.Abs(dx) * halfW + Math.Abs(dy) * halfH;
            float proj = px * dx + py * dy;
            t = (projLen > 0) ? Mathf.Clamp01((proj + projLen) / (2f * projLen)) : 0.5f;
        }

        var ca = grad.ColorA;
        var cb = grad.ColorB;
        return new Color(
            ca.R + (cb.R - ca.R) * t,
            ca.G + (cb.G - ca.G) * t,
            ca.B + (cb.B - ca.B) * t,
            ca.A + (cb.A - ca.A) * t);
    }

    // ───────────────────────── Hashing ─────────────────────────

    private static long ComputeHash(
        int width, int height,
        UIColor bgColor,
        Gradient? gradient,
        float rTL, float rTR, float rBR, float rBL,
        UIColor borderColor, float borderWidth,
        BoxShadow? shadow,
        float opacity)
    {
        unchecked
        {
            long h = 17;
            h = h * 31 + width;
            h = h * 31 + height;
            h = h * 31 + HashColor(bgColor);
            h = h * 31 + HashFloat(rTL);
            h = h * 31 + HashFloat(rTR);
            h = h * 31 + HashFloat(rBR);
            h = h * 31 + HashFloat(rBL);
            h = h * 31 + HashColor(borderColor);
            h = h * 31 + HashFloat(borderWidth);
            h = h * 31 + HashFloat(opacity);

            if (gradient.HasValue)
            {
                var g = gradient.Value;
                h = h * 31 + (int)g.Type;
                h = h * 31 + HashFloat(g.Angle);
                h = h * 31 + HashColor(g.ColorA);
                h = h * 31 + HashColor(g.ColorB);
            }

            if (shadow.HasValue)
            {
                var s = shadow.Value;
                h = h * 31 + HashFloat(s.OffsetX);
                h = h * 31 + HashFloat(s.OffsetY);
                h = h * 31 + HashFloat(s.Blur);
                h = h * 31 + HashFloat(s.Spread);
                h = h * 31 + HashColor(s.Color);
                h = h * 31 + (s.Inset ? 1 : 0);
            }

            return h;
        }
    }

    private static long HashFloat(float f)
    {
        // Quantize to avoid floating-point noise causing cache misses.
        // 1/128 resolution is plenty for pixel-level rendering.
        return (long)(f * 128f);
    }

    private static long HashColor(UIColor c)
    {
        unchecked
        {
            return ((long)(c.R * 255f) << 24)
                 | ((long)(c.G * 255f) << 16)
                 | ((long)(c.B * 255f) << 8)
                 | (long)(c.A * 255f);
        }
    }
}
