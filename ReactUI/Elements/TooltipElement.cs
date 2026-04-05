namespace ReactUI.Elements;

public static class TooltipElement
{
    public static Core.VNode Create(string text, Core.VNode child, Style.Style? style = null)
    {
        var node = new Core.VNode("tooltip") { Style = style };
        node.Props["tooltipText"] = text;
        node.Children = new[] { child };
        return node;
    }
}
