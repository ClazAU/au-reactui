namespace ReactUI.Input;

/// <summary>
/// Point-in-rect hit testing against the committed UI tree.
/// Returns the deepest (front-most) node that contains the given point.
/// </summary>
public static class HitTesting
{
    /// <summary>
    /// Walk the tree back-to-front and return the deepest node containing (x, y).
    /// </summary>
    public static Core.UINode? HitTest(Core.UINode root, float x, float y)
    {
        return HitTestRecursive(root, x, y, 0, 0);
    }

    static Core.UINode? HitTestRecursive(Core.UINode node, float x, float y, float scrollOffsetX, float scrollOffsetY)
    {
        // If this node has pointer-events:none, skip it and all descendants
        var pe = node.ComputedStyle?.PointerEvents;
        if (pe == false)
            return null;

        // The rendered position of this node accounts for accumulated scroll offset
        float renderX = node.ScreenRect.X - scrollOffsetX;
        float renderY = node.ScreenRect.Y - scrollOffsetY;
        float renderW = node.ScreenRect.Width;
        float renderH = node.ScreenRect.Height;

        // Accumulate scroll offset for children of scroll containers
        float childScrollX = scrollOffsetX;
        float childScrollY = scrollOffsetY;
        if (node.ComputedStyle?.Overflow == Style.Overflow.Scroll)
        {
            childScrollX += node.ScrollOffsetX;
            childScrollY += node.ScrollOffsetY;
        }

        // For scroll/hidden overflow containers, children are clipped to the container's rendered rect
        bool isClipping = node.ComputedStyle?.Overflow == Style.Overflow.Scroll
                       || node.ComputedStyle?.Overflow == Style.Overflow.Hidden;
        if (isClipping)
        {
            // If the point is outside this container's rendered rect, no child can be hit
            if (x < renderX || x > renderX + renderW || y < renderY || y > renderY + renderH)
                return null;
        }

        // Check children in reverse order (last child drawn on top = higher priority)
        for (int i = node.Children.Count - 1; i >= 0; i--)
        {
            var hit = HitTestRecursive(node.Children[i], x, y, childScrollX, childScrollY);
            if (hit != null) return hit;
        }

        // Check self: use scroll-adjusted position. A bare layout container (no
        // paint, no handlers) is transparent to the pointer, otherwise every
        // full-screen root would swallow the input meant for the roots below it.
        if (IsHittable(node) && x >= renderX && x <= renderX + renderW && y >= renderY && y <= renderY + renderH)
            return node;

        return null;
    }

    /// <summary>
    /// Whether a node claims pointer hits for its own box: anything painted,
    /// anything with a pointer-driven style or handler, and every leaf element.
    /// </summary>
    public static bool IsHittable(Core.UINode node)
    {
        var st = node.ComputedStyle;
        if (st?.PointerEvents == true) return true;

        switch (node.Type)
        {
            case "text":
            case "image":
            case "input":
            case "toggle":
            case "slider":
            case "keycapture":
                return true;
        }

        if (st != null)
        {
            if (st.Background is { A: > 0f }) return true;
            if (st.BackgroundGradient != null) return true;
            if (st.BorderWidth is > 0f) return true;
            if (st.BoxShadow != null) return true;
            if (st.Overflow == Style.Overflow.Scroll) return true;
            if (st.Cursor.HasValue) return true;
            if (st.Hover != null || st.Active != null || st.Focus != null) return true;
        }

        var props = node.LastVNode?.Props;
        if (props != null)
        {
            foreach (var key in props.Keys)
                if (key.StartsWith("on", System.StringComparison.Ordinal)) return true;
        }

        return false;
    }
}
