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
        // Build layout tree
        var layoutRoot = BuildLayoutTree(root);

        // Run flexbox
        YogaLayout.Calculate(layoutRoot, viewportWidth, viewportHeight);

        // Write results back
        ApplyLayout(root, layoutRoot);
    }

    private static LayoutNode BuildLayoutTree(UINode uiNode)
    {
        var ln = new LayoutNode();

        // Apply style to layout node
        if (uiNode.ComputedStyle != null)
            LayoutBridge.ApplyStyle(ln, uiNode.ComputedStyle);

        // Skip component wrapper nodes — pass through to child
        if (uiNode.Type == "__component" && uiNode.Children.Count == 1)
        {
            return BuildLayoutTree(uiNode.Children[0]);
        }

        // Text nodes need a measure function
        if (uiNode.Type == "text" && uiNode.LastVNode?.TextContent != null)
        {
            var text = uiNode.LastVNode.TextContent;
            var fontSize = uiNode.ComputedStyle?.FontSize ?? 14f;
            var lineHeight = uiNode.ComputedStyle?.LineHeight ?? 1.4f;

            ln.MeasureFunc = (maxWidth, widthMode, maxHeight, heightMode) =>
            {
                // Estimate: ~7px per char at 14px font, lineHeight * fontSize per line
                float charWidth = fontSize * 0.5f;
                float textWidth = text.Length * charWidth;

                float availWidth = widthMode == MeasureMode.Undefined ? float.MaxValue : maxWidth;
                float fitWidth = System.Math.Min(textWidth, availWidth);

                int lines = fitWidth > 0 ? (int)System.Math.Ceiling(textWidth / fitWidth) : 1;
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
        // Skip component wrappers
        if (uiNode.Type == "__component" && uiNode.Children.Count == 1)
        {
            ApplyLayout(uiNode.Children[0], layoutNode);
            // Also set the component wrapper's rect to match
            uiNode.ScreenRect = uiNode.Children[0].ScreenRect;
            return;
        }

        uiNode.ScreenRect = new Rect(
            layoutNode.ComputedX,
            layoutNode.ComputedY,
            layoutNode.ComputedWidth,
            layoutNode.ComputedHeight
        );

        // Set clip rect to full viewport by default (will be refined by parent overflow)
        if (uiNode.ClipRect.Width == 0 && uiNode.ClipRect.Height == 0)
            uiNode.ClipRect = new Rect(0, 0, float.MaxValue, float.MaxValue);

        // Recurse
        int layoutIdx = 0;
        for (int i = 0; i < uiNode.Children.Count; i++)
        {
            var child = uiNode.Children[i];
            if (child.Type == "__component" && child.Children.Count == 1)
            {
                // Component wrapper — pass through
                if (layoutIdx < layoutNode.Children.Count)
                {
                    ApplyLayout(child, layoutNode.Children[layoutIdx]);
                    layoutIdx++;
                }
            }
            else if (layoutIdx < layoutNode.Children.Count)
            {
                ApplyLayout(child, layoutNode.Children[layoutIdx]);
                layoutIdx++;
            }
        }
    }
}
