using ReactUI.Layout;
using Xunit;

namespace ReactUI.Tests;

/// <summary>
/// A text leaf must receive its container's width as a wrapping constraint even
/// when that container is itself auto-sized (AtMost) inside a fixed-width box:
/// a modal → padded page → column → paragraph chain must wrap the paragraph.
/// </summary>
public class TextWrapLayoutTests
{
    private const float TextWidth = 1200f;
    private const float LineHeight = 20f;

    private static LayoutNode TextLeaf()
    {
        return new LayoutNode
        {
            MeasureFunc = (maxWidth, widthMode, _, _) =>
            {
                float avail = widthMode == MeasureMode.Undefined ? float.MaxValue : maxWidth;
                float fit = System.MathF.Min(TextWidth, avail);
                int lines = fit > 0 && TextWidth > fit ? (int)System.MathF.Ceiling(TextWidth / fit) : 1;
                return (fit, lines * LineHeight);
            },
        };
    }

    [Fact]
    public void Paragraph_WrapsInsideAutoSizedColumnOfFixedWidthBox()
    {
        var root = new LayoutNode();
        var backdrop = new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 0, PositionLeft = 0, PositionRight = 0, PositionBottom = 0,
            AlignItems = AlignItems.Center,
        };
        var box = new LayoutNode { Width = 560, FlexShrink = 0 };
        var page = new LayoutNode { PaddingTop = 14, PaddingRight = 14, PaddingBottom = 14, PaddingLeft = 14, Gap = 12 };
        var column = new LayoutNode { Gap = 10 };
        var text = TextLeaf();

        column.AddChild(text);
        page.AddChild(column);
        box.AddChild(page);
        backdrop.AddChild(box);
        root.AddChild(backdrop);

        YogaLayout.Calculate(root, 1280, 720);

        Assert.True(text.ComputedWidth <= 532.01f, $"text width {text.ComputedWidth} exceeds the 532px content width");
        Assert.True(text.ComputedHeight >= 2 * LineHeight, $"text height {text.ComputedHeight} shows it did not wrap");
        Assert.True(box.ComputedHeight >= text.ComputedHeight + 28, "box did not grow to hold the wrapped text");
    }

    [Fact]
    public void Paragraph_WrapsDirectlyInsideAutoSizedChildOfExactlyRoot()
    {
        var root = new LayoutNode { Width = 400 };
        var column = new LayoutNode();
        var text = TextLeaf();
        column.AddChild(text);
        root.AddChild(column);

        YogaLayout.Calculate(root, 1280, 720);

        Assert.True(text.ComputedWidth <= 400.01f, $"text width {text.ComputedWidth}");
        Assert.True(text.ComputedHeight >= 2 * LineHeight, $"text height {text.ComputedHeight}");
    }
}
