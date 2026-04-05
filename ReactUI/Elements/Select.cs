using System;

namespace ReactUI.Elements;

public static class SelectElement
{
    public static Core.VNode Create(string value, Action<string> onChange, string[] options, Style.Style? style = null)
    {
        var node = new Core.VNode("select") { Style = style };
        node.Props["value"] = value;
        node.Props["onChange"] = onChange;
        node.Props["options"] = options;
        return node;
    }
}
