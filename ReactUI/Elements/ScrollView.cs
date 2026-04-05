namespace ReactUI.Elements;

public static class ScrollViewElement
{
    public static Core.VNode Create(Style.Style? style = null, params Core.VNode[] children)
    {
        var s = style ?? new Style.Style();
        s.Overflow = Style.Overflow.Scroll;
        return new Core.VNode("scrollview") { Style = s, Children = children };
    }
}
