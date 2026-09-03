using ReactUI.Style;

namespace ReactUI.Theme;

/// <summary>
/// A shadcn/ui-flavoured theme: the zinc dark palette, its 4px spacing rhythm, and the component
/// classes that go with it. Register once at startup with <see cref="Register"/>, then use these
/// class names via UI.ClassName instead of hand-written inline styles.
/// <para>
/// The values are shadcn's dark tokens converted from HSL to hex, so a panel here reads the same as
/// the web components it is modelled on.
/// </para>
/// <para>
/// Four things to know before using it, all learned from the first panels built on it:
/// </para>
/// <list type="number">
/// <item><description>
/// <b>Class order is load-bearing.</b> Classes merge left to right and the later one wins, so
/// <c>"btn btn-ghost"</c> works and <c>"btn-ghost btn"</c> silently does not. Where two classes set
/// the same property — <c>.btn-sm</c> and <c>.btn-icon</c> both set Height — the last one decides.
/// </description></item>
/// <item><description>
/// <b>Colour does not inherit.</b> A class on a container does not colour a Text child, so the
/// container classes ship <c>-text</c> companions (<c>.badge-text</c>, <c>.btn-text</c>) for the
/// label inside them.
/// </description></item>
/// <item><description>
/// <b><c>.btn-disabled</c> is cosmetic only</b> — it dims and sets the cursor, but the click still
/// fires, which suits panels that want to explain why an action is unavailable. Add
/// <c>.btn-inert</c> as well when the click should genuinely not land.
/// </description></item>
/// <item><description>
/// <b>JSX components do not mix sheets.</b> The JSX bridge resolves a component's local stylesheet
/// wholesale when it looks like it styles the node, so a local <c>.btn</c> hides this one and a
/// className cannot combine a local class with a theme class. Delete the duplicate from the
/// component's CSS and let the theme provide it.
/// </description></item>
/// </list>
public static class Shadcn
{
    // --- Tokens -----------------------------------------------------------------------------

    public static readonly UIColor Background = UIColor.FromHex("#09090b");
    public static readonly UIColor Foreground = UIColor.FromHex("#fafafa");
    public static readonly UIColor Card = UIColor.FromHex("#09090b");
    public static readonly UIColor Popover = UIColor.FromHex("#18181b");
    public static readonly UIColor Primary = UIColor.FromHex("#fafafa");
    public static readonly UIColor PrimaryForeground = UIColor.FromHex("#18181b");
    public static readonly UIColor Secondary = UIColor.FromHex("#27272a");
    public static readonly UIColor SecondaryForeground = UIColor.FromHex("#fafafa");
    public static readonly UIColor Muted = UIColor.FromHex("#27272a");
    public static readonly UIColor MutedForeground = UIColor.FromHex("#a1a1aa");
    public static readonly UIColor Accent = UIColor.FromHex("#27272a");
    public static readonly UIColor AccentForeground = UIColor.FromHex("#fafafa");
    public static readonly UIColor Destructive = UIColor.FromHex("#7f1d1d");
    public static readonly UIColor DestructiveForeground = UIColor.FromHex("#fafafa");
    public static readonly UIColor Border = UIColor.FromHex("#27272a");
    public static readonly UIColor Input = UIColor.FromHex("#27272a");
    public static readonly UIColor Ring = UIColor.FromHex("#d4d4d8");

    /// <summary>Positive counterpart to <see cref="Destructive"/>, for a confirmed/valid state.</summary>
    public static readonly UIColor Success = UIColor.FromHex("#14532d");
    public static readonly UIColor SuccessForeground = UIColor.FromHex("#fafafa");

    /// <summary>
    /// Readable status text on a card. <see cref="Destructive"/> and <see cref="Success"/> are
    /// SURFACE colours — dark enough to carry near-white text on top — so they are unreadable as
    /// text on the card itself. These are the text-weight versions of the same two signals.
    /// </summary>
    public static readonly UIColor DestructiveText = UIColor.FromHex("#f87171");
    public static readonly UIColor SuccessText = UIColor.FromHex("#4ade80");

    /// <summary>
    /// Muted foreground for use ON a primary (light) surface. <see cref="MutedForeground"/> is tuned
    /// for the dark card and washes out on a selected/inverted row.
    /// </summary>
    public static readonly UIColor PrimaryMutedForeground = UIColor.FromHex("#52525b");

