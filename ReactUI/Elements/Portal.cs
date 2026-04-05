namespace ReactUI.Elements;

public static class PortalElement
{
    public static Core.VNode Create(params Core.VNode[] children) =>
        new("__portal") { Children = children };
}
