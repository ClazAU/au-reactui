using ReactUI.Core;

namespace ReactUI.Layout;

/// <summary>
/// Runs the full layout pass: builds LayoutNode tree from UINode tree,
/// computes flexbox layout, then writes ScreenRects back to UINodes.
/// </summary>
public static class LayoutEngine
{
    /// <summary>
    /// Compute layout for a root UINode and populate ScreenRect on all nodes.
    /// </summary>
    public static void ComputeLayout(UINode root, float viewportWidth, float viewportHeight)
    {
        var contentLayout = BuildLayoutTree(root);

        // Wrap content in a viewport container so absolute-positioned roots
        // get proper AtMost sizing (content-size) and relative roots fill the viewport.
        var viewport = new LayoutNode { Width = viewportWidth, Height = viewportHeight };
        viewport.AddChild(contentLayout);

        YogaLayout.Calculate(viewport, viewportWidth, viewportHeight);
        ApplyLayout(root, contentLayout);
    }

    private static LayoutNode BuildLayoutTree(UINode uiNode)
    {
        // Component wrappers are layout-transparent: skip the wrapper and return
        // the child's LayoutNode directly. This prevents __component nodes from
        // adding unwanted Column/Stretch layout containers.
        if (uiNode.Type == "__component")
        {
            if (uiNode.Children.Count == 1)
                return BuildLayoutTree(uiNode.Children[0]);

            // Multi-child fallback (shouldn't happen — components render one root)
            var wrapper = new LayoutNode();
            foreach (var child in uiNode.Children)
                wrapper.AddChild(BuildLayoutTree(child));
            return wrapper;
        }

        var ln = new LayoutNode();

        // Apply style to layout node
        if (uiNode.ComputedStyle != null)
            LayoutBridge.ApplyStyle(ln, uiNode.ComputedStyle);

        // Text nodes need a measure function
        if (uiNode.Type == "text" && uiNode.LastVNode?.TextContent != null)
        {
            var text = uiNode.LastVNode.TextContent;
            var fontSize = uiNode.ComputedStyle?.FontSize ?? 14f;
            var fontWeight = uiNode.ComputedStyle?.FontWeight ?? 400;
            var lineHeight = uiNode.ComputedStyle?.LineHeight ?? 1.4f;

            // Inherit text styles from parent if not set
            if (!uiNode.ComputedStyle?.FontSize.HasValue ?? true)
                fontSize = uiNode.Parent?.ComputedStyle?.FontSize ?? fontSize;
            if (!uiNode.ComputedStyle?.FontWeight.HasValue ?? true)
                fontWeight = uiNode.Parent?.ComputedStyle?.FontWeight ?? fontWeight;

            // Cache the GUIStyle measurement for accurate sizing
            var guiStyle = new UnityEngine.GUIStyle();
            guiStyle.fontSize = (int)fontSize;
            guiStyle.fontStyle = fontWeight >= 700 ? UnityEngine.FontStyle.Bold : UnityEngine.FontStyle.Normal;
            guiStyle.wordWrap = false;

            var measured = guiStyle.CalcSize(new UnityEngine.GUIContent(text));
            float measuredW = measured.x;
            float measuredH = measured.y;

            ln.MeasureFunc = (maxWidth, widthMode, maxHeight, heightMode) =>
            {
                float availWidth = widthMode == MeasureMode.Undefined ? float.MaxValue : maxWidth;
                float fitWidth = System.Math.Min(measuredW, availWidth);

                // Estimate line wrapping if constrained
                int lines = fitWidth > 0 && measuredW > fitWidth
                    ? (int)System.Math.Ceiling(measuredW / fitWidth)
                    : 1;
                if (lines < 1) lines = 1;
                float fitHeight = lines * measuredH;

                return (fitWidth, fitHeight);
            };
        }

        // Slider elements need a measure function for track height
        if (uiNode.Type == "slider")
        {
            ln.MeasureFunc = (maxWidth, widthMode, maxHeight, heightMode) =>
            {
                float w = widthMode == MeasureMode.Exactly ? maxWidth : 120;
                return (w, 20); // 20px height for track + thumb
            };
        }

        // Input elements need a measure function for minimum text height
        if (uiNode.Type == "input")
        {
            var fontSize = uiNode.ComputedStyle?.FontSize ?? 14f;
            var fontWeight = uiNode.ComputedStyle?.FontWeight ?? 400;

            var guiStyle = new UnityEngine.GUIStyle();
            guiStyle.fontSize = (int)fontSize;
            guiStyle.fontStyle = fontWeight >= 700 ? UnityEngine.FontStyle.Bold : UnityEngine.FontStyle.Normal;
            float lineH = guiStyle.CalcSize(new UnityEngine.GUIContent("Ag")).y;

            ln.MeasureFunc = (maxWidth, widthMode, maxHeight, heightMode) =>
            {
                float w = widthMode == MeasureMode.Exactly ? maxWidth : 100;
                return (w, lineH);
            };
        }

        // Recurse children
        foreach (var child in uiNode.Children)
        {
            var childLayout = BuildLayoutTree(child);
            ln.AddChild(childLayout);
        }

        return ln;
    }

    private static void ApplyLayout(UINode uiNode, LayoutNode layoutNode)
    {
        // Component wrappers are layout-transparent: they share the same LayoutNode
        // as their single child. Copy the rect to the wrapper and recurse into the child.
        if (uiNode.Type == "__component")
        {
            uiNode.ScreenRect = new Rect(
                layoutNode.ComputedX,
                layoutNode.ComputedY,
                layoutNode.ComputedWidth,
                layoutNode.ComputedHeight
            );
            if (uiNode.ClipRect.Width == 0 && uiNode.ClipRect.Height == 0)
                uiNode.ClipRect = new Rect(0, 0, float.MaxValue, float.MaxValue);

            if (uiNode.Children.Count == 1)
            {
                // Same layoutNode — the child IS the layoutNode
                ApplyLayout(uiNode.Children[0], layoutNode);
            }
            else
            {
                // Multi-child fallback
                int count = System.Math.Min(uiNode.Children.Count, layoutNode.Children.Count);
                for (int i = 0; i < count; i++)
                    ApplyLayout(uiNode.Children[i], layoutNode.Children[i]);
            }
            return;
        }

        // YogaLayout.Calculate already converts ComputedX/Y to absolute screen coords
        uiNode.ScreenRect = new Rect(
            layoutNode.ComputedX,
            layoutNode.ComputedY,
            layoutNode.ComputedWidth,
            layoutNode.ComputedHeight
        );

        // Set clip rect to full viewport by default
        if (uiNode.ClipRect.Width == 0 && uiNode.ClipRect.Height == 0)
            uiNode.ClipRect = new Rect(0, 0, float.MaxValue, float.MaxValue);

        // Recurse — UINode children and LayoutNode children are 1:1
        // (with __component nodes already handled above)
        int layoutIdx = 0;
        for (int i = 0; i < uiNode.Children.Count; i++)
        {
            var child = uiNode.Children[i];
            if (child.Type == "__component")
            {
                // __component was flattened in layout tree — its LayoutNode is at layoutIdx
                if (layoutIdx < layoutNode.Children.Count)
                    ApplyLayout(child, layoutNode.Children[layoutIdx++]);
            }
            else
            {
                if (layoutIdx < layoutNode.Children.Count)
                    ApplyLayout(child, layoutNode.Children[layoutIdx++]);
            }
        }
    }
}
