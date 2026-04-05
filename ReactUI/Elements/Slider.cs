using System;

namespace ReactUI.Elements;

public static class SliderElement
{
    public static Core.VNode Create(float value, Action<float> onChange, float min = 0, float max = 1, Style.Style? style = null)
    {
        var node = new Core.VNode("slider") { Style = style };
        node.Props["value"] = value;
        node.Props["onChange"] = onChange;
        node.Props["min"] = min;
        node.Props["max"] = max;
        return node;
    }
}
