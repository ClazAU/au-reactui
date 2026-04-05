using ReactUI.Style;

namespace ReactUI.Layout;

/// <summary>
/// Bridges ReactUI.Style to the layout engine's LayoutNode inputs,
/// and converts computed layout results back to rects.
/// </summary>
public static class LayoutBridge
{
    /// <summary>
    /// Sync all layout-relevant properties from a Style to a LayoutNode.
    /// </summary>
    public static void ApplyStyle(LayoutNode node, ReactUI.Style.Style style)
    {
        // Flex container
        if (style.FlexDirection.HasValue)
            node.FlexDirection = style.FlexDirection.Value switch
            {
                Style.FlexDirection.Row => Layout.FlexDirection.Row,
                Style.FlexDirection.RowReverse => Layout.FlexDirection.RowReverse,
                Style.FlexDirection.Column => Layout.FlexDirection.Column,
                Style.FlexDirection.ColumnReverse => Layout.FlexDirection.ColumnReverse,
                _ => node.FlexDirection,
            };

        if (style.JustifyContent.HasValue)
            node.JustifyContent = style.JustifyContent.Value switch
            {
                Style.JustifyContent.FlexStart => Layout.JustifyContent.FlexStart,
                Style.JustifyContent.FlexEnd => Layout.JustifyContent.FlexEnd,
                Style.JustifyContent.Center => Layout.JustifyContent.Center,
                Style.JustifyContent.SpaceBetween => Layout.JustifyContent.SpaceBetween,
                Style.JustifyContent.SpaceAround => Layout.JustifyContent.SpaceAround,
                Style.JustifyContent.SpaceEvenly => Layout.JustifyContent.SpaceEvenly,
                _ => node.JustifyContent,
            };

        if (style.AlignItems.HasValue)
            node.AlignItems = style.AlignItems.Value switch
            {
                Style.AlignItems.FlexStart => Layout.AlignItems.FlexStart,
                Style.AlignItems.FlexEnd => Layout.AlignItems.FlexEnd,
                Style.AlignItems.Center => Layout.AlignItems.Center,
                Style.AlignItems.Stretch => Layout.AlignItems.Stretch,
                Style.AlignItems.Baseline => Layout.AlignItems.Baseline,
                _ => node.AlignItems,
            };

        if (style.AlignSelf.HasValue)
            node.AlignSelf = style.AlignSelf.Value switch
            {
                Style.AlignSelf.Auto => Layout.AlignSelf.Auto,
                Style.AlignSelf.FlexStart => Layout.AlignSelf.FlexStart,
                Style.AlignSelf.FlexEnd => Layout.AlignSelf.FlexEnd,
                Style.AlignSelf.Center => Layout.AlignSelf.Center,
                Style.AlignSelf.Stretch => Layout.AlignSelf.Stretch,
                Style.AlignSelf.Baseline => Layout.AlignSelf.Stretch, // Baseline -> Stretch fallback
                _ => node.AlignSelf,
            };

        if (style.FlexWrap.HasValue)
            node.FlexWrap = style.FlexWrap.Value switch
            {
                Style.FlexWrap.NoWrap => Layout.FlexWrap.NoWrap,
                Style.FlexWrap.Wrap => Layout.FlexWrap.Wrap,
                Style.FlexWrap.WrapReverse => Layout.FlexWrap.Wrap, // WrapReverse -> Wrap fallback
                _ => node.FlexWrap,
            };

        // Flex item
        if (style.FlexGrow.HasValue)
            node.FlexGrow = style.FlexGrow.Value;
        if (style.FlexShrink.HasValue)
            node.FlexShrink = style.FlexShrink.Value;
        if (style.FlexBasis.HasValue)
            node.FlexBasis = ResolvePx(style.FlexBasis.Value);

        // Dimensions
        if (style.Width.HasValue)
        {
            if (style.Width.Value.Unit == StyleUnit.Percent)
                node.WidthPercent = style.Width.Value.Value;
            else
                node.Width = ResolvePx(style.Width.Value);
        }
        if (style.Height.HasValue)
        {
            if (style.Height.Value.Unit == StyleUnit.Percent)
                node.HeightPercent = style.Height.Value.Value;
            else
                node.Height = ResolvePx(style.Height.Value);
        }
        if (style.MinWidth.HasValue)
            node.MinWidth = ResolvePx(style.MinWidth.Value);
        if (style.MinHeight.HasValue)
            node.MinHeight = ResolvePx(style.MinHeight.Value);
        if (style.MaxWidth.HasValue)
            node.MaxWidth = ResolvePx(style.MaxWidth.Value);
        if (style.MaxHeight.HasValue)
            node.MaxHeight = ResolvePx(style.MaxHeight.Value);

        // Padding (resolve to px, percent not supported for padding)
        if (style.Padding.HasValue)
        {
            var p = style.Padding.Value;
            node.PaddingTop = ResolveEdge(p.Top);
            node.PaddingRight = ResolveEdge(p.Right);
            node.PaddingBottom = ResolveEdge(p.Bottom);
            node.PaddingLeft = ResolveEdge(p.Left);
        }

        // Margin (store raw StyleValues for percent resolution during layout)
        if (style.Margin.HasValue)
        {
            var m = style.Margin.Value;
            node.MarginTopRaw = m.Top;
            node.MarginRightRaw = m.Right;
            node.MarginBottomRaw = m.Bottom;
            node.MarginLeftRaw = m.Left;
            node.MarginTop = ResolveEdge(m.Top);
            node.MarginRight = ResolveEdge(m.Right);
            node.MarginBottom = ResolveEdge(m.Bottom);
            node.MarginLeft = ResolveEdge(m.Left);
        }

        // Gap
        if (style.Gap.HasValue)
            node.Gap = style.Gap.Value;

        // Position
        if (style.Position.HasValue)
            node.Position = style.Position.Value switch
            {
                Style.PositionType.Relative => Layout.PositionType.Relative,
                Style.PositionType.Absolute => Layout.PositionType.Absolute,
                Style.PositionType.Fixed => Layout.PositionType.Absolute, // Fixed -> Absolute fallback
                _ => node.Position,
            };

        // Inset (top/right/bottom/left positioning)
        if (style.Inset.HasValue)
        {
            var inset = style.Inset.Value;
            node.PositionTop = ResolvePx(inset.Top);
            node.PositionRight = ResolvePx(inset.Right);
            node.PositionBottom = ResolvePx(inset.Bottom);
            node.PositionLeft = ResolvePx(inset.Left);
        }

        // Overflow
        if (style.Overflow.HasValue)
            node.Overflow = style.Overflow.Value switch
            {
                Style.Overflow.Visible => Layout.Overflow.Visible,
                Style.Overflow.Hidden => Layout.Overflow.Hidden,
                Style.Overflow.Scroll => Layout.Overflow.Scroll,
                _ => node.Overflow,
            };

        // Aspect ratio
        if (style.AspectRatio.HasValue)
            node.AspectRatio = style.AspectRatio.Value;
    }

    /// <summary>
    /// Create a rect from a layout node's computed output.
    /// Returns (x, y, width, height).
    /// </summary>
    public static (float X, float Y, float Width, float Height) ToRect(LayoutNode node)
        => (node.ComputedX, node.ComputedY, node.ComputedWidth, node.ComputedHeight);

    /// <summary>
    /// Resolve a StyleValue to a pixel float. Auto and Percent return NaN.
    /// </summary>
    private static float ResolvePx(StyleValue sv)
    {
        return sv.Unit switch
        {
            StyleUnit.Px => sv.Value,
            _ => float.NaN,
        };
    }

    /// <summary>
    /// Resolve an edge StyleValue to pixels. Percent returns 0 (resolved later during layout).
    /// </summary>
    private static float ResolveEdge(StyleValue sv)
    {
        return sv.Unit switch
        {
            StyleUnit.Px => sv.Value,
            _ => 0f,
        };
    }
}
