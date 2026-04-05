using System.Collections.Generic;

namespace ReactUI.Input;

/// <summary>
/// Manages keyboard focus for interactive elements.
/// Supports explicit focus setting and Tab/Shift+Tab navigation.
/// </summary>
public static class FocusManager
{
    static Core.UINode? _focused;

    public static Core.UINode? Focused => _focused;

    public static void SetFocus(Core.UINode? node)
    {
        if (_focused == node) return;
        if (_focused != null)
        {
            _focused.IsFocused = false;
        }
        _focused = node;
        if (_focused != null)
        {
            _focused.IsFocused = true;
        }
    }

    /// <summary>
    /// Tab through focusable elements in tree order.
    /// If reverse is true, goes to the previous focusable element (Shift+Tab).
    /// </summary>
    public static void TabNext(Core.UINode root, bool reverse = false)
    {
        var focusable = new List<Core.UINode>();
        CollectFocusable(root, focusable);
        if (focusable.Count == 0) return;

        int idx = _focused != null ? focusable.IndexOf(_focused) : -1;
        if (reverse)
            idx = idx <= 0 ? focusable.Count - 1 : idx - 1;
        else
            idx = (idx + 1) % focusable.Count;

        SetFocus(focusable[idx]);
    }

    static void CollectFocusable(Core.UINode node, List<Core.UINode> list)
    {
        if (node.Type is "button" or "input" or "select" or "toggle" or "slider" or "keycapture")
            list.Add(node);
        foreach (var child in node.Children)
            CollectFocusable(child, list);
    }
}
