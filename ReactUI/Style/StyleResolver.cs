namespace ReactUI.Style;

/// <summary>
/// Resolves the final computed style for an element by applying:
/// defaults -> inherited text properties -> element style -> pseudo-state overlays.
/// </summary>
public static class StyleResolver
{
    /// <summary>
    /// Resolves the fully computed style for an element.
    /// </summary>
    public static Style Resolve(
        Style? elementStyle,
        Style? parentComputed,
        bool isHovered,
        bool isActive,
        bool isFocused)
    {
        var result = DefaultStyle();

        // Inherit text properties from parent
        if (parentComputed != null)
            InheritTextProps(result, parentComputed);

        // Merge element style
        if (elementStyle != null)
            result = result.Merge(elementStyle);

        // Apply pseudo-states in order: hover, active, focus
        if (isHovered && result.Hover != null)
            result = result.Merge(result.Hover);
        if (isActive && result.Active != null)
            result = result.Merge(result.Active);
        if (isFocused && result.Focus != null)
            result = result.Merge(result.Focus);

        return result;
    }

    /// <summary>
    /// Creates a Style with sensible defaults matching CSS initial values.
    /// </summary>
    public static Style DefaultStyle() => new()
    {
        // Layout defaults
        FlexDirection = FlexDirection.Column,
        JustifyContent = JustifyContent.FlexStart,
        AlignItems = AlignItems.Stretch,
        AlignSelf = AlignSelf.Auto,
        FlexWrap = FlexWrap.NoWrap,
        FlexGrow = 0f,
        FlexShrink = 1f,
        FlexBasis = StyleValue.Auto,
        Position = PositionType.Relative,
        Overflow = Overflow.Visible,

        // Visual defaults
        Background = UIColor.Transparent,
        BorderRadius = 0f,
        BorderWidth = 0f,
        BorderColor = UIColor.Transparent,
        Opacity = 1f,
        BackdropBlur = 0f,
        ZIndex = 0,

        // Text defaults
        Color = UIColor.White,
        FontSize = 14f,
        FontWeight = 400,
        TextAlign = TextAlign.Left,
        LineHeight = 1.4f,

        // Interaction defaults
        Cursor = CursorType.Default,
        PointerEvents = true,
    };

    /// <summary>
    /// Copies inheritable text properties from parent to child.
    /// Only copies if the child's property is not already set.
    /// Inherited properties: Color, FontSize, FontFamily, LineHeight, FontWeight, TextAlign.
    /// </summary>
    private static void InheritTextProps(Style child, Style parent)
    {
        // These properties inherit per CSS spec
        if (parent.Color.HasValue)
            child.Color = parent.Color;
        if (parent.FontSize.HasValue)
            child.FontSize = parent.FontSize;
        if (parent.FontFamily != null)
            child.FontFamily = parent.FontFamily;
        if (parent.LineHeight.HasValue)
            child.LineHeight = parent.LineHeight;
        if (parent.FontWeight.HasValue)
            child.FontWeight = parent.FontWeight;
        if (parent.TextAlign.HasValue)
            child.TextAlign = parent.TextAlign;
    }
}
