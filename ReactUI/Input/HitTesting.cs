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
        return HitTestRecursive(root, x, y);
    }

    static Core.UINode? HitTestRecursive(Core.UINode node, float x, float y)
    {
        // Check children in reverse order (last child drawn on top = higher priority)
        for (int i = node.Children.Count - 1; i >= 0; i--)
        {
            var hit = HitTestRecursive(node.Children[i], x, y);
            if (hit != null) return hit;
        }

        // Check self: point must be within both ScreenRect and ClipRect
        if (node.ScreenRect.Contains(x, y) && node.ClipRect.Contains(x, y))
        {
            // Respect PointerEvents style (null defaults to true)
            var pe = node.ComputedStyle?.PointerEvents;
            if (pe != false)
                return node;
        }

        return null;
    }
}
