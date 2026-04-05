using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ReactUI.Rendering;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas-based renderer that replaces the old GL immediate-mode RenderPipeline.
/// Creates a Screen Space Overlay canvas and syncs UINode trees to GameObjects
/// with RectTransform, RawImage (SDF-textured backgrounds), and Text components.
/// All types used are native Unity UI — safe for IL2CPP without injected types.
/// </summary>
public class CanvasRenderer
{
    private GameObject? _canvasGO;
    private Canvas? _canvas;
    private CanvasScaler? _scaler;

    /// <summary>Cached default font for Text components.</summary>
    private Font? _defaultFont;

    /// <summary>Maps UINode identity (RuntimeHelpers.GetHashCode) to its managed GO wrapper.</summary>
    private readonly Dictionary<int, NodeGO> _nodeMap = new();

    /// <summary>Wrapper that tracks Unity objects associated with a single UINode.</summary>
    private class NodeGO
    {
        public GameObject GO = null!;
        public RectTransform RT = null!;
        public RawImage? Background;
        public Text? TextComp;
        public CanvasGroup? Group;
        public RectMask2D? Mask;
        /// <summary>Hash of the style properties that affect the SDF texture, used to skip redundant regeneration.</summary>
        public long LastStyleHash;
    }

    // -----------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------

    /// <summary>
    /// Create the root Canvas. Call once at startup.
    /// </summary>
    public void Initialize()
    {
        _canvasGO = new GameObject("ReactUI_Canvas");
        UnityEngine.Object.DontDestroyOnLoad(_canvasGO);

        _canvas = _canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 30000; // draw on top of everything

        _scaler = _canvasGO.AddComponent<CanvasScaler>();
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        // CanvasRenderer (Unity internal) is added automatically by Canvas.
        // GraphicRaycaster is intentionally omitted — we handle input ourselves.

        _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    /// <summary>
    /// Tear down the canvas and all managed GameObjects, clearing the SDF cache.
    /// </summary>
    public void Destroy()
    {
        foreach (var kvp in _nodeMap)
        {
            if (kvp.Value.GO != null)
                UnityEngine.Object.Destroy(kvp.Value.GO);
        }
        _nodeMap.Clear();

        if (_canvasGO != null)
            UnityEngine.Object.Destroy(_canvasGO);

        _canvasGO = null;
        _canvas = null;
        _scaler = null;

        SdfTextureGenerator.ClearCache();
    }

    /// <summary>
    /// Show or hide the entire canvas.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_canvasGO != null)
            _canvasGO.SetActive(visible);
    }

    // -----------------------------------------------------------------
    // Per-frame render
    // -----------------------------------------------------------------

