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

    // 26. __component wrapper is transparent in layout
    [Fact]
    public void ComponentWrapper_IsLayoutTransparent()
    {
        // Simulate: __component root wrapping an absolute-positioned panel
        var component = new ReactUI.Core.UINode("__component")
        {
            ComputedStyle = new ReactUI.Style.Style()
        };
        var panel = new ReactUI.Core.UINode("div")
        {
            Parent = component,
            ComputedStyle = new ReactUI.Style.Style
            {
                Position = ReactUI.Style.PositionType.Absolute,
                Inset = new ReactUI.Style.EdgeValues(40, float.NaN, float.NaN, 40),
                Width = ReactUI.Style.StyleValue.Px(420),
                Height = ReactUI.Style.StyleValue.Px(300),
            }
        };
        component.Children.Add(panel);

        LayoutEngine.ComputeLayout(component, 1920, 1080);

        // Panel should be at (40, 40) with its explicit size
        AssertApprox(40, panel.ScreenRect.X, "Panel X");
        AssertApprox(40, panel.ScreenRect.Y, "Panel Y");
        AssertApprox(420, panel.ScreenRect.Width, "Panel Width");
        AssertApprox(300, panel.ScreenRect.Height, "Panel Height");
    }

    // 27. Auto-height absolute panel sizes to content
    [Fact]
    public void AbsolutePanel_AutoHeight_SizesToContent()
    {
        var component = new ReactUI.Core.UINode("__component")
        {
            ComputedStyle = new ReactUI.Style.Style()
        };
        var panel = new ReactUI.Core.UINode("div")
        {
            Parent = component,
            ComputedStyle = new ReactUI.Style.Style
            {
                Position = ReactUI.Style.PositionType.Absolute,
                Inset = new ReactUI.Style.EdgeValues(40, float.NaN, float.NaN, 40),
                Width = ReactUI.Style.StyleValue.Px(420),
                // No Height — should auto-size to content
                Padding = new ReactUI.Style.EdgeValues(10),
            }
        };
        component.Children.Add(panel);

        // Add two children: 50px and 80px tall with 12px gap
        var child1 = new ReactUI.Core.UINode("div")
        {
            Parent = panel,
            ComputedStyle = new ReactUI.Style.Style
            {
                Height = ReactUI.Style.StyleValue.Px(50),
            }
        };
        var child2 = new ReactUI.Core.UINode("div")
        {
            Parent = panel,
            ComputedStyle = new ReactUI.Style.Style
            {
                Height = ReactUI.Style.StyleValue.Px(80),
                Gap = 12,
            }
        };
        panel.Children.Add(child1);
        panel.Children.Add(child2);
        panel.ComputedStyle.Gap = 12;

        LayoutEngine.ComputeLayout(component, 1920, 1080);

        // Panel height = padding(10) + child1(50) + gap(12) + child2(80) + padding(10) = 162
        AssertApprox(162, panel.ScreenRect.Height, "Panel auto height");
        AssertApprox(420, panel.ScreenRect.Width, "Panel width");
        AssertApprox(40, panel.ScreenRect.X, "Panel X");
        AssertApprox(40, panel.ScreenRect.Y, "Panel Y");
    }

    // 28. Nested __component wrappers are all transparent
    [Fact]
    public void NestedComponents_AllTransparent()
    {
        // Root __component → panel div → inner __component → content div
        var rootComp = new ReactUI.Core.UINode("__component")
        {
            ComputedStyle = new ReactUI.Style.Style()
        };
        var panel = new ReactUI.Core.UINode("div")
        {
            Parent = rootComp,
            ComputedStyle = new ReactUI.Style.Style
            {
                Width = ReactUI.Style.StyleValue.Px(400),
                Height = ReactUI.Style.StyleValue.Px(300),
                Padding = new ReactUI.Style.EdgeValues(10),
            }
        };
        rootComp.Children.Add(panel);

        var innerComp = new ReactUI.Core.UINode("__component")
        {
            Parent = panel,
            ComputedStyle = new ReactUI.Style.Style()
        };
        panel.Children.Add(innerComp);

        var content = new ReactUI.Core.UINode("div")
        {
            Parent = innerComp,
            ComputedStyle = new ReactUI.Style.Style
            {
                Height = ReactUI.Style.StyleValue.Px(100),
            }
        };
        innerComp.Children.Add(content);

        LayoutEngine.ComputeLayout(rootComp, 1920, 1080);

        // Content should be at panel position + padding, stretched to panel width - padding
        AssertApprox(10, content.ScreenRect.X, "Content X");
        AssertApprox(10, content.ScreenRect.Y, "Content Y");
        AssertApprox(380, content.ScreenRect.Width, "Content Width (stretched)");
        AssertApprox(100, content.ScreenRect.Height, "Content Height");

        // Inner component wrapper should have same rect as its content
        AssertApprox(content.ScreenRect.X, innerComp.ScreenRect.X, "InnerComp X matches content");
        AssertApprox(content.ScreenRect.Y, innerComp.ScreenRect.Y, "InnerComp Y matches content");
    }

    // 29. Button with text child has correct height from measure func
    [Fact]
    public void ButtonWithText_HasCorrectHeight()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            Width = 400,
            Gap = 8,
        };

        // Simulate a button: auto-sized with padding, containing a measured text leaf
        var button = new LayoutNode
        {
            PaddingTop = 6, PaddingBottom = 6,
            PaddingLeft = 14, PaddingRight = 14,
        };
        var textLeaf = new LayoutNode
        {
            MeasureFunc = (w, wm, h, hm) =>
            {
                float fontSize = 13f;
                float charWidth = fontSize * 0.5f;
                string text = "Click";
                float textWidth = text.Length * charWidth;
                float availW = wm == MeasureMode.Undefined ? float.MaxValue : w;
                float fitWidth = System.Math.Min(textWidth, availW);
                int lines = fitWidth > 0 ? (int)System.Math.Ceiling(textWidth / fitWidth) : 1;
                float fitHeight = lines * fontSize * 1.4f;
                return (fitWidth, fitHeight);
            }
        };
        button.AddChild(textLeaf);
        root.AddChild(button);

        // Use Undefined mode so the row auto-sizes (mimics being inside a column parent)
        CalculateUndefined(root);

        // Text: "Click" = 5 chars * 6.5 = 32.5 wide, 13*1.4 = 18.2 tall
        // Button: 32.5 + 28 = 60.5 wide, 18.2 + 12 = 30.2 tall
        AssertApprox(60.5f, button.ComputedWidth, "Button Width");
        AssertApprox(30.2f, button.ComputedHeight, "Button Height");
        Assert.True(textLeaf.ComputedHeight > 0, "Text height should not be zero");
    }

    // 30. Column auto-height with multiple auto-height children
    [Fact]
    public void ColumnAutoHeight_MultipleAutoChildren()
    {
        var root = new LayoutNode { Width = 300, Gap = 10 };

        // Child 1: auto-height with text leaf
        var child1 = new LayoutNode { PaddingTop = 16, PaddingBottom = 16, PaddingLeft = 16, PaddingRight = 16 };
        child1.AddChild(new LayoutNode
        {
            MeasureFunc = (w, wm, h, hm) => (100, 30)
        });
        root.AddChild(child1);

        // Child 2: fixed height
        root.AddChild(new LayoutNode { Height = 50 });

        CalculateUndefined(root);

        // Root height = child1(30 + 32 padding = 62) + gap(10) + child2(50) = 122
        AssertApprox(122, root.ComputedHeight, "Root auto height");
    }

    // ══════════════════════════════════════════════
    //  Additional layout tests
    // ══════════════════════════════════════════════

    // 31. JustifyContent.SpaceAround distributes space around items
    [Fact]
    public void JustifyContent_SpaceAround()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 100,
            JustifyContent = JustifyContent.SpaceAround
        };
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        // Free space = 300 - 100 = 200. Each item gets 200/2 = 100 around it.
        // Leading = 50, between = 100
        AssertApprox(50, root.Children[0].ComputedX - rootX, "Child0 X");
        AssertApprox(200, root.Children[1].ComputedX - rootX, "Child1 X");
    }

    // 32. JustifyContent.SpaceEvenly distributes space evenly
    [Fact]
    public void JustifyContent_SpaceEvenly()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 100,
            JustifyContent = JustifyContent.SpaceEvenly
        };
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        // Free space = 300 - 100 = 200. Slots = 3, each = 200/3 ≈ 66.67
        float each = 200f / 3f;
        AssertApprox(each, root.Children[0].ComputedX - rootX, "Child0 X");
        AssertApprox(each + 50 + each, root.Children[1].ComputedX - rootX, "Child1 X");
    }

    // 33. AlignItems.FlexStart positions at start of cross axis
    [Fact]
    public void AlignItems_FlexStart()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 200,
            AlignItems = AlignItems.FlexStart
        };
        root.AddChild(new LayoutNode { Width = 100, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(0, root.Children[0].ComputedY - root.ComputedY, "Child Y");
    }

    // 34. AlignItems.FlexEnd positions at end of cross axis
    [Fact]
    public void AlignItems_FlexEnd()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 200,
            AlignItems = AlignItems.FlexEnd
        };
        root.AddChild(new LayoutNode { Width = 100, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(150, root.Children[0].ComputedY - root.ComputedY, "Child Y");
    }

    // 35. AlignSelf overrides parent AlignItems
    [Fact]
    public void AlignSelf_OverridesParent()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 200,
            AlignItems = AlignItems.FlexStart
        };
        root.AddChild(new LayoutNode { Width = 100, Height = 50, AlignSelf = AlignSelf.Center });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(75, root.Children[0].ComputedY - root.ComputedY, "Child Y");
    }

    // 36. AlignSelf.Stretch stretches child on cross axis
    [Fact]
    public void AlignSelf_Stretch()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 200,
            AlignItems = AlignItems.FlexStart
        };
        root.AddChild(new LayoutNode { Width = 100, AlignSelf = AlignSelf.Stretch });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.Children[0].ComputedHeight, "Child Height");
    }

    // 37. RowReverse reverses child order
    // CSS: .parent { display:flex; flex-direction:row-reverse; width:300px }
    //      .child { width:50px }
    // Browser: first child at right edge (X=250), last child at left (X=0)
    [Fact]
    public void RowReverse_ReversesOrder()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.RowReverse, Width = 300, Height = 100
        };
        root.AddChild(new LayoutNode { Width = 50 });
        root.AddChild(new LayoutNode { Width = 50 });
        root.AddChild(new LayoutNode { Width = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        // row-reverse: items pack from the right edge
        AssertApprox(250, root.Children[0].ComputedX - rootX, "Child0 X (rightmost)");
        AssertApprox(200, root.Children[1].ComputedX - rootX, "Child1 X (middle)");
        AssertApprox(150, root.Children[2].ComputedX - rootX, "Child2 X (leftmost)");
    }

    // 38. ColumnReverse reverses vertical order
    // CSS: .parent { display:flex; flex-direction:column-reverse; height:200px }
    //      .child { height:30px }
    // Browser: first child at bottom edge (Y=170), second child above (Y=140)
    [Fact]
    public void ColumnReverse_ReversesOrder()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.ColumnReverse, Width = 200, Height = 200
        };
        root.AddChild(new LayoutNode { Height = 30 });
        root.AddChild(new LayoutNode { Height = 30 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        // column-reverse: items pack from the bottom edge
        AssertApprox(170, root.Children[0].ComputedY - rootY, "Child0 Y (bottommost)");
        AssertApprox(140, root.Children[1].ComputedY - rootY, "Child1 Y (above)");
    }

    // 39. Aspect ratio: width set, height derived
    // CSS: .box { width:200px; aspect-ratio:2 } → height = 200/2 = 100px
    // Browser: aspect-ratio works on any element, in-flow or absolute
    [Fact]
    public void AspectRatio_HeightFromWidth()
    {
        var root = new LayoutNode { Width = 200, AspectRatio = 2f };
        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.ComputedWidth, "Width");
        AssertApprox(100, root.ComputedHeight, "Height (200/2)");
    }

    // 40. Aspect ratio: height set, width derived
    [Fact]
    public void AspectRatio_WidthFromHeight()
    {
        var root = new LayoutNode { Height = 100, AspectRatio = 2f };

        CalculateUndefined(root);

        AssertApprox(200, root.ComputedWidth, "Width (100*2)");
        AssertApprox(100, root.ComputedHeight, "Height");
    }

    // 41. Gap in row layout
    [Fact]
    public void Gap_SpacesChildrenInRow()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 400, Height = 100, Gap = 20
        };
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        AssertApprox(0, root.Children[0].ComputedX - rootX, "Child0 X");
        AssertApprox(70, root.Children[1].ComputedX - rootX, "Child1 X"); // 50 + 20
        AssertApprox(140, root.Children[2].ComputedX - rootX, "Child2 X"); // 120 + 20
    }

    // 42. Absolute position with bottom/right
    [Fact]
    public void PositionAbsolute_BottomRight()
    {
        var root = new LayoutNode { Width = 300, Height = 300 };
        root.AddChild(new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionBottom = 10, PositionRight = 20,
            Width = 50, Height = 50
        });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        float rootY = root.ComputedY;
        AssertApprox(rootX + 230, root.Children[0].ComputedX, "Child X"); // 300 - 20 - 50
        AssertApprox(rootY + 240, root.Children[0].ComputedY, "Child Y"); // 300 - 10 - 50
    }

    // 43. Absolute position with left+right stretches width
    [Fact]
    public void PositionAbsolute_LeftAndRight_StretchesWidth()
    {
        var root = new LayoutNode { Width = 300, Height = 300 };
        root.AddChild(new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionLeft = 20, PositionRight = 30,
            Height = 50
        });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(250, root.Children[0].ComputedWidth, "Child Width"); // 300 - 20 - 30
    }

    // 44. Absolute position with top+bottom stretches height
    [Fact]
    public void PositionAbsolute_TopAndBottom_StretchesHeight()
    {
        var root = new LayoutNode { Width = 300, Height = 300 };
        root.AddChild(new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 10, PositionBottom = 20,
            Width = 50
        });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(270, root.Children[0].ComputedHeight, "Child Height"); // 300 - 10 - 20
    }

    // 45. Absolute child does not affect sibling positions
    [Fact]
    public void AbsoluteChild_DoesNotAffectSiblings()
    {
        var root = new LayoutNode { Width = 200, Height = 300 };
        root.AddChild(new LayoutNode { Height = 30 });
        root.AddChild(new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 0, PositionLeft = 0,
            Width = 100, Height = 100
        });
        root.AddChild(new LayoutNode { Height = 30 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        // Third child should be at Y=30 (not 130), absolute child is out of flow
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Child0 Y");
        AssertApprox(30, root.Children[2].ComputedY - rootY, "Child2 Y");
    }

    // 46. Flex-grow with basis
    [Fact]
    public void FlexGrow_WithFlexBasis()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 100
        };
        root.AddChild(new LayoutNode { FlexBasis = 50, FlexGrow = 1 });
        root.AddChild(new LayoutNode { FlexBasis = 50, FlexGrow = 1 });

        YogaLayout.Calculate(root, 800, 600);

        // Remaining = 300 - 100 = 200, split evenly → each gets 100 extra
        AssertApprox(150, root.Children[0].ComputedWidth, "Child0 Width");
        AssertApprox(150, root.Children[1].ComputedWidth, "Child1 Width");
    }

    // 47. Flex-shrink with different ratios
    [Fact]
    public void FlexShrink_DifferentRatios()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 200, Height = 50
        };
        root.AddChild(new LayoutNode { Width = 100, FlexShrink = 1 });
        root.AddChild(new LayoutNode { Width = 200, FlexShrink = 2 });

        YogaLayout.Calculate(root, 800, 600);

        // Overflow = 100. Shrink scaled: 1*100=100, 2*200=400 → total=500
        // Child0: 100 - 100*(100/500) = 100 - 20 = 80
        // Child1: 200 - 100*(400/500) = 200 - 80 = 120
        AssertApprox(80, root.Children[0].ComputedWidth, "Child0 Width");
        AssertApprox(120, root.Children[1].ComputedWidth, "Child1 Width");
    }

    // 48. Max height constrains child
    [Fact]
    public void MaxHeight_Constrains()
    {
        var root = new LayoutNode { Height = 500, MaxHeight = 200 };
        YogaLayout.Calculate(root, 800, 600);
        AssertApprox(200, root.ComputedHeight, "Height");
    }

    // 49. Min height constrains child
    [Fact]
    public void MinHeight_Constrains()
    {
        var root = new LayoutNode { Height = 20, MinHeight = 50 };
        YogaLayout.Calculate(root, 800, 600);
        AssertApprox(50, root.ComputedHeight, "Height");
    }

    // 50. Padding on all sides reduces content area
    [Fact]
    public void Padding_AllSides_ReducesContentArea()
    {
        var root = new LayoutNode
        {
            Width = 200, Height = 200,
            PaddingTop = 10, PaddingRight = 20, PaddingBottom = 30, PaddingLeft = 40,
            AlignItems = AlignItems.Stretch
        };
        root.AddChild(new LayoutNode { Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        // Child should stretch to 200 - 40 - 20 = 140 wide
        AssertApprox(140, root.Children[0].ComputedWidth, "Child Width");
        // Child X offset by left padding
        AssertApprox(40, root.Children[0].ComputedX - root.ComputedX, "Child X");
        // Child Y offset by top padding
        AssertApprox(10, root.Children[0].ComputedY - root.ComputedY, "Child Y");
    }

    // 51. Margin between siblings
    [Fact]
    public void Margin_BetweenSiblings()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 400, Height = 100
        };
        root.AddChild(new LayoutNode { Width = 50, MarginRight = 20 });
        root.AddChild(new LayoutNode { Width = 50, MarginLeft = 10 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        AssertApprox(0, root.Children[0].ComputedX - rootX, "Child0 X");
        // Child1 at: 0 + 50 (width) + 20 (marginRight) + 10 (marginLeft) = 80
        AssertApprox(80, root.Children[1].ComputedX - rootX, "Child1 X");
    }

    // 52. FlexWrap with gap between wrap lines
    [Fact]
    public void FlexWrap_WithGap_SpacesLines()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200, Height = 300,
            Gap = 10
        };
        for (int i = 0; i < 4; i++)
            root.AddChild(new LayoutNode { Width = 90, Height = 40 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        // Line 1: children 0, 1 (90 + 10 + 90 = 190 < 200)
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Child0 Y");
        AssertApprox(0, root.Children[1].ComputedY - rootY, "Child1 Y");
        // Line 2: children 2, 3 (Y = 40 + 10 gap = 50)
        AssertApprox(50, root.Children[2].ComputedY - rootY, "Child2 Y");
        AssertApprox(50, root.Children[3].ComputedY - rootY, "Child3 Y");
    }

    // 53. Deeply nested layout computes correctly
    [Fact]
    public void DeeplyNested_ThreeLevels()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 400, Height = 400,
            PaddingTop = 10, PaddingLeft = 10
        };
        var level1 = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Height = 100,
            PaddingLeft = 5
        };
        var level2 = new LayoutNode { Width = 50, Height = 50 };
        level1.AddChild(level2);
        root.AddChild(level1);

        YogaLayout.Calculate(root, 800, 600);

        // level2 X = root.X + 10 (root pad) + 5 (level1 pad)
        float expectedX = root.ComputedX + 10 + 5;
        float expectedY = root.ComputedY + 10;
        AssertApprox(expectedX, level2.ComputedX, "Level2 X");
        AssertApprox(expectedY, level2.ComputedY, "Level2 Y");
    }

    // 54. Flex-grow with three children, unequal ratios
    [Fact]
    public void FlexGrow_ThreeChildren_UnequalRatios()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 600, Height = 100
        };
        root.AddChild(new LayoutNode { FlexGrow = 1 });
        root.AddChild(new LayoutNode { FlexGrow = 2 });
        root.AddChild(new LayoutNode { FlexGrow = 3 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(100, root.Children[0].ComputedWidth, "Child0 Width"); // 600/6
        AssertApprox(200, root.Children[1].ComputedWidth, "Child1 Width"); // 1200/6
        AssertApprox(300, root.Children[2].ComputedWidth, "Child2 Width"); // 1800/6
    }

    // 55. No children - node with just padding has correct size
    [Fact]
    public void NoChildren_PaddingOnly_CorrectSize()
    {
        var root = new LayoutNode
        {
            PaddingTop = 10, PaddingRight = 20, PaddingBottom = 30, PaddingLeft = 40
        };

        CalculateUndefined(root);

        AssertApprox(60, root.ComputedWidth, "Width (padL + padR)");
        AssertApprox(40, root.ComputedHeight, "Height (padT + padB)");
    }

    // 56. Percent width on in-flow child
    // CSS: .parent { width:400px } .child { width:50%; height:50px }
    // Browser: child width = 200px (50% of parent)
    [Fact]
    public void PercentWidth_ResolvesAgainstParent()
    {
        var root = new LayoutNode { Width = 400, Height = 200 };
        root.AddChild(new LayoutNode { WidthPercent = 50, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.Children[0].ComputedWidth, "Child Width (50%)");
    }

    // 57. Percent height on in-flow child
    // CSS: .parent { height:200px } .child { width:50px; height:50% }
    // Browser: child height = 100px (50% of parent)
    [Fact]
    public void PercentHeight_ResolvesAgainstParent()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 400, Height = 200
        };
        root.AddChild(new LayoutNode { Width = 50, HeightPercent = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(100, root.Children[0].ComputedHeight, "Child Height (50%)");
    }

    // 58. MarkDirty propagates to parent
    [Fact]
    public void MarkDirty_PropagatesToParent()
    {
        var root = new LayoutNode();
        var child = new LayoutNode();
        root.AddChild(child);

        root.IsDirty = false;
        child.IsDirty = false;

        child.MarkDirty();

        Assert.True(child.IsDirty);
        Assert.True(root.IsDirty);
    }

    // 59. RemoveChild removes from tree
    [Fact]
    public void RemoveChild_RemovesFromTree()
    {
        var root = new LayoutNode();
        var child = new LayoutNode();
        root.AddChild(child);

        Assert.Single(root.Children);
        Assert.Same(root, child.Parent);

        root.RemoveChild(child);

        Assert.Empty(root.Children);
        Assert.Null(child.Parent);
    }

    // 60. Column with JustifyContent.Center
    [Fact]
    public void Column_JustifyContent_Center()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 200, Height = 300,
            JustifyContent = JustifyContent.Center
        };
        root.AddChild(new LayoutNode { Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        // Centered: (300 - 50) / 2 = 125
        AssertApprox(125, root.Children[0].ComputedY - root.ComputedY, "Child Y");
    }

    // 61. Flex grow with gap
    [Fact]
    public void FlexGrow_WithGap_DistributesCorrectly()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 320, Height = 100, Gap = 20
        };
        root.AddChild(new LayoutNode { FlexGrow = 1 });
        root.AddChild(new LayoutNode { FlexGrow = 1 });

        YogaLayout.Calculate(root, 800, 600);

        // Available = 320 - 20 (gap) = 300, split evenly
        AssertApprox(150, root.Children[0].ComputedWidth, "Child0 Width");
        AssertApprox(150, root.Children[1].ComputedWidth, "Child1 Width");
    }
}