    /// <summary>shadcn's --radius (0.5rem); sm and md step down from it as its CSS does.</summary>
    public const float RadiusLg = 8f;
    public const float RadiusMd = 6f;
    public const float RadiusSm = 4f;

    /// <summary>The 4px spacing rhythm the components are built on.</summary>
    public const float Space1 = 4f;
    public const float Space2 = 8f;
    public const float Space3 = 12f;
    public const float Space4 = 16f;
    public const float Space6 = 24f;

    public const float TextXs = 12f;
    public const float TextSm = 14f;
    public const float TextBase = 15f;
    public const float TextLg = 18f;

    public const int WeightNormal = 400;
    public const int WeightMedium = 500;
    public const int WeightSemibold = 600;

    private static bool _registered;

    /// <summary>Registers the theme's classes globally. Safe to call more than once.</summary>
    public static void Register()
    {
        if (_registered) return;
        _registered = true;

        ReactUI.UI.RegisterStyles(BuildSheet());
    }

    public static StyleSheet BuildSheet()
    {
        var s = new StyleSheet();

        // --- Surfaces ---
        s[".card"] = new Style.Style
        {
            Background = Card,
            BorderColor = Border,
            BorderWidth = 1f,
            BorderRadius = RadiusLg,
            FlexDirection = FlexDirection.Column,
            BoxShadow = new BoxShadow
            {
                OffsetX = 0f,
                OffsetY = 4f,
                Blur = 12f,
                Spread = 0f,
                Color = UIColor.FromHex("#00000066"),
            },
        };

        s[".card-header"] = new Style.Style
        {
            FlexDirection = FlexDirection.Column,
            Gap = Space1,
            Padding = new EdgeValues(Space4, Space6),
        };

        s[".card-title"] = new Style.Style
        {
            Color = Foreground,
            FontSize = TextLg,
            FontWeight = WeightSemibold,
        };

        s[".card-description"] = new Style.Style
        {
            Color = MutedForeground,
            FontSize = TextSm,
        };

        s[".card-content"] = new Style.Style
        {
            FlexDirection = FlexDirection.Column,
            Gap = Space3,
            Padding = new EdgeValues(0f, Space6, Space4, Space6),
        };

        s[".card-footer"] = new Style.Style
        {
            FlexDirection = FlexDirection.Row,
            AlignItems = AlignItems.Center,
            Gap = Space2,
            Padding = new EdgeValues(0f, Space6, Space4, Space6),
        };

        s[".popover"] = new Style.Style
        {
            Background = Popover,
            BorderColor = Border,
            BorderWidth = 1f,
            BorderRadius = RadiusMd,
            Padding = new EdgeValues(Space3),
            FlexDirection = FlexDirection.Column,
            Gap = Space2,
        };

        // --- Buttons: default, secondary, outline, ghost, destructive ---
        s[".btn"] = new Style.Style
        {
            FlexDirection = FlexDirection.Row,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            Gap = Space2,
            Height = StyleValue.Px(36f),
            Padding = new EdgeValues(0f, Space4),
            BorderRadius = RadiusMd,
            FontSize = TextSm,
            FontWeight = WeightMedium,
            Cursor = CursorType.Pointer,
            Background = Primary,
            Color = PrimaryForeground,
            // A transparent border on every variant keeps solid and outline buttons the same box
            // size, so they sit flush when mixed in one row.
            BorderWidth = 1f,
            BorderColor = UIColor.Transparent,
            Hover = new Style.Style { Opacity = 0.9f },
            Active = new Style.Style { Opacity = 0.8f },
        };

        /// A button shaped like a list row: content starts at the leading edge and the height comes
        /// from the content rather than the 36px control height.
        s[".btn-row"] = new Style.Style
        {
            JustifyContent = JustifyContent.FlexStart,
            Height = StyleValue.Auto,
            Padding = new EdgeValues(Space2, Space3),
        };

        s[".btn-sm"] = new Style.Style
        {
            Height = StyleValue.Px(32f),
            Padding = new EdgeValues(0f, Space3),
            FontSize = TextXs,
        };

        s[".btn-icon"] = new Style.Style
        {
            Height = StyleValue.Px(32f),
            Width = StyleValue.Px(32f),
            Padding = new EdgeValues(0f),
        };

        s[".btn-secondary"] = new Style.Style
        {
            Background = Secondary,
            Color = SecondaryForeground,
            Hover = new Style.Style { Background = UIColor.FromHex("#3f3f46") },
        };

        s[".btn-outline"] = new Style.Style
        {
            Background = UIColor.Transparent,
            Color = Foreground,
            BorderColor = Border,
            BorderWidth = 1f,
            Hover = new Style.Style { Background = Accent, Color = AccentForeground },
        };

        s[".btn-ghost"] = new Style.Style
        {
            Background = UIColor.Transparent,
            Color = Foreground,
            Hover = new Style.Style { Background = Accent, Color = AccentForeground },
        };

        s[".btn-destructive"] = new Style.Style
        {
            Background = Destructive,
            Color = DestructiveForeground,
            Hover = new Style.Style { Background = UIColor.FromHex("#991b1b") },
        };

        // Cosmetic only: the click still fires, so a panel can explain WHY the action is
        // unavailable. Pair with .btn-inert when the click should not land at all.
        s[".btn-disabled"] = new Style.Style
        {
            Opacity = 0.5f,
            Cursor = CursorType.NotAllowed,
        };

        s[".btn-inert"] = new Style.Style
        {
            PointerEvents = false,
        };

        /// Label inside a composed button. Colour does not inherit from the container, so a Text
        /// child needs this to pick up the button's foreground.
        s[".btn-text"] = new Style.Style
        {
            FontSize = TextSm,
            FontWeight = WeightMedium,
            Color = PrimaryForeground,
        };

        // --- Form + text ---
        s[".input"] = new Style.Style
        {
            Height = StyleValue.Px(36f),
            Padding = new EdgeValues(0f, Space3),
            Background = UIColor.Transparent,
            BorderColor = Input,
            BorderWidth = 1f,
            BorderRadius = RadiusMd,
            Color = Foreground,
            FontSize = TextSm,
            Focus = new Style.Style { BorderColor = Ring },
        };

        s[".input-invalid"] = new Style.Style
        {
            BorderColor = DestructiveText,
            Focus = new Style.Style { BorderColor = DestructiveText },
        };

        s[".input-disabled"] = new Style.Style
        {
            Opacity = 0.5f,
            Cursor = CursorType.NotAllowed,
            PointerEvents = false,
        };

        s[".label"] = new Style.Style
        {
            Color = Foreground,
            FontSize = TextSm,
            FontWeight = WeightMedium,
        };

        /// Sits between .card-title and .muted: a heading for a group inside a card.
        s[".section-title"] = new Style.Style
        {
            Color = Foreground,
            FontSize = TextSm,
            FontWeight = WeightSemibold,
        };

        s[".muted"] = new Style.Style
        {
            Color = MutedForeground,
            FontSize = TextSm,
        };

        /// Muted text ON a primary/selected surface, where .muted would wash out.
        s[".muted-inverted"] = new Style.Style
        {
            Color = PrimaryMutedForeground,
            FontSize = TextSm,
        };

        s[".text-destructive"] = new Style.Style
        {
            Color = DestructiveText,
            FontSize = TextSm,
        };

        s[".text-success"] = new Style.Style
        {
            Color = SuccessText,
            FontSize = TextSm,
        };

        s[".badge"] = new Style.Style
        {
            FlexDirection = FlexDirection.Row,
            AlignItems = AlignItems.Center,
            Padding = new EdgeValues(2f, Space2),
            BorderRadius = 999f,
            Background = Secondary,
            Color = SecondaryForeground,
            FontSize = TextXs,
            FontWeight = WeightMedium,
        };

        s[".badge-destructive"] = new Style.Style
        {
            Background = Destructive,
            Color = DestructiveForeground,
        };

        s[".badge-success"] = new Style.Style
        {
            Background = Success,
            Color = SuccessForeground,
        };

        /// Label inside a badge; colour does not inherit from the badge container.
        s[".badge-text"] = new Style.Style
        {
            FontSize = TextXs,
            FontWeight = WeightMedium,
            Color = SecondaryForeground,
        };

        s[".separator"] = new Style.Style
        {
            Height = StyleValue.Px(1f),
            Background = Border,
        };

        s[".separator-vertical"] = new Style.Style
        {
            Width = StyleValue.Px(1f),
            Height = StyleValue.Auto,
            Background = Border,
        };

        /// Row of actions with no padding of its own, for nesting inside .card-content
        /// (.card-footer bakes in the card's horizontal padding and would double-indent).
        s[".card-actions"] = new Style.Style
        {
            FlexDirection = FlexDirection.Row,
            AlignItems = AlignItems.Center,
            Gap = Space2,
        };

        return s;
    }
}
