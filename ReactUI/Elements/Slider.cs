using System;

namespace ReactUI.Elements;

public static class SliderElement
{
    /// <summary>
    /// A draggable slider. <paramref name="step"/> quantises the value; pass 0 for continuous
    /// output (the default). The thumb defaults to a circle — give it a different width/height
    /// and a small radius for a box handle.
    /// </summary>
    public static Core.VNode Create(
        float value,
        Action<float> onChange,
        float min = 0,
        float max = 1,
        Style.Style? style = null,
        float step = 0f,
        float thumbWidth = 14f,
        float thumbHeight = 14f,
        float thumbRadius = 7f)
    {
        var node = new Core.VNode("slider") { Style = style };
        node.Props["value"] = value;
        node.Props["onChange"] = onChange;
        node.Props["min"] = min;
        node.Props["max"] = max;
        node.Props["step"] = step;
        node.Props["thumbWidth"] = thumbWidth;
        node.Props["thumbHeight"] = thumbHeight;
        node.Props["thumbRadius"] = thumbRadius;
        return node;
    }
}
