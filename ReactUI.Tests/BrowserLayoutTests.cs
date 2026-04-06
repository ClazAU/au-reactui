using System.Reflection;
using ReactUI.Layout;
using Xunit;

namespace ReactUI.Tests;

/// <summary>
/// Tests that verify our flexbox implementation matches browser behavior.
/// Each test documents the equivalent CSS and expected browser result.
/// </summary>
public class BrowserLayoutTests
{
    private const float Tolerance = 0.5f; // slightly relaxed for rounding

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
    //  CSS Box Model
    // ══════════════════════════════════════════════

    /// <summary>
    /// Browser: div { width: 200px; padding: 20px; } → offsetWidth = 200px (content-box, but
    /// our layout uses border-box-like behavior where width includes padding).
    /// Our engine: width=200 with padding=20 → content area = 160, total = 200
    /// </summary>
    [Fact]
    public void BoxModel_WidthIncludesPadding()
    {
        var root = new LayoutNode
        {
            Width = 200, Height = 100,
            PaddingLeft = 20, PaddingRight = 20,
            AlignItems = AlignItems.Stretch
        };
        root.AddChild(new LayoutNode { Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.ComputedWidth, "Root Width");
        AssertApprox(160, root.Children[0].ComputedWidth, "Child stretches to content area");
    }

    // ══════════════════════════════════════════════
    //  Flex defaults
    // ══════════════════════════════════════════════

    /// <summary>
    /// Browser default: flex items have flex-shrink: 1, flex-grow: 0, flex-basis: auto
    /// Items should shrink to fit but not grow.
    /// </summary>
    [Fact]
    public void FlexDefaults_ItemsShrinkButDontGrow()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 200, Height = 50
        };
        // Two items each wanting 150px in a 200px container
        root.AddChild(new LayoutNode { Width = 150 });
        root.AddChild(new LayoutNode { Width = 150 });

        YogaLayout.Calculate(root, 800, 600);

