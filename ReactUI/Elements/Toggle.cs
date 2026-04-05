using System;

namespace ReactUI.Elements;

public static class ToggleElement
{
    public static Core.VNode Create(bool value, Action<bool> onChange, Style.Style? style = null)
    {
        var node = new Core.VNode("toggle") { Style = style };
        node.Props["value"] = value;
        node.Props["onChange"] = onChange;
        return node;
    }
}