    /// <summary>
    /// Synchronise the committed UINode tree to Unity GameObjects.
    /// Call each frame after layout has been computed.
    /// </summary>
    public void Render(Core.UINode root)
    {
        if (_canvasGO == null) return;

        var visited = new HashSet<int>();
        SyncNode(root, _canvasGO.transform, visited, 0);

        // Destroy GameObjects whose UINodes no longer exist in the tree.
        var toRemove = new List<int>();
        foreach (var kvp in _nodeMap)
        {
            if (!visited.Contains(kvp.Key))
            {
                if (kvp.Value.GO != null)
                    UnityEngine.Object.Destroy(kvp.Value.GO);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove)
            _nodeMap.Remove(key);
    }

    // -----------------------------------------------------------------
    // Recursive sync
    // -----------------------------------------------------------------

    private void SyncNode(Core.UINode node, Transform parent, HashSet<int> visited, int siblingIndex)
    {
        // 1. Skip component wrapper nodes — they have no visual representation.
        if (node.Type == "__component")
        {
            for (int i = 0; i < node.Children.Count; i++)
                SyncNode(node.Children[i], parent, visited, siblingIndex + i);
            return;
        }

        // 2. Identity key based on the object reference (stable across frames for the same UINode).
        int id = RuntimeHelpers.GetHashCode(node);
        visited.Add(id);

        // 3. Get or create the NodeGO.
        if (!_nodeMap.TryGetValue(id, out var ngo))
        {
            ngo = CreateNodeGO(node, parent);
            _nodeMap[id] = ngo;
        }

        // Reparent if the parent changed (e.g. portals, reordering).
        if (ngo.GO.transform.parent != parent)
            ngo.GO.transform.SetParent(parent, false);

        var style = node.ComputedStyle;
        var rect = node.ScreenRect;

        // 4. Position & size via RectTransform (top-left anchor).
        ngo.RT.anchorMin = new Vector2(0, 1);
        ngo.RT.anchorMax = new Vector2(0, 1);
        ngo.RT.pivot = new Vector2(0, 1);

        // Shadow expansion: the SDF texture may be larger to accommodate shadow blur/spread.
        float shadowPad = 0f;
        if (style.BoxShadow.HasValue)
        {
            var s = style.BoxShadow.Value;
            shadowPad = s.Blur + s.Spread + Mathf.Max(Mathf.Abs(s.OffsetX), Mathf.Abs(s.OffsetY));
        }

        if (shadowPad > 0)
        {
            // Expand the GO rect so the SDF texture has room for the shadow.
            ngo.RT.anchoredPosition = new Vector2(rect.X - shadowPad, -(rect.Y - shadowPad));
            ngo.RT.sizeDelta = new Vector2(rect.Width + shadowPad * 2, rect.Height + shadowPad * 2);
        }
        else
        {
            ngo.RT.anchoredPosition = new Vector2(rect.X, -rect.Y);
            ngo.RT.sizeDelta = new Vector2(rect.Width, rect.Height);
        }

        // 5. Background / visual style -----------------------------------------------
        long styleHash = ComputeVisualStyleHash(style);
        bool hasVisual = HasVisualStyle(style);

        if (hasVisual)
        {
            if (ngo.Background == null)
                ngo.Background = ngo.GO.AddComponent<RawImage>();

            if (styleHash != ngo.LastStyleHash)
            {
                // Regenerate SDF texture.
                int texW = Mathf.Max(1, Mathf.CeilToInt(rect.Width + shadowPad * 2));
                int texH = Mathf.Max(1, Mathf.CeilToInt(rect.Height + shadowPad * 2));

                // Resolve per-corner radii
                float rTL, rTR, rBR, rBL;
                if (style.BorderRadii.HasValue)
                {
                    var br = style.BorderRadii.Value;
                    rTL = br.TopLeft; rTR = br.TopRight; rBR = br.BottomRight; rBL = br.BottomLeft;
                }
                else
                {
                    rTL = rTR = rBR = rBL = style.BorderRadius ?? 0f;
                }

                var generated = SdfTextureGenerator.GetOrCreate(
                    texW, texH,
                    style.Background ?? Style.UIColor.Transparent,
                    style.BackgroundGradient,
                    rTL, rTR, rBR, rBL,
                    style.BorderColor ?? Style.UIColor.Transparent,
                    style.BorderWidth ?? 0f,
                    style.BoxShadow,
                    style.Opacity ?? 1f
                );

                ngo.Background.texture = generated?.Texture;
                ngo.Background.color = Color.white; // tint neutral — SDF texture carries all colour
                ngo.LastStyleHash = styleHash;
            }
        }
        else if (ngo.Background != null)
        {
            // Node lost its visual style — remove the RawImage.
            UnityEngine.Object.Destroy(ngo.Background);
            ngo.Background = null;
            ngo.LastStyleHash = 0;
        }

        // 6. Text rendering -----------------------------------------------------------
        if (node.Type == "text" && !string.IsNullOrEmpty(node.LastVNode?.TextContent))
        {
            if (ngo.TextComp == null)
            {
                ngo.TextComp = ngo.GO.AddComponent<Text>();
                if (_defaultFont != null)
                    ngo.TextComp.font = _defaultFont;
            }

            ngo.TextComp.text = node.LastVNode.TextContent;
            ngo.TextComp.fontSize = (int)(style.FontSize ?? 14f);
            ngo.TextComp.color = (style.Color ?? Style.UIColor.White).ToUnityColor();

            ngo.TextComp.alignment = MapTextAlignment(style.TextAlign ?? Style.TextAlign.Left);
            ngo.TextComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            ngo.TextComp.verticalOverflow = VerticalWrapMode.Overflow;

            if (style.LineHeight.HasValue)
                ngo.TextComp.lineSpacing = style.LineHeight.Value;
        }
        else if (ngo.TextComp != null)
        {
            UnityEngine.Object.Destroy(ngo.TextComp);
            ngo.TextComp = null;
        }

        // 7. Opacity ------------------------------------------------------------------
        float opacity = style.Opacity ?? 1f;
        if (opacity < 1f)
        {
            if (ngo.Group == null)
                ngo.Group = ngo.GO.AddComponent<CanvasGroup>();
            ngo.Group.alpha = opacity;
        }
        else if (ngo.Group != null)
        {
            // Restore full opacity; remove the CanvasGroup to keep things clean.
            UnityEngine.Object.Destroy(ngo.Group);
            ngo.Group = null;
        }

        // 8. Overflow clipping --------------------------------------------------------
        bool needsClip = style.Overflow == Style.Overflow.Hidden
                      || style.Overflow == Style.Overflow.Scroll;
        if (needsClip)
        {
            if (ngo.Mask == null)
                ngo.Mask = ngo.GO.AddComponent<RectMask2D>();
        }
        else if (ngo.Mask != null)
        {
            UnityEngine.Object.Destroy(ngo.Mask);
            ngo.Mask = null;
        }

        // 9. Recurse children ---------------------------------------------------------
        for (int i = 0; i < node.Children.Count; i++)
            SyncNode(node.Children[i], ngo.GO.transform, visited, i);

        // 10. Sibling order (z-order within the parent) --------------------------------
        ngo.GO.transform.SetSiblingIndex(siblingIndex);
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    /// <summary>Create a new NodeGO with a RectTransform-bearing GameObject.</summary>
    private static NodeGO CreateNodeGO(Core.UINode node, Transform parent)
    {
        // Every UI element must live under a Canvas, so creating with a RectTransform is implicit
        // when parented under one. We create a plain GO and add RectTransform ourselves for clarity.
        var go = new GameObject($"RUI_{node.Type}_{RuntimeHelpers.GetHashCode(node):X8}");
        go.transform.SetParent(parent, false);

        // AddComponent<RectTransform> is the standard way to give a GO a RectTransform.
        var rt = go.AddComponent<RectTransform>();

        return new NodeGO { GO = go, RT = rt };
    }

    /// <summary>Returns true when the node has any visual property that requires a background texture.</summary>
    private static bool HasVisualStyle(Style.Style style)
    {
        if (style.Background.HasValue && style.Background.Value.A > 0) return true;
        if (style.BackgroundGradient.HasValue && style.BackgroundGradient.Value.Type != Style.GradientType.None) return true;
        if ((style.BorderWidth ?? 0) > 0 && style.BorderColor.HasValue && style.BorderColor.Value.A > 0) return true;
        if (style.BoxShadow.HasValue && style.BoxShadow.Value.Blur > 0) return true;
        if ((style.BorderRadius ?? 0) > 0) return true;
        if (style.BorderRadii.HasValue) return true;
        return false;
    }

    /// <summary>
    /// Cheap hash of the style properties that affect the SDF texture.
    /// Used to skip redundant <see cref="SdfTextureGenerator"/> calls when the style has not changed.
    /// </summary>
    private static long ComputeVisualStyleHash(Style.Style style)
    {
        unchecked
        {
            long h = 17;
            h = h * 31 + (style.Background?.GetHashCode() ?? 0);
            h = h * 31 + (style.BackgroundGradient?.GetHashCode() ?? 0);
            h = h * 31 + (style.BorderRadius?.GetHashCode() ?? 0);
            h = h * 31 + (style.BorderRadii?.GetHashCode() ?? 0);
            h = h * 31 + (style.BorderColor?.GetHashCode() ?? 0);
            h = h * 31 + (style.BorderWidth?.GetHashCode() ?? 0);
            h = h * 31 + (style.BoxShadow?.GetHashCode() ?? 0);
            return h;
        }
    }

    /// <summary>Map our TextAlign enum to Unity's TextAnchor.</summary>
    private static TextAnchor MapTextAlignment(Style.TextAlign align)
    {
        return align switch
        {
            Style.TextAlign.Center => TextAnchor.UpperCenter,
            Style.TextAlign.Right => TextAnchor.UpperRight,
            Style.TextAlign.Justify => TextAnchor.UpperLeft, // Unity Text has no justify; fall back
            _ => TextAnchor.UpperLeft,
        };
    }
}
