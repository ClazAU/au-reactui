using System;

namespace ReactUI.Elements;

public static class ToggleElement
{
    public static Core.VNode Create(bool value, Action<bool> onChange, Style.Style? style = null)
    {
        var node = new Core.VNode("toggle")
        {
            Style = style != null ? DefaultToggleStyle().Merge(style) : DefaultToggleStyle(),
        };
        node.Props["value"] = value;
        node.Props["onChange"] = onChange;
        // Clicking is the only way to change a toggle, so route it through the standard
        // onClick dispatch instead of teaching InputSystem a new node type.
        node.Props["onClick"] = (Action)(() => onChange(!value));
        return node;
    }

    static Style.Style DefaultToggleStyle() => new()
    {
        Cursor = Style.CursorType.Pointer,
    };
}
