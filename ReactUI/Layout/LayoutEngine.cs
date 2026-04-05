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
        var layoutRoot = BuildLayoutTree(root);
        YogaLayout.Calculate(layoutRoot, viewportWidth, viewportHeight);
        ApplyLayout(root, layoutRoot);
    }

    private static LayoutNode BuildLayoutTree(UINode uiNode)
    {
        var ln = new LayoutNode();

        // Component wrappers: create a transparent pass-through container
        // (don't skip — absolute children need a parent to position relative to)
        if (uiNode.Type == "__component")
        {
            // No style to apply — just recurse children
            foreach (var child in uiNode.Children)
            {
                var childLayout = BuildLayoutTree(child);
                ln.AddChild(childLayout);
            }
            return ln;
        }

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
                float charWidth = fontSize * 0.5f;
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
        int count = System.Math.Min(uiNode.Children.Count, layoutNode.Children.Count);
        for (int i = 0; i < count; i++)
            ApplyLayout(uiNode.Children[i], layoutNode.Children[i]);
    }
}
