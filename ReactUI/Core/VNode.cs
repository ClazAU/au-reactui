using System;
using System.Collections.Generic;

namespace ReactUI.Core;

/// <summary>
/// Immutable virtual DOM node describing what should be rendered.
/// All generic/dynamic data is stored as object (boxed) for IL2CPP safety.
/// </summary>
public class VNode
{
    /// <summary>Element type: "div", "text", "button", "image", "__component", etc.</summary>
    public string Type;

    /// <summary>Stable key for keyed list reconciliation.</summary>
    public string? Key;

    /// <summary>Style descriptor (nullable; null = no explicit style).</summary>
    public Style.Style? Style;

    /// <summary>
    /// Arbitrary props bag. Stores onClick (Action), value (string), placeholder, src, etc.
    /// All values boxed as object for IL2CPP safety — no Reflection.Emit, no dynamic.
    /// </summary>
    public Dictionary<string, object?> Props;

    /// <summary>Child virtual nodes.</summary>
    public VNode[]? Children;

    /// <summary>Direct text content for text-type nodes.</summary>
    public string? TextContent;

    /// <summary>Identity of the component that produced this node.</summary>
    public int ComponentId;

    /// <summary>
    /// For component VNodes (Type == "__component"): the render function to invoke.
    /// The hooks runtime is activated before calling this.
    /// </summary>
    public Func<VNode>? RenderFunc;

    public VNode(string type)
    {
        Type = type;
        Props = new Dictionary<string, object?>();
    }
}
