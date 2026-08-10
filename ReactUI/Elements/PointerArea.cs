using System;

namespace ReactUI.Elements;

/// <summary>
/// An element that reports the pointer's normalized position within its own rect while pressed.
/// Use for controls whose value is a position rather than a click — colour wheels, XY pads, curve
/// editors. The callback receives (0,0) at the top-left and (1,1) at the bottom-right; values run
/// outside that range if the pointer is dragged beyond the element, so clamp if you need to.
/// </summary>
public static class PointerAreaElement
{
    public static Core.VNode Create(Action<UnityEngine.Vector2> onPointer, Style.Style? style = null, params Core.VNode[] children)
    {
        var node = new Core.VNode("pointerarea") { Style = style };
        node.Props["onPointer"] = onPointer;
        node.Children = children.Length > 0 ? children : null;
        return node;
    }
}
