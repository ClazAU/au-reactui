using System;

namespace ReactUI.Elements;

public static class KeyCaptureElement
{
    public static Core.VNode Create(Action<UnityEngine.KeyCode> onCapture, Style.Style? style = null, string prompt = "Press a key...")
    {
        var node = new Core.VNode("keycapture") { Style = style, TextContent = prompt };
        node.Props["onCapture"] = onCapture;
        return node;
    }
}
