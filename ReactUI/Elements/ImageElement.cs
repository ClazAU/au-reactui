namespace ReactUI.Elements;

public static class ImageElement
{
    public static Core.VNode Create(UnityEngine.Texture2D texture, Style.Style? style = null)
    {
        var node = new Core.VNode("image") { Style = style };
        node.Props["texture"] = texture;
        return node;
    }
}
