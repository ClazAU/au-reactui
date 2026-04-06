using ReactUI.Style;
using S = ReactUI.Style.Style;

namespace ReactUI.Editor.Components;

/// <summary>
/// Shared styles for the editor UI.
/// </summary>
public static class EditorStyles
{
    public static readonly StyleSheet Sheet = new()
    {
        [".editor-root"] = new S
        {
            Position = PositionType.Absolute,
            Inset = new EdgeValues(30, 30, 30, 30),
            Background = UIColor.FromHex("#0d0d14"),
            BorderRadius = 12,
            BorderWidth = 1,
            BorderColor = UIColor.FromHex("#ffffff10"),
            BoxShadow = new BoxShadow { Blur = 32, Color = UIColor.FromRgba("rgba(0,0,0,0.6)") },
            Overflow = Overflow.Hidden,
        },

        [".toolbar"] = new S
        {
            FlexDirection = FlexDirection.Row,
            Padding = new EdgeValues(8, 12),
            Gap = 8,
            Background = UIColor.FromHex("#16161e"),
            AlignItems = AlignItems.Center,
            BorderColor = UIColor.FromHex("#ffffff08"),
        },

        [".toolbar-title"] = new S
        {
            FontSize = 14,
            FontWeight = 700,
            Color = UIColor.FromHex("#7c3aed"),
            FlexGrow = 1,
            TextAlign = TextAlign.Right,
        },

        [".toolbar-btn"] = new S
        {
            Padding = new EdgeValues(4, 10),
            Background = UIColor.FromHex("#2d2a33"),
            Color = UIColor.FromHex("#c0c0c0"),
            BorderRadius = 4,
            FontSize = 12,
            Cursor = CursorType.Pointer,
            Hover = new S { Background = UIColor.FromHex("#3d3a43") },
        },

        [".toolbar-btn-primary"] = new S
        {
            Background = UIColor.FromHex("#7c3aed"),
            Color = UIColor.FromHex("#ffffff"),
            Hover = new S { Background = UIColor.FromHex("#6d28d9") },
        },

        [".toolbar-btn-active"] = new S
        {
            Background = UIColor.FromHex("#22c55e"),
            Color = UIColor.FromHex("#ffffff"),
        },

        [".main-area"] = new S
        {
            FlexDirection = FlexDirection.Row,
            FlexGrow = 1,
            FlexShrink = 1,
            FlexBasis = StyleValue.Px(0),
            Overflow = Overflow.Hidden,
        },

        [".sidebar"] = new S
        {
            Width = StyleValue.Px(180),
            Background = UIColor.FromHex("#12121a"),
            BorderColor = UIColor.FromHex("#ffffff08"),
            Padding = new EdgeValues(8),
            Gap = 2,
            Overflow = Overflow.Scroll,
        },

        [".sidebar-title"] = new S
        {
            FontSize = 11,
            FontWeight = 600,
            Color = UIColor.FromHex("#666"),
            Padding = new EdgeValues(4, 8),
        },

        [".file-item"] = new S
        {
            Padding = new EdgeValues(4, 8),
            BorderRadius = 4,
            FontSize = 12,
            Color = UIColor.FromHex("#a0a0a0"),
            Cursor = CursorType.Pointer,
            Hover = new S { Background = UIColor.FromHex("#1e1e28"), Color = UIColor.FromHex("#e0e0e0") },
        },

        [".file-item-active"] = new S
        {
            Background = UIColor.FromHex("#7c3aed20"),
            Color = UIColor.FromHex("#c4b5fd"),
        },

        [".file-item-jsx"] = new S { Color = UIColor.FromHex("#fbbf24") },
        [".file-item-css"] = new S { Color = UIColor.FromHex("#38bdf8") },

        [".editor-area"] = new S
        {
            FlexGrow = 1,
            FlexShrink = 1,
            FlexBasis = StyleValue.Px(0),
            FlexDirection = FlexDirection.Column,
            Overflow = Overflow.Hidden,
        },

        [".code-scroll"] = new S
        {
            FlexGrow = 1,
            FlexShrink = 1,
            FlexBasis = StyleValue.Px(0),
            Overflow = Overflow.Scroll,
            Background = UIColor.FromHex("#0d0d14"),
            Padding = new EdgeValues(4, 0),
        },

        [".code-line"] = new S
        {
            FlexDirection = FlexDirection.Row,
            AlignItems = AlignItems.Center,
            MinHeight = StyleValue.Px(22),
        },

        [".line-number"] = new S
        {
            Width = StyleValue.Px(40),
            TextAlign = TextAlign.Right,
            FontSize = 12,
            Color = UIColor.FromHex("#444"),
            Padding = new EdgeValues(0, 8, 0, 0),
        },

        [".line-input"] = new S
        {
            FlexGrow = 1,
            FontSize = 13,
            Color = UIColor.FromHex("#d4d4d4"),
            Background = UIColor.Transparent,
            Padding = new EdgeValues(2, 4),
            BorderWidth = 0,
            Focus = new S { Background = UIColor.FromHex("#ffffff08") },
        },

        [".line-input-focused"] = new S
        {
            Background = UIColor.FromHex("#ffffff05"),
        },

        [".error-panel"] = new S
        {
            Background = UIColor.FromHex("#2d1515"),
            Padding = new EdgeValues(8, 12),
            BorderColor = UIColor.FromHex("#ef4444"),
            MaxHeight = StyleValue.Px(80),
            Overflow = Overflow.Scroll,
        },

        [".error-text"] = new S
        {
            FontSize = 12,
            Color = UIColor.FromHex("#fca5a5"),
        },

        [".status-bar"] = new S
        {
            FlexDirection = FlexDirection.Row,
            Padding = new EdgeValues(4, 12),
            Background = UIColor.FromHex("#16161e"),
            AlignItems = AlignItems.Center,
            Gap = 12,
        },

        [".status-text"] = new S
        {
            FontSize = 11,
            Color = UIColor.FromHex("#666"),
        },
    };
}
