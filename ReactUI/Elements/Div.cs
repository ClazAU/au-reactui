using System.Collections.Generic;
using System.Linq;

namespace ReactUI.Elements;

public static class Div
{
    public static Core.VNode Create(Style.Style? style = null, params Core.VNode[] children) =>
        new("div") { Style = style, Children = children };

    public static Core.VNode Create(Style.Style? style, IEnumerable<Core.VNode?> children) =>
        new("div") { Style = style, Children = children.Where(c => c != null).Cast<Core.VNode>().ToArray() };
}
