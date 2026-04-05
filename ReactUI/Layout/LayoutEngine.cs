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
            var lineHeight = uiNode.ComputedStyle?.LineHeight ?? 1.4f;

            ln.MeasureFunc = (maxWidth, widthMode, maxHeight, heightMode) =>
            {
                float charWidth = fontSize * 0.62f;
                float textWidth = text.Length * charWidth;

                float availWidth = widthMode == MeasureMode.Undefined ? float.MaxValue : maxWidth;
                float fitWidth = System.Math.Min(textWidth, availWidth);

                int lines = fitWidth > 0 ? (int)System.Math.Ceiling(textWidth / fitWidth) : 1;
                if (lines < 1) lines = 1;
                float fitHeight = lines * fontSize * lineHeight;

                return (fitWidth, fitHeight);
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
