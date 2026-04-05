using System;

namespace ReactUI.Elements;

public static class InputElement
{
    public static Core.VNode Create(string value, Action<string> onChange, Style.Style? style = null, string placeholder = "")
    {
        var node = new Core.VNode("input") { Style = style ?? DefaultInputStyle() };
        node.Props["value"] = value;
        node.Props["onChange"] = onChange;
        node.Props["placeholder"] = placeholder;
        return node;
    }

    static Style.Style DefaultInputStyle() => new()
    {
        Padding = new Style.EdgeValues(6, 10),
        BorderWidth = 1,
        BorderColor = "#444",
        BorderRadius = 6,
        Background = "#1a1a2e",
        Color = "#e0e0e0",
        FontSize = 14,
        Cursor = Style.CursorType.Text,
    };
}
