using System.Collections.Generic;

namespace ReactUI.Core;

/// <summary>
/// The committed (live) node in the instantiated UI tree.
/// Holds computed style, layout results, interaction state, and render data.
/// </summary>
public class UINode
{
    /// <summary>Element type mirrored from VNode.</summary>
    public string Type;

    /// <summary>Stable key from VNode.</summary>
    public string? Key;

    /// <summary>Fully resolved style (base + pseudo-state overlays applied).</summary>
    public Style.Style ComputedStyle;

    /// <summary>Layout results after flexbox computation. Null until layout pass runs.</summary>
    // NOTE: LayoutNode will be defined in ReactUI.Layout when that layer is implemented.
    public object? Layout; // ReactUI.Layout.LayoutNode — stored as object to avoid forward-ref compile error

    /// <summary>Parent in the committed tree.</summary>
    public UINode? Parent;

    /// <summary>Children in the committed tree.</summary>
    public List<UINode> Children = new();

    // --- Interaction state ---
    public bool IsHovered;
    public bool IsActive;
    public bool IsFocused;
    public float ScrollOffsetX;
    public float ScrollOffsetY;

    /// <summary>Total content height for scroll containers (set during render traversal).</summary>
    public float ContentHeight;

    // --- Render data ---
    /// <summary>Screen-space rectangle after layout.</summary>
    public Rect ScreenRect;

    /// <summary>Clip rectangle (for overflow: hidden / scroll).</summary>
    public Rect ClipRect;

    /// <summary>Draw order (z-sorted for rendering).</summary>
    public int DrawOrder;

    // --- Portal ---
    public bool IsPortal;

    // --- Component identity ---
    public int ComponentId;

    /// <summary>Hook state for component nodes.</summary>
    public Hooks.HookContext? HookContext;

    /// <summary>The VNode that was last reconciled into this UINode (for diffing).</summary>
    public VNode? LastVNode;

    public UINode(string type)
    {
        Type = type;
        ComputedStyle = new Style.Style();
    }
}

/// <summary>
/// Simple axis-aligned rectangle. Avoids depending on UnityEngine.Rect everywhere.
/// </summary>
public struct Rect
{
    public float X, Y, Width, Height;

    public Rect(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }

    public readonly bool Contains(float px, float py) =>
        px >= X && px < X + Width && py >= Y && py < Y + Height;

    public readonly float Right => X + Width;
    public readonly float Bottom => Y + Height;

    public static readonly Rect Zero = new(0, 0, 0, 0);

    public override readonly string ToString() => $"Rect({X}, {Y}, {Width}, {Height})";
}
