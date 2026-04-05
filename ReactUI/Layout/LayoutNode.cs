using System;
using System.Collections.Generic;

namespace ReactUI.Layout;

public class LayoutNode
{
    // --- Inputs (from style) ---
    public FlexDirection FlexDirection = FlexDirection.Column;
    public JustifyContent JustifyContent = JustifyContent.FlexStart;
    public AlignItems AlignItems = AlignItems.Stretch;
    public AlignSelf AlignSelf = AlignSelf.Auto;
    public FlexWrap FlexWrap = FlexWrap.NoWrap;
    public float FlexGrow = 0;
    public float FlexShrink = 1;
    public float FlexBasis = float.NaN; // NaN = auto
    public float Width = float.NaN, Height = float.NaN;
    public float MinWidth = float.NaN, MinHeight = float.NaN;
    public float MaxWidth = float.NaN, MaxHeight = float.NaN;
    public float PaddingTop, PaddingRight, PaddingBottom, PaddingLeft;
    public float MarginTop, MarginRight, MarginBottom, MarginLeft;
    public float Gap;
    public PositionType Position = PositionType.Relative;
    public float PositionTop = float.NaN, PositionRight = float.NaN;
    public float PositionBottom = float.NaN, PositionLeft = float.NaN;
    public Overflow Overflow = Overflow.Visible;
    public float AspectRatio = float.NaN;
    public bool Display = true; // false = display:none

    // --- Outputs (computed) ---
    public float ComputedX, ComputedY;
    public float ComputedWidth, ComputedHeight;

    // --- Tree ---
    public LayoutNode? Parent;
    public List<LayoutNode> Children = new();
    public bool IsDirty = true;

    // Measure function for leaf nodes (text)
    public Func<float, MeasureMode, float, MeasureMode, (float w, float h)>? MeasureFunc;

    public void AddChild(LayoutNode child)
    {
        child.Parent = this;
        Children.Add(child);
        IsDirty = true;
    }

    public void RemoveChild(LayoutNode child)
    {
        child.Parent = null;
        Children.Remove(child);
        IsDirty = true;
    }

    public void MarkDirty()
    {
        IsDirty = true;
        Parent?.MarkDirty();
    }
}
