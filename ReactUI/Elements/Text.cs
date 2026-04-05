namespace ReactUI.Elements;

public static class TextElement
{
    public static Core.VNode Create(string content, Style.Style? style = null) =>
        new("text") { TextContent = content, Style = style };
}
