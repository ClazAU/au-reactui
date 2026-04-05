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
    static int _logCount;

    public static void ComputeLayout(UINode root, float viewportWidth, float viewportHeight)
    {
        // Build layout tree
        var layoutRoot = BuildLayoutTree(root);

        // Run flexbox
        YogaLayout.Calculate(layoutRoot, viewportWidth, viewportHeight);

        // Write results back
        ApplyLayout(root, layoutRoot);

        // Debug: log first few frames
        if (_logCount++ < 3)
            LogTree(root, 0);
    }

    static void LogTree(UINode node, int depth)
    {
        var indent = new string(' ', depth * 2);
        var r = node.ScreenRect;
        var style = node.ComputedStyle;
        var pos = style?.Position;
        Plugin.ReactUIPlugin.Logger.LogInfo(
            $"[Layout] {indent}{node.Type} rect=({r.X:F0},{r.Y:F0},{r.Width:F0},{r.Height:F0}) pos={pos} bg={style?.Background.HasValue}");
        foreach (var child in node.Children)
            LogTree(child, depth + 1);
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

                if (_logCount <= 2)
                    Plugin.ReactUIPlugin.Logger.LogInfo(
                        $"[Measure] '{text}' maxW={maxWidth:F0} mode={widthMode} → w={fitWidth:F0} h={fitHeight:F0} lines={lines}");

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