        // Both shrink equally (flex-shrink: 1 default)
        AssertApprox(100, root.Children[0].ComputedWidth, "Child0 shrinks");
        AssertApprox(100, root.Children[1].ComputedWidth, "Child1 shrinks");
    }

    /// <summary>
    /// Browser: flex container with no explicit items → container collapses to padding/border only.
    /// </summary>
    [Fact]
    public void EmptyFlexContainer_CollapsesToPadding()
    {
        var root = new LayoutNode
        {
            PaddingTop = 10, PaddingBottom = 10,
            PaddingLeft = 20, PaddingRight = 20
        };

        CalculateUndefined(root);

        AssertApprox(40, root.ComputedWidth, "Width = padL + padR");
        AssertApprox(20, root.ComputedHeight, "Height = padT + padB");
    }

    // ══════════════════════════════════════════════
    //  Holy grail layout (header, footer, 3-column body)
    // ══════════════════════════════════════════════

    /// <summary>
    /// Classic "holy grail" layout:
    /// .container { display:flex; flex-direction:column; height:600px }
    /// .header { height: 60px }
    /// .body { flex:1; display:flex; flex-direction:row }
    /// .sidebar-left { width: 200px }
    /// .main { flex:1 }
    /// .sidebar-right { width: 150px }
    /// .footer { height: 40px }
    /// </summary>
    [Fact]
    public void HolyGrailLayout()
    {
        var container = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 1024, Height = 600
        };

        var header = new LayoutNode { Height = 60 };
        var body = new LayoutNode { FlexDirection = FlexDirection.Row, FlexGrow = 1 };
        var footer = new LayoutNode { Height = 40 };

        var sidebarLeft = new LayoutNode { Width = 200 };
        var main = new LayoutNode { FlexGrow = 1 };
        var sidebarRight = new LayoutNode { Width = 150 };

        body.AddChild(sidebarLeft);
        body.AddChild(main);
        body.AddChild(sidebarRight);

        container.AddChild(header);
        container.AddChild(body);
        container.AddChild(footer);

        YogaLayout.Calculate(container, 1024, 600);

        // Header: 60px tall, full width
        AssertApprox(60, header.ComputedHeight, "Header height");
        AssertApprox(1024, header.ComputedWidth, "Header width");

        // Body: fills remaining 600 - 60 - 40 = 500px
        AssertApprox(500, body.ComputedHeight, "Body height");

        // Sidebars fixed, main fills remaining
        AssertApprox(200, sidebarLeft.ComputedWidth, "Left sidebar");
        AssertApprox(150, sidebarRight.ComputedWidth, "Right sidebar");
        AssertApprox(674, main.ComputedWidth, "Main area (1024-200-150)");

        // Footer
        AssertApprox(40, footer.ComputedHeight, "Footer height");
    }

    // ══════════════════════════════════════════════
    //  Navbar layout
    // ══════════════════════════════════════════════

    /// <summary>
    /// .navbar { display:flex; flex-direction:row; height:48px; padding:0 16px; gap:8px; align-items:center }
    /// .logo { width:32px; height:32px }
    /// .spacer { flex:1 }
    /// .btn { width:80px; height:32px }
    /// </summary>
    [Fact]
    public void NavbarLayout_LogoLeftButtonsRight()
    {
        var navbar = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            Width = 800, Height = 48,
            PaddingLeft = 16, PaddingRight = 16,
            Gap = 8,
            AlignItems = AlignItems.Center
        };

        var logo = new LayoutNode { Width = 32, Height = 32 };
        var spacer = new LayoutNode { FlexGrow = 1 };
        var btn1 = new LayoutNode { Width = 80, Height = 32 };
        var btn2 = new LayoutNode { Width = 80, Height = 32 };

        navbar.AddChild(logo);
        navbar.AddChild(spacer);
        navbar.AddChild(btn1);
        navbar.AddChild(btn2);

        YogaLayout.Calculate(navbar, 800, 600);

        // Logo at left padding offset
        float navX = navbar.ComputedX;
        AssertApprox(navX + 16, logo.ComputedX, "Logo X");

        // Logo vertically centered: (48-32)/2 = 8
        AssertApprox(navbar.ComputedY + 8, logo.ComputedY, "Logo Y centered");

        // Buttons at right side
        // Inner = 800 - 32 = 768, items = 32+spacer+80+80 with 3 gaps of 8 = 24
        // Spacer = 768 - 32 - 80 - 80 - 24 = 552
        AssertApprox(552, spacer.ComputedWidth, "Spacer fills remaining");

        // Buttons vertically centered
        AssertApprox(navbar.ComputedY + 8, btn1.ComputedY, "Btn1 Y centered");
        AssertApprox(navbar.ComputedY + 8, btn2.ComputedY, "Btn2 Y centered");
    }

    // ══════════════════════════════════════════════
    //  Card grid with wrap
    // ══════════════════════════════════════════════

    /// <summary>
    /// .grid { display:flex; flex-wrap:wrap; width:600px; gap:20px }
    /// .card { width:170px; height:200px }
    /// 3 cards per row: 170*3 + 20*2 = 550 < 600. 4th card wraps.
    /// </summary>
    [Fact]
    public void CardGrid_ThreePerRow_FourthWraps()
    {
        var grid = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 600, Height = 500,
            Gap = 20
        };

        for (int i = 0; i < 6; i++)
            grid.AddChild(new LayoutNode { Width = 170, Height = 200 });

        YogaLayout.Calculate(grid, 800, 600);

        float gridX = grid.ComputedX;
        float gridY = grid.ComputedY;

        // Row 1: cards 0,1,2
        AssertApprox(gridX, grid.Children[0].ComputedX, "Card0 X");
        AssertApprox(gridX + 190, grid.Children[1].ComputedX, "Card1 X");
        AssertApprox(gridX + 380, grid.Children[2].ComputedX, "Card2 X");
        AssertApprox(gridY, grid.Children[0].ComputedY, "Card0 Y");
        AssertApprox(gridY, grid.Children[1].ComputedY, "Card1 Y");
        AssertApprox(gridY, grid.Children[2].ComputedY, "Card2 Y");

        // Row 2: cards 3,4,5
        float row2Y = gridY + 200 + 20; // height + gap
        AssertApprox(gridX, grid.Children[3].ComputedX, "Card3 X");
        AssertApprox(gridX + 190, grid.Children[4].ComputedX, "Card4 X");
        AssertApprox(gridX + 380, grid.Children[5].ComputedX, "Card5 X");
        AssertApprox(row2Y, grid.Children[3].ComputedY, "Card3 Y");
    }

    // ══════════════════════════════════════════════
    //  Centering patterns
    // ══════════════════════════════════════════════

    /// <summary>
    /// Classic centering: display:flex; justify-content:center; align-items:center
    /// </summary>
    [Fact]
    public void Centering_JustifyAndAlignCenter()
    {
        var container = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            Width = 400, Height = 400,
            JustifyContent = JustifyContent.Center,
            AlignItems = AlignItems.Center
        };
        container.AddChild(new LayoutNode { Width = 100, Height = 100 });

        YogaLayout.Calculate(container, 800, 600);

        float cx = container.ComputedX;
        float cy = container.ComputedY;
        AssertApprox(cx + 150, container.Children[0].ComputedX, "Centered X");
        AssertApprox(cy + 150, container.Children[0].ComputedY, "Centered Y");
    }

    /// <summary>
    /// Centering with margin auto: In a column container, a flex-grow:1 child
    /// fills remaining space (like margin:auto behavior).
    /// </summary>
    [Fact]
    public void FlexGrow_OneChild_FillsAllSpace()
    {
        var container = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 300, Height = 300
        };
        container.AddChild(new LayoutNode { FlexGrow = 1 });

        YogaLayout.Calculate(container, 800, 600);

        AssertApprox(300, container.Children[0].ComputedHeight, "Child fills container");
        AssertApprox(300, container.Children[0].ComputedWidth, "Child stretches cross axis");
    }

    // ══════════════════════════════════════════════
    //  Sticky footer pattern
    // ══════════════════════════════════════════════

    /// <summary>
    /// .page { display:flex; flex-direction:column; min-height:100vh }
    /// .content { flex:1 }
    /// .footer { height:60px }
    /// When content is small, footer sticks to bottom.
    /// </summary>
    [Fact]
    public void StickyFooter_ContentExpandsToFill()
    {
        var page = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 800, Height = 600
        };
        var content = new LayoutNode { FlexGrow = 1 };
        var footer = new LayoutNode { Height = 60 };

        page.AddChild(content);
        page.AddChild(footer);

        YogaLayout.Calculate(page, 800, 600);

        AssertApprox(540, content.ComputedHeight, "Content fills 600-60");
        AssertApprox(page.ComputedY + 540, footer.ComputedY, "Footer at bottom");
    }

    // ══════════════════════════════════════════════
    //  Overflow and absolute positioning
    // ══════════════════════════════════════════════

    /// <summary>
    /// Dropdown menu: parent is relative, dropdown is absolute positioned below.
    /// .trigger { position:relative; width:120px; height:40px }
    /// .dropdown { position:absolute; top:40px; left:0; width:200px; height:300px }
    /// </summary>
    [Fact]
    public void DropdownMenu_AbsolutePositionedBelowTrigger()
    {
        var wrapper = new LayoutNode { Width = 800, Height = 600 };
        var trigger = new LayoutNode { Width = 120, Height = 40 };
        var dropdown = new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 40, PositionLeft = 0,
            Width = 200, Height = 300
        };

        wrapper.AddChild(trigger);
        wrapper.AddChild(dropdown);

        YogaLayout.Calculate(wrapper, 800, 600);

        // Trigger at top-left
        AssertApprox(wrapper.ComputedX, trigger.ComputedX, "Trigger X");
        AssertApprox(wrapper.ComputedY, trigger.ComputedY, "Trigger Y");

        // Dropdown directly below trigger
        AssertApprox(wrapper.ComputedX, dropdown.ComputedX, "Dropdown X");
        AssertApprox(wrapper.ComputedY + 40, dropdown.ComputedY, "Dropdown Y");
        AssertApprox(200, dropdown.ComputedWidth, "Dropdown Width");
        AssertApprox(300, dropdown.ComputedHeight, "Dropdown Height");
    }

    /// <summary>
    /// Modal overlay: position:absolute; top:0; left:0; right:0; bottom:0
    /// Should fill entire parent.
    /// </summary>
    [Fact]
    public void ModalOverlay_FillsParent()
    {
        var root = new LayoutNode { Width = 1920, Height = 1080 };
        var overlay = new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 0, PositionRight = 0,
            PositionBottom = 0, PositionLeft = 0
        };
        root.AddChild(overlay);

        YogaLayout.Calculate(root, 1920, 1080);

        AssertApprox(1920, overlay.ComputedWidth, "Overlay fills width");
        AssertApprox(1080, overlay.ComputedHeight, "Overlay fills height");
        AssertApprox(root.ComputedX, overlay.ComputedX, "Overlay X");
        AssertApprox(root.ComputedY, overlay.ComputedY, "Overlay Y");
    }

    // ══════════════════════════════════════════════
    //  Flex basis vs width
    // ══════════════════════════════════════════════

    /// <summary>
    /// flex-basis takes precedence over width when both are set in flex layout.
    /// .item { flex-basis:200px; width:100px } → item gets 200px on main axis.
    /// </summary>
    [Fact]
    public void FlexBasis_TakesPrecedenceOverWidth()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 600, Height = 100
        };
        root.AddChild(new LayoutNode { Width = 100, FlexBasis = 200 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.Children[0].ComputedWidth, "flex-basis wins over width");
    }

    // ══════════════════════════════════════════════
    //  Nested flex containers
    // ══════════════════════════════════════════════

    /// <summary>
    /// Sidebar layout with nested flex:
    /// .sidebar { display:flex; flex-direction:column; width:250px; height:600px }
    /// .header { height:50px }
    /// .nav { flex:1; display:flex; flex-direction:column; gap:4px }
    /// .nav-item { height:36px }
    /// .footer { height:80px }
    /// </summary>
    [Fact]
    public void SidebarLayout_NestedFlex()
    {
        var sidebar = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 250, Height = 600
        };
        var header = new LayoutNode { Height = 50 };
        var nav = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, FlexGrow = 1, Gap = 4
        };
        var footer = new LayoutNode { Height = 80 };

        for (int i = 0; i < 5; i++)
            nav.AddChild(new LayoutNode { Height = 36 });

        sidebar.AddChild(header);
        sidebar.AddChild(nav);
        sidebar.AddChild(footer);

        YogaLayout.Calculate(sidebar, 800, 600);

        // Nav fills 600 - 50 - 80 = 470
        AssertApprox(470, nav.ComputedHeight, "Nav fills remaining");
        AssertApprox(250, nav.ComputedWidth, "Nav stretches to sidebar width");

        // Nav items have correct positions
        float navY = nav.ComputedY;
        for (int i = 0; i < 5; i++)
        {
            float expectedY = navY + i * (36 + 4);
            AssertApprox(expectedY, nav.Children[i].ComputedY, $"NavItem{i} Y");
            AssertApprox(36, nav.Children[i].ComputedHeight, $"NavItem{i} Height");
        }
    }

    // ══════════════════════════════════════════════
    //  Form layout
    // ══════════════════════════════════════════════

    /// <summary>
    /// .form { display:flex; flex-direction:column; width:400px; gap:12px; padding:20px }
    /// .row { display:flex; flex-direction:row; gap:8px; align-items:center }
    /// .label { width:100px; height:20px }
    /// .input { flex:1; height:32px }
    /// </summary>
    [Fact]
    public void FormLayout_LabelAndInput()
    {
        var form = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 400,
            PaddingTop = 20, PaddingRight = 20, PaddingBottom = 20, PaddingLeft = 20,
            Gap = 12
        };

        for (int i = 0; i < 3; i++)
        {
            var row = new LayoutNode
            {
                FlexDirection = FlexDirection.Row, Gap = 8,
                AlignItems = AlignItems.Center
            };
            row.AddChild(new LayoutNode { Width = 100, Height = 20 });
            row.AddChild(new LayoutNode { FlexGrow = 1, Height = 32 });
            form.AddChild(row);
        }

        CalculateUndefined(form);

        // Content width = 400 - 40 = 360
        // Each row stretches to content width
        for (int i = 0; i < 3; i++)
        {
            var row = form.Children[i];
            AssertApprox(360, row.ComputedWidth, $"Row{i} Width");

            var label = row.Children[0];
            var input = row.Children[1];

            AssertApprox(100, label.ComputedWidth, $"Row{i} Label Width");
            // Input = 360 - 100 - 8 (gap) = 252
            AssertApprox(252, input.ComputedWidth, $"Row{i} Input Width");
            AssertApprox(32, input.ComputedHeight, $"Row{i} Input Height");
        }

        // Form auto-height = padding(20) + 3*row_height + 2*gap(12) + padding(20)
        // Row height = 32 (tallest child, stretch), so 20 + 32*3 + 12*2 + 20 = 160
        AssertApprox(160, form.ComputedHeight, "Form auto height");
    }

    // ══════════════════════════════════════════════
    //  Percentage dimensions
    // ══════════════════════════════════════════════

    /// <summary>
    /// .parent { width:800px; height:600px }
    /// .child { width:50%; height:25% }
    /// Browser: child is 400x150 — percent resolves against parent for any in-flow child.
    /// </summary>
    [Fact]
    public void PercentDimensions_ResolveAgainstParent()
    {
        var parent = new LayoutNode { Width = 800, Height = 600 };
        parent.AddChild(new LayoutNode { WidthPercent = 50, HeightPercent = 25 });

        YogaLayout.Calculate(parent, 800, 600);

        AssertApprox(400, parent.Children[0].ComputedWidth, "50% of 800");
        AssertApprox(150, parent.Children[0].ComputedHeight, "25% of 600");
    }

    // ══════════════════════════════════════════════
    //  Min/max with flex-grow
    // ══════════════════════════════════════════════

    /// <summary>
    /// .container { display:flex; flex-direction:row; width:600px }
    /// .item1 { flex:1; max-width:200px }
    /// .item2 { flex:1 }
    /// Item1 grows but is capped at max-width. Item2 takes the rest.
    /// </summary>
    [Fact]
    public void FlexGrow_WithMaxWidth_CapsGrowth()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 600, Height = 100
        };
        root.AddChild(new LayoutNode { FlexGrow = 1, MaxWidth = 200 });
        root.AddChild(new LayoutNode { FlexGrow = 1 });

        YogaLayout.Calculate(root, 800, 600);

        Assert.True(root.Children[0].ComputedWidth <= 200 + Tolerance,
            $"Child0 capped at max-width: {root.Children[0].ComputedWidth}");
    }

    /// <summary>
    /// .container { display:flex; flex-direction:row; width:200px }
    /// .item { flex-shrink:1; width:300px; min-width:150px }
    /// Item shrinks but not below min-width.
    /// </summary>
    [Fact]
    public void FlexShrink_WithMinWidth_FloorsAtMin()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 200, Height = 50
        };
        root.AddChild(new LayoutNode { Width = 300, FlexShrink = 1, MinWidth = 150 });

        YogaLayout.Calculate(root, 800, 600);

        Assert.True(root.Children[0].ComputedWidth >= 150 - Tolerance,
            $"Child0 min-width respected: {root.Children[0].ComputedWidth}");
    }

    // ══════════════════════════════════════════════
    //  Stretch with padding
    // ══════════════════════════════════════════════

    /// <summary>
    /// Browser: stretch respects parent padding.
    /// .parent { width:300px; height:200px; padding:20px; align-items:stretch }
    /// .child { } → child width = 300 - 40 = 260
    /// </summary>
    [Fact]
    public void Stretch_RespectsParentPadding()
    {
        var root = new LayoutNode
        {
            Width = 300, Height = 200,
            PaddingTop = 20, PaddingRight = 20, PaddingBottom = 20, PaddingLeft = 20,
            AlignItems = AlignItems.Stretch
        };
        root.AddChild(new LayoutNode { Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(260, root.Children[0].ComputedWidth, "Stretched width = parent - padding");
    }

    /// <summary>
    /// Stretch with child margin: child should be narrower.
    /// .parent { width:300px; align-items:stretch }
    /// .child { margin:10px } → child width = 300 - 20 = 280
    /// </summary>
    [Fact]
    public void Stretch_RespectsChildMargin()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 300, Height = 200,
            AlignItems = AlignItems.Stretch
        };
        root.AddChild(new LayoutNode { Height = 50, MarginLeft = 10, MarginRight = 10 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(280, root.Children[0].ComputedWidth, "Stretched width minus margins");
    }

    // ══════════════════════════════════════════════
    //  Chat/message layout
    // ══════════════════════════════════════════════

    /// <summary>
    /// Chat bubble: avatar left, message right with auto height.
    /// .msg { display:flex; flex-direction:row; gap:8px; padding:8px; align-items:flex-start }
    /// .avatar { width:32px; height:32px }
    /// .bubble { flex:1 } → contains text of known measured size
    /// </summary>
    [Fact]
    public void ChatLayout_AvatarAndBubble()
    {
        var msg = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 400,
            PaddingTop = 8, PaddingRight = 8, PaddingBottom = 8, PaddingLeft = 8,
            Gap = 8,
            AlignItems = AlignItems.FlexStart
        };
        var avatar = new LayoutNode { Width = 32, Height = 32 };
        var bubble = new LayoutNode { FlexGrow = 1 };

        // Text inside bubble
        bubble.AddChild(new LayoutNode
        {
            MeasureFunc = (w, wm, h, hm) =>
            {
                float availW = wm == MeasureMode.Undefined ? 1000 : w;
                return (System.Math.Min(300, availW), 60); // 3 lines of text
            }
        });

        msg.AddChild(avatar);
        msg.AddChild(bubble);

        CalculateUndefined(msg);

        // Inner = 400 - 16 = 384
        // Avatar = 32, gap = 8, bubble = 384 - 32 - 8 = 344
        AssertApprox(32, avatar.ComputedWidth, "Avatar width");
        AssertApprox(344, bubble.ComputedWidth, "Bubble fills remaining");
        AssertApprox(60, bubble.ComputedHeight, "Bubble auto-height from text");

        // Message auto-height = pad(8) + max(avatar=32, bubble=60) + pad(8) = 76
        AssertApprox(76, msg.ComputedHeight, "Message auto height");
    }

    // ══════════════════════════════════════════════
    //  Aspect ratio in absolute positioning
    // ══════════════════════════════════════════════

    /// <summary>
    /// Absolute positioned element with aspect ratio.
    /// .box { position:absolute; left:10; top:10; width:200px; aspect-ratio:16/9 }
    /// </summary>
    [Fact]
    public void AbsoluteWithAspectRatio()
    {
        var root = new LayoutNode { Width = 800, Height = 600 };
        root.AddChild(new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionLeft = 10, PositionTop = 10,
            Width = 320, AspectRatio = 16f / 9f
        });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(320, root.Children[0].ComputedWidth, "Width");
        AssertApprox(180, root.Children[0].ComputedHeight, "Height (320 / 16*9)");
    }

    // ══════════════════════════════════════════════
    //  Flex-shrink: 0 (no shrink)
    // ══════════════════════════════════════════════

    /// <summary>
    /// An item with flex-shrink:0 should not shrink below its basis.
    /// .container { display:flex; flex-direction:row; width:200px }
    /// .fixed { width:150px; flex-shrink:0 }
    /// .flex { width:100px; flex-shrink:1 }
    /// </summary>
    [Fact]
    public void FlexShrinkZero_DoesNotShrink()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 200, Height = 50
        };
        root.AddChild(new LayoutNode { Width = 150, FlexShrink = 0 });
        root.AddChild(new LayoutNode { Width = 100, FlexShrink = 1 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(150, root.Children[0].ComputedWidth, "Fixed item stays 150");
        AssertApprox(50, root.Children[1].ComputedWidth, "Flex item absorbs overflow");
    }

    // ══════════════════════════════════════════════
    //  Intrinsic sizing (auto width/height)
    // ══════════════════════════════════════════════

    /// <summary>
    /// A button with padding and text child auto-sizes correctly.
    /// Browser: inline-flex button with padding wraps tight around content.
    /// </summary>
    [Fact]
    public void AutoSize_ButtonWithPaddingAndText()
    {
        var button = new LayoutNode
        {
            PaddingTop = 8, PaddingBottom = 8,
            PaddingLeft = 16, PaddingRight = 16,
            AlignItems = AlignItems.FlexStart
        };
        button.AddChild(new LayoutNode
        {
            MeasureFunc = (w, wm, h, hm) => (60, 16) // text measures 60x16
        });

        CalculateUndefined(button);

        AssertApprox(92, button.ComputedWidth, "Button width = 60 + 32 padding");
        AssertApprox(32, button.ComputedHeight, "Button height = 16 + 16 padding");
    }

    // ══════════════════════════════════════════════
    //  Reverse with justify-content
    // ══════════════════════════════════════════════

    /// <summary>
    /// .container { display:flex; flex-direction:row-reverse; width:300px; justify-content:flex-start }
    /// .child { width:50px }
    /// Browser: flex-start in row-reverse means right edge. First child at X=250.
    /// </summary>
    [Fact]
    public void RowReverse_FlexStart_PacksFromRight()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.RowReverse, Width = 300, Height = 50,
            JustifyContent = JustifyContent.FlexStart
        };
        root.AddChild(new LayoutNode { Width = 50 });
        root.AddChild(new LayoutNode { Width = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        // row-reverse + flex-start: first child at right edge
        AssertApprox(250, root.Children[0].ComputedX - rootX, "Child0 at right");
        AssertApprox(200, root.Children[1].ComputedX - rootX, "Child1 next to child0");
    }

    /// <summary>
    /// .container { display:flex; flex-direction:row-reverse; width:300px; justify-content:center }
    /// .child { width:50px }
    /// Browser: items centered, but in reverse order (first child rightmost).
    /// Center of 100px items in 300px = 100px leading on each side.
    /// Reversed: child1 at X=100, child0 at X=150.
    /// </summary>
    [Fact]
    public void RowReverse_Center_CenteredInReverseOrder()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.RowReverse, Width = 300, Height = 50,
            JustifyContent = JustifyContent.Center
        };
        root.AddChild(new LayoutNode { Width = 50 });
        root.AddChild(new LayoutNode { Width = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        // Center is not affected by reverse direction.
        // Reversed iteration: child1 placed first (left), child0 placed second (right)
        AssertApprox(150, root.Children[0].ComputedX - rootX, "Child0 (rightmost in center)");
        AssertApprox(100, root.Children[1].ComputedX - rootX, "Child1 (leftmost in center)");
    }

    /// <summary>
    /// .container { display:flex; flex-direction:column-reverse; height:400px; justify-content:flex-end }
    /// .child { height:50px }
    /// Browser: flex-end in column-reverse means top edge.
    /// </summary>
    [Fact]
    public void ColumnReverse_FlexEnd_PacksAtTop()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.ColumnReverse, Width = 100, Height = 400,
            JustifyContent = JustifyContent.FlexEnd
        };
        root.AddChild(new LayoutNode { Height = 50 });
        root.AddChild(new LayoutNode { Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        // column-reverse + flex-end = items at top, reversed order
        AssertApprox(50, root.Children[0].ComputedY - rootY, "Child0 Y");
        AssertApprox(0, root.Children[1].ComputedY - rootY, "Child1 Y");
    }

    // ══════════════════════════════════════════════
    //  Percent dimensions (in-flow children)
    // ══════════════════════════════════════════════

    /// <summary>
    /// .parent { display:flex; flex-direction:row; width:600px; height:400px }
    /// .child { width:33.33%; height:50% }
    /// Browser: child is 200x200.
    /// </summary>
    [Fact]
    public void PercentWidth_InFlowRow_ResolvesOnMainAxis()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 600, Height = 400,
            AlignItems = AlignItems.FlexStart
        };
        root.AddChild(new LayoutNode { WidthPercent = 33.33f, HeightPercent = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.Children[0].ComputedWidth, "33.33% of 600");
        AssertApprox(200, root.Children[0].ComputedHeight, "50% of 400");
    }

    /// <summary>
    /// .parent { display:flex; flex-direction:column; width:400px; height:600px }
    /// .child { width:75%; height:50% }
    /// Browser: child is 300x300.
    /// </summary>
    [Fact]
    public void PercentHeight_InFlowColumn_ResolvesOnMainAxis()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 400, Height = 600,
            AlignItems = AlignItems.FlexStart
        };
        root.AddChild(new LayoutNode { WidthPercent = 75, HeightPercent = 50 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(300, root.Children[0].ComputedWidth, "75% of 400");
        AssertApprox(300, root.Children[0].ComputedHeight, "50% of 600");
    }

    /// <summary>
    /// Three children each 33.33% wide in a row → they fill the container.
    /// .parent { display:flex; flex-direction:row; width:900px }
    /// .child { width:33.33% }
    /// </summary>
    [Fact]
    public void PercentWidth_ThreeChildren_FillRow()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 900, Height = 100
        };
        for (int i = 0; i < 3; i++)
            root.AddChild(new LayoutNode { WidthPercent = 33.33f });

        YogaLayout.Calculate(root, 900, 600);

        float rootX = root.ComputedX;
        AssertApprox(300, root.Children[0].ComputedWidth, "Child0 33.33%");
        AssertApprox(300, root.Children[1].ComputedWidth, "Child1 33.33%");
        AssertApprox(300, root.Children[2].ComputedWidth, "Child2 33.33%");
        AssertApprox(rootX, root.Children[0].ComputedX, "Child0 X");
        AssertApprox(rootX + 300, root.Children[1].ComputedX, "Child1 X");
        AssertApprox(rootX + 600, root.Children[2].ComputedX, "Child2 X");
    }

    // ══════════════════════════════════════════════
    //  Aspect ratio
    // ══════════════════════════════════════════════

    /// <summary>
    /// .box { width:320px; aspect-ratio:16/9 }
    /// Browser: height = 320 / (16/9) = 180px
    /// </summary>
    [Fact]
    public void AspectRatio_16by9_ResolvesHeight()
    {
        var root = new LayoutNode { Width = 800, Height = 600 };
        root.AddChild(new LayoutNode
        {
            Width = 320, AspectRatio = 16f / 9f,
            AlignSelf = AlignSelf.FlexStart
        });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(320, root.Children[0].ComputedWidth, "Width");
        AssertApprox(180, root.Children[0].ComputedHeight, "Height (16:9)");
    }

    /// <summary>
    /// .box { height:200px; aspect-ratio:2 }
    /// Browser: width = 200 * 2 = 400px
    /// </summary>
    [Fact]
    public void AspectRatio_WidthFromHeight_InFlow()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 800, Height = 600,
            AlignItems = AlignItems.FlexStart
        };
        root.AddChild(new LayoutNode { Height = 200, AspectRatio = 2f });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(400, root.Children[0].ComputedWidth, "Width = height * ratio");
        AssertApprox(200, root.Children[0].ComputedHeight, "Height stays 200");
    }

    /// <summary>
    /// .square { width:100px; aspect-ratio:1 }
    /// Browser: 100x100 square
    /// </summary>
    [Fact]
    public void AspectRatio_Square()
    {
        var root = new LayoutNode { Width = 800, Height = 600 };
        root.AddChild(new LayoutNode
        {
            Width = 100, AspectRatio = 1f,
            AlignSelf = AlignSelf.FlexStart
        });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(100, root.Children[0].ComputedWidth, "Width");
        AssertApprox(100, root.Children[0].ComputedHeight, "Height = Width for 1:1");
    }

    // ══════════════════════════════════════════════
    //  Tab bar layout
    // ══════════════════════════════════════════════

    /// <summary>
    /// .tabs { display:flex; flex-direction:row; width:600px; height:44px }
    /// .tab { flex:1; height:44px }
    /// Browser: 4 tabs each 150px wide, filling the bar.
    /// </summary>
    [Fact]
    public void TabBar_EqualWidthTabs()
    {
        var tabs = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 600, Height = 44
        };
        for (int i = 0; i < 4; i++)
            tabs.AddChild(new LayoutNode { FlexGrow = 1, Height = 44 });

        YogaLayout.Calculate(tabs, 800, 600);

        float tabsX = tabs.ComputedX;
        for (int i = 0; i < 4; i++)
        {
            AssertApprox(150, tabs.Children[i].ComputedWidth, $"Tab{i} Width");
            AssertApprox(tabsX + i * 150, tabs.Children[i].ComputedX, $"Tab{i} X");
        }
    }

    // ══════════════════════════════════════════════
    //  Toolbar with fixed + flex items
    // ══════════════════════════════════════════════

    /// <summary>
    /// .toolbar { display:flex; flex-direction:row; width:800px; height:40px; gap:8px; padding:0 12px; align-items:center }
    /// .search { flex:1; height:28px }
    /// .icon { width:28px; height:28px }
    /// Layout: [icon][icon][search         ][icon]
    /// </summary>
    [Fact]
    public void Toolbar_FixedAndFlexItems()
    {
        var toolbar = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 800, Height = 40,
            PaddingLeft = 12, PaddingRight = 12,
            Gap = 8, AlignItems = AlignItems.Center
        };

        var icon1 = new LayoutNode { Width = 28, Height = 28 };
        var icon2 = new LayoutNode { Width = 28, Height = 28 };
        var search = new LayoutNode { FlexGrow = 1, Height = 28 };
        var icon3 = new LayoutNode { Width = 28, Height = 28 };

        toolbar.AddChild(icon1);
        toolbar.AddChild(icon2);
        toolbar.AddChild(search);
        toolbar.AddChild(icon3);

        YogaLayout.Calculate(toolbar, 800, 600);

        // Inner width = 800 - 24 = 776
        // Fixed items = 28*3 = 84, gaps = 8*3 = 24, search = 776 - 84 - 24 = 668
        AssertApprox(28, icon1.ComputedWidth, "Icon1 Width");
        AssertApprox(28, icon2.ComputedWidth, "Icon2 Width");
        AssertApprox(668, search.ComputedWidth, "Search fills remaining");
        AssertApprox(28, icon3.ComputedWidth, "Icon3 Width");

        // All vertically centered: (40-28)/2 = 6
        float toolY = toolbar.ComputedY;
        AssertApprox(toolY + 6, icon1.ComputedY, "Icon1 centered Y");
        AssertApprox(toolY + 6, search.ComputedY, "Search centered Y");
    }

    // ══════════════════════════════════════════════
    //  Dialog/modal with centered content
    // ══════════════════════════════════════════════

    /// <summary>
    /// .overlay { position:absolute; inset:0; display:flex; justify-content:center; align-items:center }
    /// .dialog { width:480px; height:320px }
    /// Browser: dialog centered in viewport.
    /// </summary>
    [Fact]
    public void CenteredDialog_InAbsoluteOverlay()
    {
        var viewport = new LayoutNode { Width = 1920, Height = 1080 };
        var overlay = new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 0, PositionRight = 0, PositionBottom = 0, PositionLeft = 0,
            FlexDirection = FlexDirection.Row,
            JustifyContent = JustifyContent.Center,
            AlignItems = AlignItems.Center
        };
        var dialog = new LayoutNode { Width = 480, Height = 320 };

        overlay.AddChild(dialog);
        viewport.AddChild(overlay);

        YogaLayout.Calculate(viewport, 1920, 1080);

        AssertApprox(1920, overlay.ComputedWidth, "Overlay fills viewport width");
        AssertApprox(1080, overlay.ComputedHeight, "Overlay fills viewport height");

        // Dialog centered: (1920-480)/2 = 720, (1080-320)/2 = 380
        AssertApprox(720, dialog.ComputedX - viewport.ComputedX, "Dialog centered X");
        AssertApprox(380, dialog.ComputedY - viewport.ComputedY, "Dialog centered Y");
    }

    // ══════════════════════════════════════════════
    //  Tag/chip row that wraps
    // ══════════════════════════════════════════════

    /// <summary>
    /// .tags { display:flex; flex-wrap:wrap; gap:6px; width:200px }
    /// .tag { width:60px; height:24px }
    /// Browser: 3 per row (60*3 + 6*2 = 192 < 200), wraps after 3.
    /// </summary>
    [Fact]
    public void TagRow_WrapsCorrectly()
    {
        var tags = new LayoutNode
        {
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Width = 200, Height = 200,
            Gap = 6
        };
        for (int i = 0; i < 7; i++)
            tags.AddChild(new LayoutNode { Width = 60, Height = 24 });

        YogaLayout.Calculate(tags, 800, 600);

        float tagsX = tags.ComputedX;
        float tagsY = tags.ComputedY;

        // Row 1: tags 0,1,2 (60+6+60+6+60 = 192 < 200)
        AssertApprox(tagsX, tags.Children[0].ComputedX, "Tag0 X");
        AssertApprox(tagsX + 66, tags.Children[1].ComputedX, "Tag1 X");
        AssertApprox(tagsX + 132, tags.Children[2].ComputedX, "Tag2 X");
        AssertApprox(tagsY, tags.Children[0].ComputedY, "Tag0 Y row1");

        // Row 2: tags 3,4,5
        float row2Y = tagsY + 24 + 6;
        AssertApprox(tagsX, tags.Children[3].ComputedX, "Tag3 X");
        AssertApprox(row2Y, tags.Children[3].ComputedY, "Tag3 Y row2");

        // Row 3: tag 6
        float row3Y = row2Y + 24 + 6;
        AssertApprox(tagsX, tags.Children[6].ComputedX, "Tag6 X");
        AssertApprox(row3Y, tags.Children[6].ComputedY, "Tag6 Y row3");
    }

    // ══════════════════════════════════════════════
    //  Split pane layout
    // ══════════════════════════════════════════════

    /// <summary>
    /// .split { display:flex; flex-direction:row; width:1200px; height:800px }
    /// .left { width:300px }
    /// .divider { width:4px }
    /// .right { flex:1 }
    /// </summary>
    [Fact]
    public void SplitPane_FixedLeftFlexRight()
    {
        var split = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 1200, Height = 800
        };
        var left = new LayoutNode { Width = 300 };
        var divider = new LayoutNode { Width = 4 };
        var right = new LayoutNode { FlexGrow = 1 };

        split.AddChild(left);
        split.AddChild(divider);
        split.AddChild(right);

        YogaLayout.Calculate(split, 1200, 800);

        AssertApprox(300, left.ComputedWidth, "Left pane");
        AssertApprox(4, divider.ComputedWidth, "Divider");
        AssertApprox(896, right.ComputedWidth, "Right pane (1200-300-4)");

        // All stretch to full height
        AssertApprox(800, left.ComputedHeight, "Left height stretched");
        AssertApprox(800, right.ComputedHeight, "Right height stretched");
    }

    // ══════════════════════════════════════════════
    //  Nested percent
    // ══════════════════════════════════════════════

    /// <summary>
    /// .outer { width:1000px; height:800px }
    /// .inner { width:50%; height:50% }
    /// .deep { width:50%; height:50% }
    /// Browser: deep = 250x200
    /// </summary>
    [Fact]
    public void NestedPercent_ResolvesAgainstEachParent()
    {
        var outer = new LayoutNode { Width = 1000, Height = 800 };
        var inner = new LayoutNode { WidthPercent = 50, HeightPercent = 50 };
        var deep = new LayoutNode
        {
            WidthPercent = 50, HeightPercent = 50,
            AlignSelf = AlignSelf.FlexStart
        };

        inner.AddChild(deep);
        outer.AddChild(inner);

        YogaLayout.Calculate(outer, 1000, 800);

        AssertApprox(500, inner.ComputedWidth, "Inner 50% of 1000");
        AssertApprox(400, inner.ComputedHeight, "Inner 50% of 800");
        AssertApprox(250, deep.ComputedWidth, "Deep 50% of 500");
        AssertApprox(200, deep.ComputedHeight, "Deep 50% of 400");
    }

    // ══════════════════════════════════════════════
    //  Absolute inset with padding
    // ══════════════════════════════════════════════

    /// <summary>
    /// .parent { position:relative; width:400px; height:300px; padding:20px }
    /// .child { position:absolute; top:0; left:0; right:0; bottom:0 }
    /// Browser: child spans full parent box (ignoring padding for absolute positioning).
    /// The child's top:0/left:0 is relative to the padding box, so it starts at the
    /// padding edge and spans to the padding edge on all sides.
    /// </summary>
    [Fact]
    public void Absolute_InsetZero_WithParentPadding()
    {
        var parent = new LayoutNode
        {
            Width = 400, Height = 300,
            PaddingTop = 20, PaddingRight = 20, PaddingBottom = 20, PaddingLeft = 20
        };
        var child = new LayoutNode
        {
            Position = PositionType.Absolute,
            PositionTop = 0, PositionRight = 0, PositionBottom = 0, PositionLeft = 0
        };
        parent.AddChild(child);

        YogaLayout.Calculate(parent, 800, 600);

        // CSS absolute inset:0 with padding = fills content box
        AssertApprox(360, child.ComputedWidth, "Width = 400 - 20 - 20");
        AssertApprox(260, child.ComputedHeight, "Height = 300 - 20 - 20");
    }

    // ══════════════════════════════════════════════
    //  Mixed fixed + percent + grow
    // ══════════════════════════════════════════════

    /// <summary>
    /// .container { display:flex; flex-direction:row; width:1000px; height:100px }
    /// .fixed { width:200px }
    /// .percent { width:30% }
    /// .flex { flex:1 }
    /// Browser: fixed=200, percent=300, flex=500
    /// </summary>
    [Fact]
    public void MixedSizing_FixedPercentFlex()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 1000, Height = 100
        };
        root.AddChild(new LayoutNode { Width = 200 });
        root.AddChild(new LayoutNode { WidthPercent = 30 });
        root.AddChild(new LayoutNode { FlexGrow = 1 });

        YogaLayout.Calculate(root, 1000, 600);

        AssertApprox(200, root.Children[0].ComputedWidth, "Fixed 200");
        AssertApprox(300, root.Children[1].ComputedWidth, "30% of 1000");
        AssertApprox(500, root.Children[2].ComputedWidth, "Flex fills 1000-200-300");
    }

    // ══════════════════════════════════════════════
    //  Scroll container pattern
    // ══════════════════════════════════════════════

    /// <summary>
    /// .scroll-container { display:flex; flex-direction:column; width:300px; height:400px; overflow:hidden }
    /// .header { height:50px }
    /// .scroll-area { flex:1; overflow:scroll }
    /// .footer { height:50px }
    /// Browser: scroll-area gets 300px height (400 - 50 - 50).
    /// </summary>
    [Fact]
    public void ScrollContainer_FlexAreaFillsMiddle()
    {
        var container = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 300, Height = 400
        };
        var header = new LayoutNode { Height = 50 };
        var scrollArea = new LayoutNode { FlexGrow = 1 };
        var footer = new LayoutNode { Height = 50 };

        container.AddChild(header);
        container.AddChild(scrollArea);
        container.AddChild(footer);

        YogaLayout.Calculate(container, 800, 600);

        AssertApprox(50, header.ComputedHeight, "Header");
        AssertApprox(300, scrollArea.ComputedHeight, "Scroll area fills 400-50-50");
        AssertApprox(50, footer.ComputedHeight, "Footer");
        AssertApprox(container.ComputedY + 350, footer.ComputedY, "Footer at bottom");
    }

    // ══════════════════════════════════════════════
    //  Display none in flex
    // ══════════════════════════════════════════════

    /// <summary>
    /// Hidden items should not take space or affect sibling positions.
    /// .row { display:flex; flex-direction:row; width:300px; gap:10px }
    /// .item { width:50px; height:50px }
    /// .hidden { display:none }
    /// Browser: visible items at 0, 60, 120 — hidden item takes no space.
    /// </summary>
    [Fact]
    public void DisplayNone_ItemSkippedInFlexRow()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 300, Height = 100, Gap = 10
        };
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });
        root.AddChild(new LayoutNode { Width = 50, Height = 50, Display = false });
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });
        root.AddChild(new LayoutNode { Width = 50, Height = 50 });

        YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        AssertApprox(rootX, root.Children[0].ComputedX, "Child0 X");
        // Child1 is hidden, child2 should be at 50+10=60
        AssertApprox(rootX + 60, root.Children[2].ComputedX, "Child2 X (after hidden)");
        AssertApprox(rootX + 120, root.Children[3].ComputedX, "Child3 X");
    }

    // ══════════════════════════════════════════════
    //  Flex-grow with unequal basis
    // ══════════════════════════════════════════════

    /// <summary>
    /// .row { display:flex; flex-direction:row; width:500px }
    /// .a { width:100px; flex-grow:1 }
    /// .b { width:200px; flex-grow:1 }
    /// Browser: remaining = 200, each gets +100. a=200, b=300.
    /// </summary>
    [Fact]
    public void FlexGrow_UnequalBasis_DistributesEqually()
    {
        var root = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 500, Height = 50
        };
        root.AddChild(new LayoutNode { Width = 100, FlexGrow = 1 });
        root.AddChild(new LayoutNode { Width = 200, FlexGrow = 1 });

        YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.Children[0].ComputedWidth, "A: 100 + 100");
        AssertApprox(300, root.Children[1].ComputedWidth, "B: 200 + 100");
    }

    // ══════════════════════════════════════════════
    //  Margin auto equivalent with space-between
    // ══════════════════════════════════════════════

    /// <summary>
    /// .header { display:flex; flex-direction:row; width:800px; height:48px; justify-content:space-between; align-items:center }
    /// .left-group { display:flex; gap:8px }
    /// .right-group { display:flex; gap:8px }
    /// .item { width:40px; height:40px }
    /// Browser: left group at start, right group at end.
    /// </summary>
    [Fact]
    public void SpaceBetween_TwoGroups_PushToEdges()
    {
        var header = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Width = 800, Height = 48,
            JustifyContent = JustifyContent.SpaceBetween,
            AlignItems = AlignItems.Center
        };
        var leftGroup = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Gap = 8,
            AlignItems = AlignItems.FlexStart
        };
        leftGroup.AddChild(new LayoutNode { Width = 40, Height = 40 });
        leftGroup.AddChild(new LayoutNode { Width = 40, Height = 40 });

        var rightGroup = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Gap = 8,
            AlignItems = AlignItems.FlexStart
        };
        rightGroup.AddChild(new LayoutNode { Width = 40, Height = 40 });

        header.AddChild(leftGroup);
        header.AddChild(rightGroup);

        YogaLayout.Calculate(header, 800, 600);

        float headerX = header.ComputedX;
        AssertApprox(headerX, leftGroup.ComputedX, "Left group at start");
        // Right group: X = 800 - rightGroup width (40)
        AssertApprox(headerX + 760, rightGroup.ComputedX, "Right group at end");
        // Left group width = 40 + 8 + 40 = 88
        AssertApprox(88, leftGroup.ComputedWidth, "Left group auto width");
    }

    // ══════════════════════════════════════════════
    //  Settings panel layout
    // ══════════════════════════════════════════════

    /// <summary>
    /// Real-world settings panel:
    /// .panel { display:flex; flex-direction:column; width:500px; padding:24px; gap:16px }
    /// .section { display:flex; flex-direction:column; gap:8px }
    /// .section-title { height:20px }
    /// .setting-row { display:flex; flex-direction:row; justify-content:space-between; align-items:center; height:36px }
    /// .label { flex:1 }
    /// .toggle { width:48px; height:24px }
    /// </summary>
    [Fact]
    public void SettingsPanel_SectionsWithRows()
    {
        var panel = new LayoutNode
        {
            FlexDirection = FlexDirection.Column, Width = 500,
            PaddingTop = 24, PaddingRight = 24, PaddingBottom = 24, PaddingLeft = 24,
            Gap = 16
        };

        // Section 1: title + 2 settings
        var section1 = new LayoutNode { FlexDirection = FlexDirection.Column, Gap = 8 };
        section1.AddChild(new LayoutNode { Height = 20 }); // title

        for (int i = 0; i < 2; i++)
        {
            var row = new LayoutNode
            {
                FlexDirection = FlexDirection.Row, Height = 36,
                JustifyContent = JustifyContent.SpaceBetween,
                AlignItems = AlignItems.Center
            };
            row.AddChild(new LayoutNode { FlexGrow = 1, Height = 20 }); // label
            row.AddChild(new LayoutNode { Width = 48, Height = 24 }); // toggle
            section1.AddChild(row);
        }
        panel.AddChild(section1);

        // Section 2: title + 1 setting
        var section2 = new LayoutNode { FlexDirection = FlexDirection.Column, Gap = 8 };
        section2.AddChild(new LayoutNode { Height = 20 }); // title
        var row2 = new LayoutNode
        {
            FlexDirection = FlexDirection.Row, Height = 36,
            JustifyContent = JustifyContent.SpaceBetween,
            AlignItems = AlignItems.Center
        };
        row2.AddChild(new LayoutNode { FlexGrow = 1, Height = 20 });
        row2.AddChild(new LayoutNode { Width = 48, Height = 24 });
        section2.AddChild(row2);
        panel.AddChild(section2);

        CalculateUndefined(panel);

        float contentW = 500 - 48; // 452
        // Section1: title(20) + gap(8) + row(36) + gap(8) + row(36) = 108
        // Section2: title(20) + gap(8) + row(36) = 64
        // Panel: pad(24) + s1(108) + gap(16) + s2(64) + pad(24) = 236
        AssertApprox(236, panel.ComputedHeight, "Panel auto height");
        AssertApprox(contentW, section1.ComputedWidth, "Section1 stretches");
        AssertApprox(contentW, section2.ComputedWidth, "Section2 stretches");

        // Each row stretches to section width, toggle at far right
        var settingRow = section1.Children[1]; // first setting row
        AssertApprox(contentW, settingRow.ComputedWidth, "Setting row width");
    }
}
