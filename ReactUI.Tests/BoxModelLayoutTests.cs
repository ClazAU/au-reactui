using ReactUI.Layout;
using ReactUI.Style;
using Xunit;
using S = ReactUI.Style.Style;
using LayoutNode = ReactUI.Layout.LayoutNode;
using FlexDirection = ReactUI.Layout.FlexDirection;

namespace ReactUI.Tests;

public class BoxModelLayoutTests
{
    private const float Tolerance = 0.01f;

    private static LayoutNode Node(S style)
    {
        var node = new LayoutNode();
        LayoutBridge.ApplyStyle(node, style);
        return node;
    }

    [Fact]
    public void Border_InsetsChildrenLikePadding()
    {
        var box = Node(new S { Width = 200, Height = 100, BorderWidth = 6, Padding = new EdgeValues(10) });
        var child = Node(new S { FlexGrow = 1 });
        box.AddChild(child);

        YogaLayout.Calculate(box, 800, 600);

        Assert.InRange(child.ComputedX, 16 - Tolerance, 16 + Tolerance);
        Assert.InRange(child.ComputedY, 16 - Tolerance, 16 + Tolerance);
        Assert.InRange(child.ComputedWidth, 168 - Tolerance, 168 + Tolerance);
        Assert.InRange(child.ComputedHeight, 68 - Tolerance, 68 + Tolerance);
    }

    [Fact]
    public void Border_WithoutPadding_StillInsetsChildren()
    {
        var box = Node(new S { Width = 200, Height = 100, BorderWidth = 4 });
        var child = Node(new S { FlexGrow = 1 });
        box.AddChild(child);

        YogaLayout.Calculate(box, 800, 600);

        Assert.InRange(child.ComputedY, 4 - Tolerance, 4 + Tolerance);
        Assert.InRange(child.ComputedHeight, 92 - Tolerance, 92 + Tolerance);
    }

    [Fact]
    public void Border_GrowsAnAutoSizedBox()
    {
        var box = Node(new S { FlexDirection = ReactUI.Style.FlexDirection.Row, BorderWidth = 5 });
        box.AddChild(Node(new S { Width = 40, Height = 30 }));
        var root = new LayoutNode { Width = 400, Height = 300, AlignItems = ReactUI.Layout.AlignItems.FlexStart };
        root.AddChild(box);

        YogaLayout.Calculate(root, 800, 600);

        Assert.InRange(box.ComputedWidth, 50 - Tolerance, 50 + Tolerance);
        Assert.InRange(box.ComputedHeight, 40 - Tolerance, 40 + Tolerance);
    }

    [Fact]
    public void PaddedLeaf_IsMeasuredAgainstItsContentWidth()
    {
        float offeredWidth = float.NaN;
        var leaf = new LayoutNode { PaddingLeft = 20, PaddingRight = 20 };
        leaf.MeasureFunc = (maxWidth, _, _, _) =>
        {
            offeredWidth = maxWidth;
            return (maxWidth, 10);
        };
        var column = new LayoutNode { Width = 200, Height = 100, FlexDirection = FlexDirection.Column };
        column.AddChild(leaf);

        YogaLayout.Calculate(column, 800, 600);

        Assert.InRange(offeredWidth, 160 - Tolerance, 160 + Tolerance);
        Assert.InRange(leaf.ComputedWidth, 200 - Tolerance, 200 + Tolerance);
    }
}
