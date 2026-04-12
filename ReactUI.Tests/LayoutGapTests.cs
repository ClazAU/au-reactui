using System.Reflection;
using ReactUI.Layout;
using Xunit;

namespace ReactUI.Tests;

/// <summary>
/// Tests for flex wrap auto-height, overflow scroll, and other layout
/// edge cases not covered by existing LayoutTests/BrowserLayoutTests.
/// </summary>
public class LayoutGapTests
{
    private const float Tolerance = 0.5f;

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
        method!.Invoke(null, new object[]
        {
            node, float.NaN, float.NaN,
            MeasureMode.Undefined, MeasureMode.Undefined
        });
    }

    // ══════════════════════════════════════════════
    //  Flex wrap with auto height
    // ══════════════════════════════════════════════

    /// <summary>
    /// Wrapping container with no explicit height should grow to fit all rows.
    /// CSS: .grid { display:flex; flex-direction:row; flex-wrap:wrap; width:200px }
    ///      .cell { width:80px; height:40px }
    /// 3 cells: 2 per row (80+80=160 < 200), 3rd wraps.
    /// Expected height: 40 + 40 = 80
    /// </summary>
    [Fact]
    public void FlexWrap_AutoHeight_GrowsToFitAllRows()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200
        };
        for (int i = 0; i < 3; i++)
            root.AddChild(new LayoutNode { Width = 80, Height = 40 });

        YogaLayout.Calculate(root, 800, 600);

        // 2 rows: row1 (child0, child1), row2 (child2)
        AssertApprox(80, root.ComputedHeight, "Container auto-height = 2 rows * 40");
    }

    /// <summary>
    /// The exact repro from the bug report: 15 children, 64px each, in 392px container.
    /// CSS: .outer { width:392px }
    ///      .grid { flex-direction:row; flex-wrap:wrap; gap:10px }
    ///      .cell { width:64px; height:64px; flex-shrink:0 }
    /// Row capacity: floor((392 + 10) / (64 + 10)) = 5 per row (5*64 + 4*10 = 360 < 392)
    /// 15 cells → 3 rows. Height = 3*64 + 2*10 = 212
    /// </summary>
    [Fact]
    public void FlexWrap_AutoHeight_BugRepro_15Cells()
    {
        var outer = new LayoutNode { Width = 392 };
        var grid = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Gap = 10
        };
        for (int i = 0; i < 15; i++)
            grid.AddChild(new LayoutNode { Width = 64, Height = 64, FlexShrink = 0 });

        outer.AddChild(grid);
        YogaLayout.Calculate(outer, 800, 600);

        // 5 per row, 3 rows: 3*64 + 2*10 = 212
        AssertApprox(212, grid.ComputedHeight, "Grid height = 3 rows of 64 + 2 gaps of 10");
    }

    /// <summary>
    /// Wrap with auto height and gap between rows.
    /// </summary>
    [Fact]
    public void FlexWrap_AutoHeight_WithGap()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200,
            Gap = 10
        };
        // 3 children, 90px each + 10 gap: 90+10+90 = 190 fits, 3rd wraps
        for (int i = 0; i < 3; i++)
            root.AddChild(new LayoutNode { Width = 90, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        // 2 rows of 50px + 1 gap of 10 = 110
        AssertApprox(110, root.ComputedHeight, "Auto-height = 2*50 + 10 gap");
    }

    /// <summary>
    /// Nested: a column parent contains a wrapping row child with auto height.
    /// The parent should also grow to accommodate the wrapped content.
    /// </summary>
    [Fact]
    public void FlexWrap_AutoHeight_NestedInColumn()
    {
        var column = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            Width = 300,
            PaddingTop = 10, PaddingBottom = 10
        };
        var header = new LayoutNode { Height = 30 };
        var grid = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Gap = 5
        };
        // 6 children, 90px each in 300px: 3 per row (90+5+90+5+90 = 280 < 300)
        for (int i = 0; i < 6; i++)
            grid.AddChild(new LayoutNode { Width = 90, Height = 40 });

        column.AddChild(header);
        column.AddChild(grid);

        CalculateUndefined(column);

        // Grid: 2 rows of 40 + 1 gap of 5 = 85
        AssertApprox(85, grid.ComputedHeight, "Grid auto-height");
        // Column: pad(10) + header(30) + grid(85) + pad(10) = 135
        AssertApprox(135, column.ComputedHeight, "Column grows to fit wrapped grid");
    }

    // ══════════════════════════════════════════════
    //  Wrap with mixed-height children
    // ══════════════════════════════════════════════

    /// <summary>
    /// Each wrapped line's cross size should be the max child height in that line.
    /// </summary>
    [Fact]
    public void FlexWrap_MixedHeights_LineUsesMaxHeight()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200, Height = 300
        };
        // Line 1: 80px + 80px = 160 fits. Heights 30 and 60 → line height = 60
        root.AddChild(new LayoutNode { Width = 80, Height = 30 });
        root.AddChild(new LayoutNode { Width = 80, Height = 60 });
        // Line 2: 80px fits. Height 40 → line height = 40
        root.AddChild(new LayoutNode { Width = 150, Height = 40 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        // Line 1 starts at Y=0, line 2 at Y=60
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Child0 Y");
        AssertApprox(0, root.Children[1].ComputedY - rootY, "Child1 Y");
        AssertApprox(60, root.Children[2].ComputedY - rootY, "Child2 Y (line 2 after 60px line)");
    }

    // ══════════════════════════════════════════════
    //  Wrap with padding on container
    // ══════════════════════════════════════════════

    /// <summary>
    /// Wrapping container with padding should use inner width for wrap decisions.
    /// </summary>
    [Fact]
    public void FlexWrap_WithPadding_WrapsBasedOnInnerWidth()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200, Height = 200,
            PaddingLeft = 30, PaddingRight = 30
        };
        // Inner width = 200 - 60 = 140
        // Each child 80px → only 1 fits per row (80 < 140, 80+80=160 > 140)
        root.AddChild(new LayoutNode { Width = 80, Height = 30 });
        root.AddChild(new LayoutNode { Width = 80, Height = 30 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        float rootX = root.ComputedX;
        // Child0 on line 1, child1 on line 2
        AssertApprox(30, root.Children[0].ComputedX - rootX, "Child0 X (after left padding)");
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Child0 Y");
        AssertApprox(30, root.Children[1].ComputedY - rootY, "Child1 Y (wrapped to line 2)");
    }

    // ══════════════════════════════════════════════
    //  Overflow scroll containers
    // ══════════════════════════════════════════════

    /// <summary>
    /// Column scroll: children are not constrained on height (main axis).
    /// They should size to their own content/explicit height.
    /// </summary>
    [Fact]
    public void OverflowScroll_Column_ChildrenSizeToContent()
    {
        var scroll = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            Width = 300, Height = 200,
            Overflow = Overflow.Scroll
        };
        // Children totaling more than container height
        for (int i = 0; i < 10; i++)
            scroll.AddChild(new LayoutNode { Height = 50 });

        YogaLayout.Calculate(scroll, 800, 600);

        // Container stays at 200
        AssertApprox(200, scroll.ComputedHeight, "Scroll container height stays 200");
        // Children positioned sequentially beyond container
        float scrollY = scroll.ComputedY;
        for (int i = 0; i < 10; i++)
        {
            AssertApprox(i * 50, scroll.Children[i].ComputedY - scrollY, $"Child{i} Y");
            AssertApprox(50, scroll.Children[i].ComputedHeight, $"Child{i} Height");
        }
    }

    /// <summary>
    /// Row scroll: children are not constrained on width (main axis).
    /// </summary>
    [Fact]
    public void OverflowScroll_Row_ChildrenSizeToContent()
    {
        var scroll = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            Width = 200, Height = 100,
            Overflow = Overflow.Scroll
        };
        for (int i = 0; i < 5; i++)
            scroll.AddChild(new LayoutNode { Width = 100, Height = 50, FlexShrink = 0 });

        YogaLayout.Calculate(scroll, 800, 600);

        // Container stays at 200
        AssertApprox(200, scroll.ComputedWidth, "Scroll container width");
        // Children not shrunk: each stays 100px
        float scrollX = scroll.ComputedX;
        for (int i = 0; i < 5; i++)
        {
            AssertApprox(100, scroll.Children[i].ComputedWidth, $"Child{i} Width (not shrunk)");
            AssertApprox(i * 100, scroll.Children[i].ComputedX - scrollX, $"Child{i} X");
        }
    }

    // ══════════════════════════════════════════════
    //  Stretch does NOT apply with explicit cross size
    // ══════════════════════════════════════════════

    /// <summary>
    /// A child with an explicit height in a row container should NOT be stretched,
    /// even when AlignItems is Stretch.
    /// </summary>
    [Fact]
    public void Stretch_DoesNotOverrideExplicitCrossSize()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 200,
            AlignItems = AlignItems.Stretch
        };
        root.AddChild(new LayoutNode { Width = 100, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        // Child has explicit height=50, should NOT stretch to 200
        AssertApprox(50, root.Children[0].ComputedHeight, "Explicit height preserved");
    }

    /// <summary>
    /// A child with percent height should NOT be stretched either.
    /// </summary>
    [Fact]
    public void Stretch_DoesNotOverridePercentCrossSize()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 200,
            AlignItems = AlignItems.Stretch
        };
        root.AddChild(new LayoutNode { Width = 100, HeightPercent = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(100, root.Children[0].ComputedHeight, "Percent height = 50% of 200 = 100");
    }

    // ══════════════════════════════════════════════
    //  Absolute children with percent dimensions
    // ══════════════════════════════════════════════

    /// <summary>
    /// Absolute child with percent width/height resolves against parent content area.
    /// </summary>
    [Fact]
    public void Absolute_PercentDimensions_ResolveAgainstParent()
    {
        var root = new LayoutNode
        {
            Width = 400, Height = 300,
            PaddingTop = 10, PaddingRight = 10, PaddingBottom = 10, PaddingLeft = 10
        };
        root.AddChild(new LayoutNode
        {
            Position = PositionType.Absolute,
            WidthPercent = 50,
            HeightPercent = 50,
            PositionTop = 0, PositionLeft = 0
        });

        YogaLayout.Calculate(root, 800, 600);

        // Content area: 380 x 280. 50% = 190 x 140
        AssertApprox(190, root.Children[0].ComputedWidth, "50% of content width");
        AssertApprox(140, root.Children[0].ComputedHeight, "50% of content height");
    }

    // ══════════════════════════════════════════════
    //  Flex basis from MeasureFunc (intrinsic sizing)
    // ══════════════════════════════════════════════

    /// <summary>
    /// In a row, a child with no explicit width but a MeasureFunc should use
    /// intrinsic width as its flex basis.
    /// </summary>
    [Fact]
    public void FlexBasis_Intrinsic_FromMeasureFunc()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            Width = 500, Height = 100,
            AlignItems = AlignItems.FlexStart
        };
        var measured = new LayoutNode
        {
            MeasureFunc = (w, wm, h, hm) => (120, 30)
        };
        var filler = new LayoutNode { FlexGrow = 1 };

        root.AddChild(measured);
        root.AddChild(filler);

        YogaLayout.Calculate(root, 800, 600);

        // Measured child takes its intrinsic width, filler takes the rest
        AssertApprox(120, measured.ComputedWidth, "Measured width from intrinsic");
        AssertApprox(380, filler.ComputedWidth, "Filler = 500 - 120");
    }

    // ══════════════════════════════════════════════
    //  Aspect ratio edge cases
    // ══════════════════════════════════════════════

    /// <summary>
    /// Aspect ratio in an auto-width context: child in a column with no explicit width
    /// but with explicit height and aspect ratio.
    /// </summary>
    [Fact]
    public void AspectRatio_AutoWidthInColumn()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 400, Height = 300,
            AlignItems = AlignItems.FlexStart
        };
        root.AddChild(new LayoutNode { Height = 100, AspectRatio = 2f });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.Children[0].ComputedWidth, "Width = 100 * 2");
        AssertApprox(100, root.Children[0].ComputedHeight, "Height stays 100");
    }

    // ══════════════════════════════════════════════
    //  Min/Max with flex grow/shrink edge cases
    // ══════════════════════════════════════════════

    /// <summary>
    /// Two flex-grow children where one is capped by max-width.
    /// Standard flex: grow distributes proportionally, then clamps. No redistribution.
    /// </summary>
    [Fact]
    public void FlexGrow_MaxWidth_CapsWithoutRedistribution()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 600, Height = 100
        };
        root.AddChild(new LayoutNode { FlexGrow = 1, MaxWidth = 100 });
        root.AddChild(new LayoutNode { FlexGrow = 1 });

        YogaLayout.Calculate(root, 800, 600);

        // Child0 capped at max-width
        Assert.True(root.Children[0].ComputedWidth <= 100 + Tolerance,
            $"Child0 capped: {root.Children[0].ComputedWidth}");
        // Each gets 300 from grow, child0 clamped to 100
        AssertApprox(300, root.Children[1].ComputedWidth, "Child1 gets its share");
    }

    /// <summary>
    /// Min height prevents a column child from collapsing.
    /// </summary>
    [Fact]
    public void MinHeight_PreventsCollapse()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 200
        };
        root.AddChild(new LayoutNode { MinHeight = 50 });

        CalculateUndefined(root);

        Assert.True(root.Children[0].ComputedHeight >= 50 - Tolerance,
            $"Min height enforced: {root.Children[0].ComputedHeight}");
    }

    // ══════════════════════════════════════════════
    //  Gap with reverse directions
    // ══════════════════════════════════════════════

    /// <summary>
    /// Gap should still space items correctly in row-reverse.
    /// </summary>
    [Fact]
    public void Gap_RowReverse_SpacesCorrectly()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.RowReverse,
            Width = 300, Height = 100, Gap = 20
        };
        root.AddChild(new LayoutNode { Width = 50 });
        root.AddChild(new LayoutNode { Width = 50 });
        root.AddChild(new LayoutNode { Width = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        // row-reverse with gap: items pack from right with gap between
        AssertApprox(250, root.Children[0].ComputedX - rootX, "Child0 (rightmost)");
        AssertApprox(180, root.Children[1].ComputedX - rootX, "Child1 (250 - 50 - 20)");
        AssertApprox(110, root.Children[2].ComputedX - rootX, "Child2 (180 - 50 - 20)");
    }

    /// <summary>
    /// Gap should still space items correctly in column-reverse.
    /// </summary>
    [Fact]
    public void Gap_ColumnReverse_SpacesCorrectly()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.ColumnReverse,
            Width = 100, Height = 300, Gap = 15
        };
        root.AddChild(new LayoutNode { Height = 40 });
        root.AddChild(new LayoutNode { Height = 40 });
        root.AddChild(new LayoutNode { Height = 40 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        // column-reverse: items pack from bottom with gap
        AssertApprox(260, root.Children[0].ComputedY - rootY, "Child0 (bottommost)");
        AssertApprox(205, root.Children[1].ComputedY - rootY, "Child1 (260 - 40 - 15)");
        AssertApprox(150, root.Children[2].ComputedY - rootY, "Child2 (205 - 40 - 15)");
    }

    // ══════════════════════════════════════════════
    //  Wrap with single child per line
    // ══════════════════════════════════════════════

    /// <summary>
    /// When each child is wider than the container, each gets its own line.
    /// </summary>
    [Fact]
    public void FlexWrap_EachChildGetsOwnLine()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 100, Height = 300
        };
        // Children wider than container - first always placed, then wrap before next
        root.AddChild(new LayoutNode { Width = 100, Height = 30 });
        root.AddChild(new LayoutNode { Width = 100, Height = 40 });
        root.AddChild(new LayoutNode { Width = 100, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Child0 Y");
        AssertApprox(30, root.Children[1].ComputedY - rootY, "Child1 Y (after 30)");
        AssertApprox(70, root.Children[2].ComputedY - rootY, "Child2 Y (after 30+40)");
    }

    // ══════════════════════════════════════════════
    //  FlexShrink = 0 prevents shrinking in wrap context
    // ══════════════════════════════════════════════

    /// <summary>
    /// Children with flex-shrink:0 in a wrapping row keep their full width.
    /// </summary>
    [Fact]
    public void FlexWrap_FlexShrinkZero_ChildrenKeepSize()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200, Height = 200
        };
        for (int i = 0; i < 4; i++)
            root.AddChild(new LayoutNode { Width = 80, Height = 30, FlexShrink = 0 });

        YogaLayout.Calculate(root, 800, 600);

        // Each child stays 80px
        for (int i = 0; i < 4; i++)
            AssertApprox(80, root.Children[i].ComputedWidth, $"Child{i} Width");
    }

    // ══════════════════════════════════════════════
    //  Complex real-world: settings grid with wrap
    // ══════════════════════════════════════════════

    /// <summary>
    /// A responsive settings grid: mod toggles wrap into rows.
    /// Outer panel (fixed width, auto height) → wrapping grid → cells with content.
    /// </summary>
    [Fact]
    public void FlexWrap_AutoHeight_SettingsGrid()
    {
        var panel = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            Width = 420,
            PaddingTop = 16, PaddingBottom = 16, PaddingLeft = 16, PaddingRight = 16,
            Gap = 12
        };

        // Title
        var title = new LayoutNode { Height = 24 };
        panel.AddChild(title);

        // Grid of toggle cards
        var grid = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Gap = 8
        };
        // Inner width = 420-32 = 388. Cards: 120px each.
        // Per row: 120+8+120+8+120 = 376 < 388, so 3 per row.
        for (int i = 0; i < 9; i++)
            grid.AddChild(new LayoutNode { Width = 120, Height = 80, FlexShrink = 0 });

        panel.AddChild(grid);

        CalculateUndefined(panel);

        // Grid: 3 rows of 80 + 2 gaps of 8 = 256
        AssertApprox(256, grid.ComputedHeight, "Grid auto-height");
        // Panel: pad(16) + title(24) + gap(12) + grid(256) + pad(16) = 324
        AssertApprox(324, panel.ComputedHeight, "Panel auto-height");
    }

    // ══════════════════════════════════════════════
    //  Wrap auto-height with Exactly mode parent
    // ══════════════════════════════════════════════

    /// <summary>
    /// When a wrapping container is inside a parent that provides Exactly mode
    /// (like the root), the container should still expand its height to fit
    /// all wrapped rows, not be constrained to the parent's available height.
    /// This is the core scenario the flex-wrap auto-height bug affected.
    /// </summary>
    [Fact]
    public void FlexWrap_AutoHeight_InsideExactlyModeParent()
    {
        // Root provides Exactly mode at 800x600
        var root = new LayoutNode { Width = 800, Height = 600 };
        var grid = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200
            // No explicit height → should auto-size
        };
        for (int i = 0; i < 6; i++)
            grid.AddChild(new LayoutNode { Width = 90, Height = 40 });

        root.AddChild(grid);

        YogaLayout.Calculate(root, 800, 600);

        // 2 per row (90+90=180 < 200), 3 rows of 40 = 120
        AssertApprox(120, grid.ComputedHeight, "Grid auto-height despite Exactly parent");
    }

    // ══════════════════════════════════════════════
    //  Multiple margins in flex
    // ══════════════════════════════════════════════

    /// <summary>
    /// Margins on all sides of a flex child affect both positioning and flex basis.
    /// </summary>
    [Fact]
    public void Margin_AllSides_AffectsLayout()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 400, Height = 200
        };
        root.AddChild(new LayoutNode
        {
            Width = 100, Height = 50,
            MarginTop = 10, MarginRight = 20, MarginBottom = 30, MarginLeft = 40
        });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        float rootY = root.ComputedY;
        AssertApprox(40, root.Children[0].ComputedX - rootX, "Child X offset by marginLeft");
        AssertApprox(10, root.Children[0].ComputedY - rootY, "Child Y offset by marginTop");
    }

    // ══════════════════════════════════════════════
    //  Padding + Margin combined
    // ══════════════════════════════════════════════

    /// <summary>
    /// Both parent padding and child margin should stack.
    /// </summary>
    [Fact]
    public void PaddingAndMargin_Stack()
    {
        var root = new LayoutNode
        {
            Width = 400, Height = 300,
            PaddingTop = 20, PaddingLeft = 30
        };
        root.AddChild(new LayoutNode
        {
            Width = 100, Height = 50,
            MarginTop = 15, MarginLeft = 25
        });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        float rootY = root.ComputedY;
        // X = padding(30) + margin(25) = 55
        AssertApprox(55, root.Children[0].ComputedX - rootX, "Child X = pad + margin");
        // Y = padding(20) + margin(15) = 35
        AssertApprox(35, root.Children[0].ComputedY - rootY, "Child Y = pad + margin");
    }

    // ══════════════════════════════════════════════
    //  Zero-size edge cases
    // ══════════════════════════════════════════════

    /// <summary>
    /// A container with no children and no padding has zero content size.
    /// </summary>
    [Fact]
    public void EmptyContainer_ZeroContentSize()
    {
        var root = new LayoutNode();
        CalculateUndefined(root);

        AssertApprox(0, root.ComputedWidth, "Empty width");
        AssertApprox(0, root.ComputedHeight, "Empty height");
    }

    /// <summary>
    /// A container with display:none children should collapse.
    /// </summary>
    [Fact]
    public void AllChildrenHidden_ContainerCollapses()
    {
        var root = new LayoutNode { Width = 200 };
        root.AddChild(new LayoutNode { Height = 50, Display = false });
        root.AddChild(new LayoutNode { Height = 50, Display = false });

        CalculateUndefined(root);

        AssertApprox(0, root.ComputedHeight, "Height collapses when all children hidden");
    }

    // ══════════════════════════════════════════════
    //  Deeply nested flex with auto sizes
    // ══════════════════════════════════════════════

    /// <summary>
    /// Four levels deep: auto-sizing propagates up correctly.
    /// </summary>
    [Fact]
    public void DeepNesting_AutoSizePropagatesUp()
    {
        var root = new LayoutNode { Width = 500 };
        var level1 = new LayoutNode { PaddingTop = 5, PaddingBottom = 5 };
        var level2 = new LayoutNode { PaddingTop = 5, PaddingBottom = 5 };
        var leaf = new LayoutNode { Height = 30 };

        level2.AddChild(leaf);
        level1.AddChild(level2);
        root.AddChild(level1);

        CalculateUndefined(root);

        // leaf: 30, level2: 30+10=40, level1: 40+10=50, root: 50
        AssertApprox(40, level2.ComputedHeight, "Level2 height");
        AssertApprox(50, level1.ComputedHeight, "Level1 height");
        AssertApprox(50, root.ComputedHeight, "Root height propagated up");
    }

    // ══════════════════════════════════════════════
    //  Flex basis zero with flex-grow (equal distribution)
    // ══════════════════════════════════════════════

    /// <summary>
    /// flex-basis:0 with flex-grow distributes ALL space proportionally,
    /// ignoring child content size.
    /// CSS: .child { flex: 1 1 0 }
    /// </summary>
    [Fact]
    public void FlexBasisZero_WithGrow_DistributesAllSpace()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 100
        };
        root.AddChild(new LayoutNode { FlexBasis = 0, FlexGrow = 1 });
        root.AddChild(new LayoutNode { FlexBasis = 0, FlexGrow = 1 });
        root.AddChild(new LayoutNode { FlexBasis = 0, FlexGrow = 1 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(100, root.Children[0].ComputedWidth, "Each gets 1/3");
        AssertApprox(100, root.Children[1].ComputedWidth, "Each gets 1/3");
        AssertApprox(100, root.Children[2].ComputedWidth, "Each gets 1/3");
    }

    // ══════════════════════════════════════════════
    //  Overflow.Hidden with absolute child
    // ══════════════════════════════════════════════

    /// <summary>
    /// Layout should still compute positions for absolute children
    /// inside an overflow:hidden container (rendering clips, not layout).
    /// </summary>
    [Fact]
    public void OverflowHidden_AbsoluteChild_StillPositioned()
    {
        var root = new LayoutNode
        {
            Width = 200, Height = 200,
            Overflow = Overflow.Hidden
        };
        root.AddChild(new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 10, PositionLeft = 10,
            Width = 300, Height = 300  // larger than parent
        });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        float rootY = root.ComputedY;
        AssertApprox(rootX + 10, root.Children[0].ComputedX, "Abs child X");
        AssertApprox(rootY + 10, root.Children[0].ComputedY, "Abs child Y");
        AssertApprox(300, root.Children[0].ComputedWidth, "Abs child full width");
        AssertApprox(300, root.Children[0].ComputedHeight, "Abs child full height");
    }

    // ══════════════════════════════════════════════
    //  Wrap with auto height AND explicit width on container
    // ══════════════════════════════════════════════

    /// <summary>
    /// Container width is percent-based, auto height, wrapping.
    /// </summary>
    [Fact]
    public void FlexWrap_AutoHeight_PercentWidth()
    {
        var parent = new LayoutNode { Width = 400, Height = 600 };
        var grid = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            WidthPercent = 50  // 200px
        };
        for (int i = 0; i < 4; i++)
            grid.AddChild(new LayoutNode { Width = 90, Height = 30 });

        parent.AddChild(grid);
        YogaLayout.Calculate(parent, 800, 600);

        // 200px width: 2 per row (90+90=180 < 200), 2 rows of 30 = 60
        AssertApprox(200, grid.ComputedWidth, "Grid 50% width");
        AssertApprox(60, grid.ComputedHeight, "Grid auto-height = 2 rows");
    }
}
