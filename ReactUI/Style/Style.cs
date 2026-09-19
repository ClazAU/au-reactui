namespace ReactUI.Style;

// Layout enums
public enum FlexDirection { Row, RowReverse, Column, ColumnReverse }
public enum JustifyContent { FlexStart, FlexEnd, Center, SpaceBetween, SpaceAround, SpaceEvenly }
public enum AlignItems { FlexStart, FlexEnd, Center, Stretch, Baseline }
public enum AlignSelf { Auto, FlexStart, FlexEnd, Center, Stretch, Baseline }
public enum FlexWrap { NoWrap, Wrap, WrapReverse }
public enum PositionType { Relative, Absolute, Fixed }
public enum Overflow { Visible, Hidden, Scroll }

// Text enums
public enum TextAlign { Left, Center, Right, Justify }

// Image enums
public enum ObjectFit { Fill, Contain, Cover, None, ScaleDown }

// Interaction enums
public enum CursorType { Default, Pointer, Text, Grab, Grabbing, NotAllowed, None }

/// <summary>
/// CSS-like style definition. All properties are nullable; null means "not set" / inherit from defaults.
/// This is a class (not struct) because it contains self-referential nullable fields (Hover, Active, Focus).
/// </summary>
public class Style
{
    // --- Layout ---
    public FlexDirection? FlexDirection;
    public JustifyContent? JustifyContent;
    public AlignItems? AlignItems;
    public AlignSelf? AlignSelf;
    public FlexWrap? FlexWrap;
    public float? FlexGrow;
    public float? FlexShrink;
    public StyleValue? FlexBasis;
    public StyleValue? Width;
    public StyleValue? Height;
    public StyleValue? MinWidth;
    public StyleValue? MinHeight;
    public StyleValue? MaxWidth;
    public StyleValue? MaxHeight;
    public EdgeValues? Padding;
    public EdgeValues? Margin;
    public float? Gap;
    public PositionType? Position;
    public EdgeValues? Inset;
    public Overflow? Overflow;
    public float? AspectRatio;

    // --- Visual ---
    public UIColor? Background;
    public Gradient? BackgroundGradient;
    public float? BorderRadius;
    public CornerRadius? BorderRadii;
    public UIColor? BorderColor;
    public float? BorderWidth;
    public BoxShadow? BoxShadow;
    public float? Opacity;
    public float? BackdropBlur;
    public int? ZIndex;

    // --- Text ---
    public UIColor? Color;
    public float? FontSize;
    public int? FontWeight;
    public TextAlign? TextAlign;
    public float? LineHeight;
    public string? FontFamily;

    // --- Image ---
    public ObjectFit? ObjectFit;

    /// <summary>Draws the text again in this colour around the glyphs. Inherited by child text like Color.</summary>
    public UIColor? TextOutlineColor;

    /// <summary>Outline thickness in logical pixels; defaults to 2 when only the colour is set.</summary>
    public float? TextOutlineWidth;

    /// <summary>
    /// Drawn as part of the element's own background: over <see cref="Background"/>, inside the border and its
    /// rounded corners, under every child. Fitted with <see cref="ObjectFit"/> (Cover by default) and coloured
    /// by <see cref="ImageTint"/>.
    /// </summary>
    public UnityEngine.Texture2D? BackgroundImage;

    /// <summary>Multiplies an image's pixels; black keeps the alpha and gives a silhouette.</summary>
    public UIColor? ImageTint;

    // --- Interaction ---
    public CursorType? Cursor;
    public bool? PointerEvents;

    // --- Pseudo-states (self-referential) ---
    public Style? Hover;
    public Style? Active;
    public Style? Focus;

    // --- Transitions ---
    public Transition[]? Transitions;

    /// <summary>
    /// Returns a new Style with overlay's defined (non-null) values winning over this style's values.
    /// </summary>
    public Style Merge(Style overlay)
    {
        if (overlay == null) return this;

        return new Style
        {
            // Layout
            FlexDirection = overlay.FlexDirection ?? FlexDirection,
            JustifyContent = overlay.JustifyContent ?? JustifyContent,
            AlignItems = overlay.AlignItems ?? AlignItems,
            AlignSelf = overlay.AlignSelf ?? AlignSelf,
            FlexWrap = overlay.FlexWrap ?? FlexWrap,
            FlexGrow = overlay.FlexGrow ?? FlexGrow,
            FlexShrink = overlay.FlexShrink ?? FlexShrink,
            FlexBasis = overlay.FlexBasis ?? FlexBasis,
            Width = overlay.Width ?? Width,
            Height = overlay.Height ?? Height,
            MinWidth = overlay.MinWidth ?? MinWidth,
            MinHeight = overlay.MinHeight ?? MinHeight,
            MaxWidth = overlay.MaxWidth ?? MaxWidth,
            MaxHeight = overlay.MaxHeight ?? MaxHeight,
            Padding = overlay.Padding ?? Padding,
            Margin = overlay.Margin ?? Margin,
            Gap = overlay.Gap ?? Gap,
            Position = overlay.Position ?? Position,
            Inset = overlay.Inset ?? Inset,
            Overflow = overlay.Overflow ?? Overflow,
            AspectRatio = overlay.AspectRatio ?? AspectRatio,

            // Visual
            Background = overlay.Background ?? Background,
            BackgroundGradient = overlay.BackgroundGradient ?? BackgroundGradient,
            BorderRadius = overlay.BorderRadius ?? BorderRadius,
            BorderRadii = overlay.BorderRadii ?? BorderRadii,
            BorderColor = overlay.BorderColor ?? BorderColor,
            BorderWidth = overlay.BorderWidth ?? BorderWidth,
            BoxShadow = overlay.BoxShadow ?? BoxShadow,
            Opacity = overlay.Opacity ?? Opacity,
            BackdropBlur = overlay.BackdropBlur ?? BackdropBlur,
            ZIndex = overlay.ZIndex ?? ZIndex,

            // Text
            Color = overlay.Color ?? Color,
            FontSize = overlay.FontSize ?? FontSize,
            FontWeight = overlay.FontWeight ?? FontWeight,
            TextAlign = overlay.TextAlign ?? TextAlign,
            LineHeight = overlay.LineHeight ?? LineHeight,
            FontFamily = overlay.FontFamily ?? FontFamily,

            // Image
            ObjectFit = overlay.ObjectFit ?? ObjectFit,
            ImageTint = overlay.ImageTint ?? ImageTint,
            BackgroundImage = overlay.BackgroundImage ?? BackgroundImage,
            TextOutlineColor = overlay.TextOutlineColor ?? TextOutlineColor,
            TextOutlineWidth = overlay.TextOutlineWidth ?? TextOutlineWidth,

            // Interaction
            Cursor = overlay.Cursor ?? Cursor,
            PointerEvents = overlay.PointerEvents ?? PointerEvents,

            // Pseudo-states: overlay wins entirely if set
            Hover = overlay.Hover ?? Hover,
            Active = overlay.Active ?? Active,
            Focus = overlay.Focus ?? Focus,

            // Transitions
            Transitions = overlay.Transitions ?? Transitions,
        };
    }
}
