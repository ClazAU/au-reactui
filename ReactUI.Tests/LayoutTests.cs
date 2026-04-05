using System.Reflection;
using ReactUI.Layout;
using Xunit;

namespace ReactUI.Tests;

public class LayoutTests
{
    private const float Tolerance = 0.01f;

    private static void AssertApprox(float expected, float actual, string label = "")
    {
        Assert.True(
            System.MathF.Abs(expected - actual) <= Tolerance,
            $"{label} expected {expected} but was {actual}");
    }

    private static void CalculateUndefined(LayoutNode node)
    {
        var method = typeof(YogaLayout).GetMethod(
            "LayoutInternal",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        method!.Invoke(null, new object[]
        {
            node,
            float.NaN,
            float.NaN,
            MeasureMode.Undefined,
            MeasureMode.Undefined
        });
    }

    // 1. Single node with no explicit size fills available space (Exactly mode)
    [Fact]
    public void SingleNode_FillsAvailableSpace()
    {
        var root = new LayoutNode();
        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(800, root.ComputedWidth, "Width");
        // Root with MeasureMode.Exactly and no children resolves height to available
        AssertApprox(600, root.ComputedHeight, "Height");
    }

    // 2. Fixed size node
    [Fact]
    public void FixedSizeNode()
    {
        var root = new LayoutNode { Width = 200, Height = 100 };
        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.ComputedWidth, "Width");
        AssertApprox(100, root.ComputedHeight, "Height");
    }

