using System;

namespace ReactUI.Elements;

public static class ButtonElement
{
    public static Core.VNode Create(string label, Action onClick, Style.Style? style = null)
    {
        var node = new Core.VNode("button") { Style = style != null ? DefaultButtonStyle().Merge(style) : DefaultButtonStyle() };
        node.Props["onClick"] = onClick;
        node.Children = new[] { TextElement.Create(label) };
        return node;
    }

    public static Core.VNode Create(Action onClick, Style.Style? style, params Core.VNode[] children)
    {
        var node = new Core.VNode("button") { Style = style != null ? DefaultButtonStyle().Merge(style) : DefaultButtonStyle() };
        node.Props["onClick"] = onClick;
        node.Children = children;
        return node;
    }

    static Style.Style DefaultButtonStyle() => new()
    {
        Cursor = Style.CursorType.Pointer,
        AlignItems = Style.AlignItems.Center,
        JustifyContent = Style.JustifyContent.Center,
    };
}