    // 3. Column layout (default) - children stacked vertically
    [Fact]
    public void ColumnLayout_ChildrenStackedVertically()
    {
        var root = new LayoutNode { Width = 200, Height = 300 };
        for (int i = 0; i < 3; i++)
            root.AddChild(new LayoutNode { Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(0, root.Children[0].ComputedY - root.ComputedY, "Child0 Y");
        AssertApprox(50, root.Children[1].ComputedY - root.ComputedY, "Child1 Y");
        AssertApprox(100, root.Children[2].ComputedY - root.ComputedY, "Child2 Y");
    }

    // 4. Row layout - children side by side
    [Fact]
    public void RowLayout_ChildrenSideBySide()
    {
        var root = new LayoutNode { FlexDirection = FlexDirection.Row, Width = 400, Height = 100 };
        for (int i = 0; i < 3; i++)
            root.AddChild(new LayoutNode { Width = 100 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(0, root.Children[0].ComputedX - root.ComputedX, "Child0 X");
        AssertApprox(100, root.Children[1].ComputedX - root.ComputedX, "Child1 X");
        AssertApprox(200, root.Children[2].ComputedX - root.ComputedX, "Child2 X");
    }

    // 5. Padding offsets child position
    [Fact]
    public void Padding_OffsetsChildPosition()
    {
        var root = new LayoutNode
        {
            Width = 300, Height = 200,
            PaddingTop = 10, PaddingLeft = 20
        };
        root.AddChild(new LayoutNode { Width = 50, Height = 30 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(20, root.Children[0].ComputedX - root.ComputedX, "Child X");
        AssertApprox(10, root.Children[0].ComputedY - root.ComputedY, "Child Y");
    }

    // 6. MarginTop pushes child down
    [Fact]
    public void MarginTop_PushesChildDown()
    {
        var root = new LayoutNode { Width = 200, Height = 200 };
        root.AddChild(new LayoutNode { Height = 30, MarginTop = 15 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(15, root.Children[0].ComputedY - root.ComputedY, "Child Y");
    }

    // 7. Gap between children in column
    [Fact]
    public void Gap_SpacesChildrenInColumn()
    {
        var root = new LayoutNode { Width = 200, Height = 300, Gap = 10 };
        for (int i = 0; i < 3; i++)
            root.AddChild(new LayoutNode { Height = 30 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(0, root.Children[0].ComputedY - root.ComputedY, "Child0 Y");
        AssertApprox(40, root.Children[1].ComputedY - root.ComputedY, "Child1 Y");
        AssertApprox(80, root.Children[2].ComputedY - root.ComputedY, "Child2 Y");
    }

    // 8. flex-grow distributes space proportionally
    [Fact]
    public void FlexGrow_DistributesSpaceProportionally()
    {
        var root = new LayoutNode { FlexDirection = FlexDirection.Row, Width = 300, Height = 100 };
        root.AddChild(new LayoutNode { FlexGrow = 1 });
        root.AddChild(new LayoutNode { FlexGrow = 2 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(100, root.Children[0].ComputedWidth, "Child0 Width");
        AssertApprox(200, root.Children[1].ComputedWidth, "Child1 Width");
    }

    // 9. flex-shrink distributes overflow
    [Fact]
    public void FlexShrink_DistributesOverflow()
    {
        var root = new LayoutNode { FlexDirection = FlexDirection.Row, Width = 100, Height = 50 };
        root.AddChild(new LayoutNode { Width = 80, FlexShrink = 1 });
        root.AddChild(new LayoutNode { Width = 80, FlexShrink = 1 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(50, root.Children[0].ComputedWidth, "Child0 Width");
        AssertApprox(50, root.Children[1].ComputedWidth, "Child1 Width");
    }

    // 10. JustifyContent.Center centers child on main axis
    [Fact]
    public void JustifyContent_Center()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 100,
            JustifyContent = JustifyContent.Center
        };
        root.AddChild(new LayoutNode { Width = 100, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(100, root.Children[0].ComputedX - root.ComputedX, "Child X");
    }

    // 11. JustifyContent.SpaceBetween spreads children
    [Fact]
    public void JustifyContent_SpaceBetween()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 100,
            JustifyContent = JustifyContent.SpaceBetween
        };
        for (int i = 0; i < 3; i++)
            root.AddChild(new LayoutNode { Width = 50, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        AssertApprox(0, root.Children[0].ComputedX - rootX, "Child0 X");
        AssertApprox(125, root.Children[1].ComputedX - rootX, "Child1 X");
        AssertApprox(250, root.Children[2].ComputedX - rootX, "Child2 X");
    }

    // 12. JustifyContent.FlexEnd pushes to end
    [Fact]
    public void JustifyContent_FlexEnd()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 100,
            JustifyContent = JustifyContent.FlexEnd
        };
        root.AddChild(new LayoutNode { Width = 100, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.Children[0].ComputedX - root.ComputedX, "Child X");
    }

    // 13. AlignItems.Center centers on cross axis
    [Fact]
    public void AlignItems_Center()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 200,
            AlignItems = AlignItems.Center
        };
        root.AddChild(new LayoutNode { Width = 100, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(75, root.Children[0].ComputedY - root.ComputedY, "Child Y");
    }

    // 14. AlignItems.Stretch stretches child on cross axis
    [Fact]
    public void AlignItems_Stretch()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 200,
            AlignItems = AlignItems.Stretch
        };
        root.AddChild(new LayoutNode { Width = 100 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.Children[0].ComputedHeight, "Child Height");
    }

    // 15. Position absolute
    [Fact]
    public void PositionAbsolute()
    {
        var root = new LayoutNode { Width = 300, Height = 300 };
        root.AddChild(new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 10, PositionLeft = 20,
            Width = 50, Height = 50
        });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        float rootY = root.ComputedY;
        AssertApprox(rootX + 20, root.Children[0].ComputedX, "Child X");
        AssertApprox(rootY + 10, root.Children[0].ComputedY, "Child Y");
    }

    // 16. Display none - hidden child does not occupy space
    [Fact]
    public void DisplayNone_SkipsChild()
    {
        var root = new LayoutNode { Width = 200, Height = 300 };
        root.AddChild(new LayoutNode { Height = 30 });
        root.AddChild(new LayoutNode { Height = 30, Display = false });
        root.AddChild(new LayoutNode { Height = 30 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Child0 Y");
        // Child2 should be at Y=30, not 60, because child1 is hidden
        AssertApprox(30, root.Children[2].ComputedY - rootY, "Child2 Y");
    }

    // 17. Flex wrap - wraps to next line
    [Fact]
    public void FlexWrap_WrapsToNextLine()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200, Height = 200
        };
        for (int i = 0; i < 3; i++)
            root.AddChild(new LayoutNode { Width = 100, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        float rootY = root.ComputedY;

        // First line: children 0 and 1
        AssertApprox(0, root.Children[0].ComputedX - rootX, "Child0 X");
        AssertApprox(100, root.Children[1].ComputedX - rootX, "Child1 X");
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Child0 Y (line 0)");
        AssertApprox(0, root.Children[1].ComputedY - rootY, "Child1 Y (line 0)");

        // Second line: child 2
        AssertApprox(0, root.Children[2].ComputedX - rootX, "Child2 X");
        AssertApprox(50, root.Children[2].ComputedY - rootY, "Child2 Y (line 1)");
    }

    // 18. MeasureFunc for leaf nodes (AlignSelf prevents stretch override)
    [Fact]
    public void MeasureFunc_DeterminesLeafSize()
    {
        var root = new LayoutNode { Width = 200, AlignItems = AlignItems.FlexStart };
        var leaf = new LayoutNode
        {
            MeasureFunc = (w, wm, h, hm) => (80, 20)
        };
        root.AddChild(leaf);

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(80, leaf.ComputedWidth, "Leaf Width");
        AssertApprox(20, leaf.ComputedHeight, "Leaf Height");
    }

    // 19. Min/Max constraints
    [Fact]
    public void MaxWidth_Constrains()
    {
        var root = new LayoutNode { Width = 500, MaxWidth = 200 };
        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.ComputedWidth, "Width");
    }

    [Fact]
    public void MinWidth_Constrains()
    {
        var root = new LayoutNode { Width = 50, MinWidth = 100 };
        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(100, root.ComputedWidth, "Width");
    }

    // 20. Nested layout: outer row containing inner column with children
    [Fact]
    public void NestedLayout_RowContainingColumn()
    {
        var outer = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            Width = 400, Height = 200
        };
        var inner = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            Width = 150
        };
        inner.AddChild(new LayoutNode { Height = 40 });
        inner.AddChild(new LayoutNode { Height = 60 });
        outer.AddChild(inner);

        YogaLayout.Calculate(outer, 800, 600);

        AssertApprox(150, inner.ComputedWidth, "Inner Width");
        // Inner column children stacked
        float innerY = inner.ComputedY;
        AssertApprox(0, inner.Children[0].ComputedY - innerY, "InnerChild0 Y");
        AssertApprox(40, inner.Children[1].ComputedY - innerY, "InnerChild1 Y");
    }

    // 21. Auto width resolves from widest child in a column container
    [Fact]
    public void AutoWidth_ColumnContainer_UsesWidestChild()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            Height = 200
        };
        root.AddChild(new LayoutNode { Width = 90, Height = 20 });
        root.AddChild(new LayoutNode { Width = 140, Height = 20 });

        CalculateUndefined(root);

        AssertApprox(140, root.ComputedWidth, "Root Width");
    }

    // 22. Auto height resolves from stacked children in a column container
    [Fact]
    public void AutoHeight_ColumnContainer_UsesChildrenHeightAndGap()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            Width = 200,
            Gap = 8
        };
        root.AddChild(new LayoutNode { Height = 30 });
        root.AddChild(new LayoutNode { Height = 50 });

        CalculateUndefined(root);

        AssertApprox(88, root.ComputedHeight, "Root Height");
    }

    // 23. Auto width row container resolves from total child widths + gaps
    [Fact]
    public void AutoWidth_RowContainer_UsesChildrenWidthAndGap()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            Height = 80,
            Gap = 5
        };
        root.AddChild(new LayoutNode { Width = 50, Height = 20 });
        root.AddChild(new LayoutNode { Width = 70, Height = 20 });

        CalculateUndefined(root);

        AssertApprox(125, root.ComputedWidth, "Root Width");
    }

    // 24. Auto-size container uses measured leaf dimensions
    [Fact]
    public void AutoSize_UsesMeasuredLeafSize()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            AlignItems = AlignItems.FlexStart
        };
        root.AddChild(new LayoutNode
        {
            MeasureFunc = (w, wm, h, hm) => (73, 19)
        });

        CalculateUndefined(root);

        AssertApprox(73, root.ComputedWidth, "Root Width");
        AssertApprox(19, root.ComputedHeight, "Root Height");
    }

    // 25. ScreenRect positions should accumulate parent offsets
    [Fact]
    public void LayoutEngine_AccumulatesParentOffsetsForScreenRect()
    {
        var root = new ReactUI.Core.UINode("div")
        {
            ComputedStyle = new ReactUI.Style.Style
            {
                Width = ReactUI.Style.StyleValue.Px(300),
                Height = ReactUI.Style.StyleValue.Px(300),
                Padding = new ReactUI.Style.EdgeValues { Left = 10, Top = 20 }
            }
        };

        var child = new ReactUI.Core.UINode("div")
        {
            Parent = root,
            ComputedStyle = new ReactUI.Style.Style
            {
                Width = ReactUI.Style.StyleValue.Px(100),
                Height = ReactUI.Style.StyleValue.Px(100),
                Margin = new ReactUI.Style.EdgeValues { Left = 15, Top = 25 }
            }
        };

        root.Children.Add(child);

        LayoutEngine.ComputeLayout(root, 800, 600);

        AssertApprox(25, child.ScreenRect.X, "Child ScreenRect.X");
        AssertApprox(45, child.ScreenRect.Y, "Child ScreenRect.Y");
        AssertApprox(100, child.ScreenRect.Width, "Child ScreenRect.Width");
        AssertApprox(100, child.ScreenRect.Height, "Child ScreenRect.Height");
    }
}
