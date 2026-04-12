using ReactUI.Style;
using Xunit;
using S = ReactUI.Style.Style;
using LayoutNode = ReactUI.Layout.LayoutNode;

namespace ReactUI.Tests;

/// <summary>
/// Tests that verify layout computation when styles come from StyleSheets
/// and LayoutBridge, rather than being set directly on LayoutNodes.
/// Exercises the full pipeline: StyleSheet → Resolve → LayoutBridge → ReactUI.Layout.YogaLayout.
/// </summary>
public class StylesheetLayoutTests
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
        var method = typeof(ReactUI.Layout.YogaLayout).GetMethod(
            "LayoutInternal",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method!.Invoke(null, new object[]
        {
            node, float.NaN, float.NaN,
            ReactUI.Layout.MeasureMode.Undefined, ReactUI.Layout.MeasureMode.Undefined
        });
    }

    /// <summary>
    /// Helper: create a LayoutNode from a resolved style.
    /// </summary>
    private static LayoutNode Node(S style)
    {
        var node = new LayoutNode();
        ReactUI.Layout.LayoutBridge.ApplyStyle(node, style);
        return node;
    }

    /// <summary>
    /// Helper: create a LayoutNode from a stylesheet class name.
    /// </summary>
    private static LayoutNode Node(StyleSheet sheet, string className)
    {
        var node = new LayoutNode();
        ReactUI.Layout.LayoutBridge.ApplyStyle(node, sheet.Resolve(className));
        return node;
    }

    /// <summary>
    /// Helper: create a LayoutNode with resolved style (class + inline override).
    /// </summary>
    private static LayoutNode Node(StyleSheet sheet, string className, S inline)
    {
        var node = new LayoutNode();
        ReactUI.Layout.LayoutBridge.ApplyStyle(node, sheet.Resolve(className, inline));
        return node;
    }

    // ══════════════════════════════════════════════
    //  Basic: StyleSheet → LayoutBridge → Layout
    // ══════════════════════════════════════════════

    [Fact]
    public void BasicStylesheet_FixedSize()
    {
        var styles = new StyleSheet
        {
            [".box"] = new S
            {
                Width = StyleValue.Px(200),
                Height = StyleValue.Px(100),
            }
        };

        var root = Node(styles, "box");
        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.ComputedWidth, "Width");
        AssertApprox(100, root.ComputedHeight, "Height");
    }

    [Fact]
    public void Stylesheet_PercentDimensions()
    {
        var styles = new StyleSheet
        {
            [".container"] = new S
            {
                Width = StyleValue.Px(400),
                Height = StyleValue.Px(300),
            },
            [".half"] = new S
            {
                Width = StyleValue.Pct(50),
                Height = StyleValue.Pct(50),
            }
        };

        var root = Node(styles, "container");
        var child = Node(styles, "half");
        root.AddChild(child);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, child.ComputedWidth, "50% of 400");
        AssertApprox(150, child.ComputedHeight, "50% of 300");
    }

    // ══════════════════════════════════════════════
    //  Flex container from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_RowLayout_WithGap()
    {
        var styles = new StyleSheet
        {
            [".row"] = new S
            {
                FlexDirection = FlexDirection.Row,
                Width = StyleValue.Px(400),
                Height = StyleValue.Px(100),
                Gap = 10,
            },
            [".item"] = new S
            {
                Width = StyleValue.Px(80),
                Height = StyleValue.Px(80),
            }
        };

        var root = Node(styles, "row");
        for (int i = 0; i < 3; i++)
            root.AddChild(Node(styles, "item"));

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        AssertApprox(0, root.Children[0].ComputedX - rootX, "Item0 X");
        AssertApprox(90, root.Children[1].ComputedX - rootX, "Item1 X (80+10)");
        AssertApprox(180, root.Children[2].ComputedX - rootX, "Item2 X (160+20)");
    }

    [Fact]
    public void Stylesheet_ColumnLayout_FlexGrow()
    {
        var styles = new StyleSheet
        {
            [".page"] = new S
            {
                FlexDirection = FlexDirection.Column,
                Width = StyleValue.Px(800),
                Height = StyleValue.Px(600),
            },
            [".header"] = new S { Height = StyleValue.Px(60) },
            [".content"] = new S { FlexGrow = 1 },
            [".footer"] = new S { Height = StyleValue.Px(40) },
        };

        var root = Node(styles, "page");
        var header = Node(styles, "header");
        var content = Node(styles, "content");
        var footer = Node(styles, "footer");
        root.AddChild(header);
        root.AddChild(content);
        root.AddChild(footer);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(60, header.ComputedHeight, "Header");
        AssertApprox(500, content.ComputedHeight, "Content fills 600-60-40");
        AssertApprox(40, footer.ComputedHeight, "Footer");
    }

    // ══════════════════════════════════════════════
    //  Padding and margin from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_Padding_OffsetsChildren()
    {
        var styles = new StyleSheet
        {
            [".padded"] = new S
            {
                Width = StyleValue.Px(300),
                Height = StyleValue.Px(200),
                Padding = new EdgeValues(20),
                AlignItems = AlignItems.Stretch,
            },
            [".child"] = new S
            {
                Height = StyleValue.Px(50),
            }
        };

        var root = Node(styles, "padded");
        var child = Node(styles, "child");
        root.AddChild(child);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(20, child.ComputedX - root.ComputedX, "Child X offset by padding");
        AssertApprox(20, child.ComputedY - root.ComputedY, "Child Y offset by padding");
        AssertApprox(260, child.ComputedWidth, "Stretched width = 300 - 40");
    }

    [Fact]
    public void Stylesheet_Margin_PushesChild()
    {
        var styles = new StyleSheet
        {
            [".container"] = new S
            {
                Width = StyleValue.Px(300),
                Height = StyleValue.Px(200),
            },
            [".spaced"] = new S
            {
                Width = StyleValue.Px(100),
                Height = StyleValue.Px(50),
                Margin = new EdgeValues(15, 20),
            }
        };

        var root = Node(styles, "container");
        var child = Node(styles, "spaced");
        root.AddChild(child);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(20, child.ComputedX - root.ComputedX, "Margin left");
        AssertApprox(15, child.ComputedY - root.ComputedY, "Margin top");
    }

    // ══════════════════════════════════════════════
    //  Alignment from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_JustifyCenter_AlignCenter()
    {
        var styles = new StyleSheet
        {
            [".centered"] = new S
            {
                FlexDirection = FlexDirection.Row,
                Width = StyleValue.Px(400),
                Height = StyleValue.Px(400),
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            },
            [".box"] = new S
            {
                Width = StyleValue.Px(100),
                Height = StyleValue.Px(100),
            }
        };

        var root = Node(styles, "centered");
        var box = Node(styles, "box");
        root.AddChild(box);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(150, box.ComputedX - root.ComputedX, "Centered X");
        AssertApprox(150, box.ComputedY - root.ComputedY, "Centered Y");
    }

    [Fact]
    public void Stylesheet_SpaceBetween()
    {
        var styles = new StyleSheet
        {
            [".toolbar"] = new S
            {
                FlexDirection = FlexDirection.Row,
                Width = StyleValue.Px(600),
                Height = StyleValue.Px(48),
                JustifyContent = JustifyContent.SpaceBetween,
                AlignItems = AlignItems.Center,
            },
            [".icon"] = new S
            {
                Width = StyleValue.Px(32),
                Height = StyleValue.Px(32),
            }
        };

        var root = Node(styles, "toolbar");
        root.AddChild(Node(styles, "icon"));
        root.AddChild(Node(styles, "icon"));
        root.AddChild(Node(styles, "icon"));

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        AssertApprox(0, root.Children[0].ComputedX - rootX, "First at start");
        AssertApprox(568, root.Children[2].ComputedX - rootX, "Last at end (600-32)");
    }

    [Fact]
    public void Stylesheet_AlignSelf_Override()
    {
        var styles = new StyleSheet
        {
            [".row"] = new S
            {
                FlexDirection = FlexDirection.Row,
                Width = StyleValue.Px(400),
                Height = StyleValue.Px(200),
                AlignItems = AlignItems.FlexStart,
            },
            [".normal"] = new S
            {
                Width = StyleValue.Px(50),
                Height = StyleValue.Px(50),
            },
            [".self-center"] = new S
            {
                Width = StyleValue.Px(50),
                Height = StyleValue.Px(50),
                AlignSelf = AlignSelf.Center,
            }
        };

        var root = Node(styles, "row");
        root.AddChild(Node(styles, "normal"));
        root.AddChild(Node(styles, "self-center"));

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        float rootY = root.ComputedY;
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Normal at top");
        AssertApprox(75, root.Children[1].ComputedY - rootY, "Self-center at (200-50)/2");
    }

    // ══════════════════════════════════════════════
    //  Flex wrap from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_FlexWrap_AutoHeight()
    {
        var styles = new StyleSheet
        {
            [".grid"] = new S
            {
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
                Width = StyleValue.Px(200),
                Gap = 10,
            },
            [".cell"] = new S
            {
                Width = StyleValue.Px(90),
                Height = StyleValue.Px(40),
            }
        };

        var root = Node(styles, "grid");
        for (int i = 0; i < 4; i++)
            root.AddChild(Node(styles, "cell"));

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        // 2 per row (90+10+90=190 < 200), 2 rows of 40 + 1 gap of 10 = 90
        AssertApprox(90, root.ComputedHeight, "Grid auto-height = 2 rows + gap");
    }

    [Fact]
    public void Stylesheet_FlexWrap_ChildPositions()
    {
        var styles = new StyleSheet
        {
            [".wrap-row"] = new S
            {
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
                Width = StyleValue.Px(250),
                Height = StyleValue.Px(300),
                Gap = 10,
            },
            [".card"] = new S
            {
                Width = StyleValue.Px(110),
                Height = StyleValue.Px(60),
                FlexShrink = 0,
            }
        };

        var root = Node(styles, "wrap-row");
        for (int i = 0; i < 4; i++)
            root.AddChild(Node(styles, "card"));

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        float rootY = root.ComputedY;
        // Row 1: 110+10+110 = 230 < 250
        AssertApprox(0, root.Children[0].ComputedX - rootX, "Card0 X");
        AssertApprox(120, root.Children[1].ComputedX - rootX, "Card1 X");
        AssertApprox(0, root.Children[0].ComputedY - rootY, "Card0 Y");
        // Row 2
        AssertApprox(0, root.Children[2].ComputedX - rootX, "Card2 X");
        AssertApprox(70, root.Children[2].ComputedY - rootY, "Card2 Y (60+10)");
    }

    // ══════════════════════════════════════════════
    //  Absolute positioning from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_AbsolutePosition()
    {
        var styles = new StyleSheet
        {
            [".viewport"] = new S
            {
                Width = StyleValue.Px(800),
                Height = StyleValue.Px(600),
            },
            [".overlay"] = new S
            {
                Position = PositionType.Absolute,
                Inset = new EdgeValues(0),
            }
        };

        var root = Node(styles, "viewport");
        var overlay = Node(styles, "overlay");
        root.AddChild(overlay);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(800, overlay.ComputedWidth, "Overlay fills width");
        AssertApprox(600, overlay.ComputedHeight, "Overlay fills height");
    }

    [Fact]
    public void Stylesheet_AbsolutePosition_WithInset()
    {
        var styles = new StyleSheet
        {
            [".parent"] = new S
            {
                Width = StyleValue.Px(400),
                Height = StyleValue.Px(300),
            },
            [".tooltip"] = new S
            {
                Position = PositionType.Absolute,
                Inset = new EdgeValues(10, float.NaN, float.NaN, 20),
                Width = StyleValue.Px(150),
                Height = StyleValue.Px(80),
            }
        };

        var root = Node(styles, "parent");
        var tooltip = Node(styles, "tooltip");
        root.AddChild(tooltip);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(20, tooltip.ComputedX - root.ComputedX, "Tooltip left=20");
        AssertApprox(10, tooltip.ComputedY - root.ComputedY, "Tooltip top=10");
        AssertApprox(150, tooltip.ComputedWidth, "Tooltip width");
        AssertApprox(80, tooltip.ComputedHeight, "Tooltip height");
    }

    // ══════════════════════════════════════════════
    //  Multiple classes resolved together
    // ══════════════════════════════════════════════

    [Fact]
    public void MultipleClasses_MergedLayout()
    {
        var styles = new StyleSheet
        {
            [".base"] = new S
            {
                Width = StyleValue.Px(200),
                Height = StyleValue.Px(100),
                Padding = new EdgeValues(10),
            },
            [".large"] = new S
            {
                Width = StyleValue.Px(400),
                Height = StyleValue.Px(200),
            }
        };

        // "base large" - large overrides base's width/height, keeps base's padding
        var root = Node(styles, "base large");
        root.AddChild(new LayoutNode { Height = 50 });

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(400, root.ComputedWidth, "Large width wins");
        AssertApprox(200, root.ComputedHeight, "Large height wins");
        AssertApprox(10, root.Children[0].ComputedX - root.ComputedX, "Base padding kept");
        AssertApprox(10, root.Children[0].ComputedY - root.ComputedY, "Base padding kept");
    }

    // ══════════════════════════════════════════════
    //  Inline override on top of stylesheet class
    // ══════════════════════════════════════════════

    [Fact]
    public void InlineOverride_TakesPrecedence()
    {
        var styles = new StyleSheet
        {
            [".card"] = new S
            {
                Width = StyleValue.Px(200),
                Height = StyleValue.Px(150),
                Padding = new EdgeValues(16),
            }
        };

        // Inline override changes width
        var root = Node(styles, "card", new S { Width = StyleValue.Px(300) });
        root.AddChild(new LayoutNode { Height = 50 });

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(300, root.ComputedWidth, "Inline width overrides class");
        AssertApprox(150, root.ComputedHeight, "Class height preserved");
        AssertApprox(16, root.Children[0].ComputedX - root.ComputedX, "Class padding preserved");
    }

    // ══════════════════════════════════════════════
    //  StyleResolver + LayoutBridge pipeline
    // ══════════════════════════════════════════════

    [Fact]
    public void ResolvedStyle_DefaultFlexDirection_IsColumn()
    {
        var resolved = StyleResolver.Resolve(new S(), null, false, false, false);
        var node = Node(resolved);

        node.AddChild(new LayoutNode { Width = 100, Height = 30 });
        node.AddChild(new LayoutNode { Width = 100, Height = 40 });

        ReactUI.Layout.YogaLayout.Calculate(node, 800, 600);

        float nodeY = node.ComputedY;
        // Default is column → children stacked vertically
        AssertApprox(0, node.Children[0].ComputedY - nodeY, "Child0 at top");
        AssertApprox(30, node.Children[1].ComputedY - nodeY, "Child1 below child0");
    }

    [Fact]
    public void ResolvedStyle_WithParentInheritance_DoesNotAffectLayout()
    {
        // Text properties are inherited but shouldn't affect layout
        var parent = StyleResolver.Resolve(
            new S { FontSize = 24, Color = new UIColor(255, 0, 0, 255) },
            null, false, false, false);

        var child = StyleResolver.Resolve(new S
        {
            Width = StyleValue.Px(100),
            Height = StyleValue.Px(50),
        }, parent, false, false, false);

        var root = new LayoutNode { Width = 400, Height = 300 };
        var childNode = Node(child);
        root.AddChild(childNode);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        // Text inheritance doesn't break layout
        AssertApprox(100, childNode.ComputedWidth, "Width unaffected by text inheritance");
        AssertApprox(50, childNode.ComputedHeight, "Height unaffected by text inheritance");
    }

    // ══════════════════════════════════════════════
    //  Overflow scroll from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_OverflowScroll_ChildrenNotConstrained()
    {
        var styles = new StyleSheet
        {
            [".scroll-area"] = new S
            {
                FlexDirection = FlexDirection.Column,
                Width = StyleValue.Px(300),
                Height = StyleValue.Px(200),
                Overflow = Overflow.Scroll,
            },
            [".item"] = new S
            {
                Height = StyleValue.Px(60),
            }
        };

        var root = Node(styles, "scroll-area");
        for (int i = 0; i < 5; i++)
            root.AddChild(Node(styles, "item"));

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(200, root.ComputedHeight, "Container stays at 200");
        // Children not constrained - all get 60px
        for (int i = 0; i < 5; i++)
            AssertApprox(60, root.Children[i].ComputedHeight, $"Child{i} height");
        // Last child positioned beyond container
        AssertApprox(240, root.Children[4].ComputedY - root.ComputedY, "Last child at 4*60=240");
    }

    // ══════════════════════════════════════════════
    //  Aspect ratio from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_AspectRatio()
    {
        var styles = new StyleSheet
        {
            [".widescreen"] = new S
            {
                Width = StyleValue.Px(320),
                AspectRatio = 16f / 9f,
            }
        };

        var root = new LayoutNode { Width = 800, Height = 600 };
        var child = Node(styles, "widescreen");
        child.AlignSelf = ReactUI.Layout.AlignSelf.FlexStart;
        root.AddChild(child);

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(320, child.ComputedWidth, "Width");
        AssertApprox(180, child.ComputedHeight, "Height = 320 / (16/9)");
    }

    // ══════════════════════════════════════════════
    //  Min/Max constraints from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_MinMaxWidth()
    {
        var styles = new StyleSheet
        {
            [".constrained"] = new S
            {
                Width = StyleValue.Px(800),
                MaxWidth = StyleValue.Px(300),
                MinHeight = StyleValue.Px(50),
            }
        };

        var root = Node(styles, "constrained");
        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        AssertApprox(300, root.ComputedWidth, "Clamped by max-width");
        Assert.True(root.ComputedHeight >= 50 - Tolerance, "Min-height enforced");
    }

    // ══════════════════════════════════════════════
    //  Real-world: settings panel via stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void SettingsPanel_FullStylesheetPipeline()
    {
        var styles = new StyleSheet
        {
            [".panel"] = new S
            {
                FlexDirection = FlexDirection.Column,
                Width = StyleValue.Px(500),
                Padding = new EdgeValues(24),
                Gap = 16,
            },
            [".section"] = new S
            {
                FlexDirection = FlexDirection.Column,
                Gap = 8,
            },
            [".section-title"] = new S
            {
                Height = StyleValue.Px(20),
            },
            [".setting-row"] = new S
            {
                FlexDirection = FlexDirection.Row,
                Height = StyleValue.Px(36),
                JustifyContent = JustifyContent.SpaceBetween,
                AlignItems = AlignItems.Center,
            },
            [".label"] = new S { FlexGrow = 1 },
            [".toggle"] = new S
            {
                Width = StyleValue.Px(48),
                Height = StyleValue.Px(24),
            },
        };

        var panel = Node(styles, "panel");

        // Section with title + 2 rows
        var section = Node(styles, "section");
        section.AddChild(Node(styles, "section-title"));
        for (int i = 0; i < 2; i++)
        {
            var row = Node(styles, "setting-row");
            row.AddChild(Node(styles, "label"));
            row.AddChild(Node(styles, "toggle"));
            section.AddChild(row);
        }
        panel.AddChild(section);

        CalculateUndefined(panel);

        float contentW = 500 - 48; // 452
        // Section: title(20) + gap(8) + row(36) + gap(8) + row(36) = 108
        // Panel: pad(24) + section(108) + pad(24) = 156
        AssertApprox(156, panel.ComputedHeight, "Panel auto-height");
        AssertApprox(contentW, section.ComputedWidth, "Section stretches");

        // Toggle in first row: at far right
        var firstRow = section.Children[1];
        var toggle = firstRow.Children[1];
        AssertApprox(48, toggle.ComputedWidth, "Toggle width");
    }

    // ══════════════════════════════════════════════
    //  Real-world: navbar via stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Navbar_FullStylesheetPipeline()
    {
        var styles = new StyleSheet
        {
            [".navbar"] = new S
            {
                FlexDirection = FlexDirection.Row,
                Width = StyleValue.Px(800),
                Height = StyleValue.Px(48),
                Padding = new EdgeValues(0, 16),
                Gap = 8,
                AlignItems = AlignItems.Center,
            },
            [".logo"] = new S
            {
                Width = StyleValue.Px(32),
                Height = StyleValue.Px(32),
            },
            [".spacer"] = new S { FlexGrow = 1 },
            [".nav-btn"] = new S
            {
                Width = StyleValue.Px(80),
                Height = StyleValue.Px(32),
            },
        };

        var navbar = Node(styles, "navbar");
        var logo = Node(styles, "logo");
        var spacer = Node(styles, "spacer");
        var btn1 = Node(styles, "nav-btn");
        var btn2 = Node(styles, "nav-btn");
        navbar.AddChild(logo);
        navbar.AddChild(spacer);
        navbar.AddChild(btn1);
        navbar.AddChild(btn2);

        ReactUI.Layout.YogaLayout.Calculate(navbar, 800, 600);

        // Logo at left padding
        AssertApprox(16, logo.ComputedX - navbar.ComputedX, "Logo at left pad");
        // Logo vertically centered: (48-32)/2 = 8
        AssertApprox(8, logo.ComputedY - navbar.ComputedY, "Logo centered Y");
        // Inner = 800-32 = 768. Items: 32+spacer+80+80 + 3 gaps(24) → spacer = 768-192-24 = 552
        AssertApprox(552, spacer.ComputedWidth, "Spacer fills remaining");
    }

    // ══════════════════════════════════════════════
    //  Real-world: card grid with wrap
    // ══════════════════════════════════════════════

    [Fact]
    public void CardGrid_StylesheetWrap()
    {
        var styles = new StyleSheet
        {
            [".grid"] = new S
            {
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
                Width = StyleValue.Px(600),
                Gap = 20,
            },
            [".card"] = new S
            {
                Width = StyleValue.Px(170),
                Height = StyleValue.Px(120),
                FlexShrink = 0,
            }
        };

        var grid = Node(styles, "grid");
        for (int i = 0; i < 6; i++)
            grid.AddChild(Node(styles, "card"));

        ReactUI.Layout.YogaLayout.Calculate(grid, 800, 600);

        // 3 per row: 170*3 + 20*2 = 550 < 600
        // 2 rows of 120 + 1 gap of 20 = 260
        AssertApprox(260, grid.ComputedHeight, "Grid auto-height");

        float gridX = grid.ComputedX;
        float gridY = grid.ComputedY;
        // Row 1
        AssertApprox(0, grid.Children[0].ComputedX - gridX, "Card0 X");
        AssertApprox(190, grid.Children[1].ComputedX - gridX, "Card1 X");
        AssertApprox(380, grid.Children[2].ComputedX - gridX, "Card2 X");
        // Row 2
        AssertApprox(140, grid.Children[3].ComputedY - gridY, "Card3 Y (120+20)");
    }

    // ══════════════════════════════════════════════
    //  Real-world: modal dialog
    // ══════════════════════════════════════════════

    [Fact]
    public void ModalDialog_CenteredOverlay()
    {
        var styles = new StyleSheet
        {
            [".viewport"] = new S
            {
                Width = StyleValue.Px(1920),
                Height = StyleValue.Px(1080),
            },
            [".overlay"] = new S
            {
                Position = PositionType.Absolute,
                Inset = new EdgeValues(0),
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            },
            [".dialog"] = new S
            {
                Width = StyleValue.Px(480),
                Height = StyleValue.Px(320),
                Padding = new EdgeValues(24),
                FlexDirection = FlexDirection.Column,
                Gap = 16,
            },
            [".dialog-title"] = new S { Height = StyleValue.Px(28) },
            [".dialog-body"] = new S { FlexGrow = 1 },
            [".dialog-footer"] = new S
            {
                Height = StyleValue.Px(40),
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.FlexEnd,
                Gap = 8,
            },
            [".btn"] = new S
            {
                Width = StyleValue.Px(80),
                Height = StyleValue.Px(32),
            },
        };

        var viewport = Node(styles, "viewport");
        var overlay = Node(styles, "overlay");
        var dialog = Node(styles, "dialog");
        var title = Node(styles, "dialog-title");
        var body = Node(styles, "dialog-body");
        var footer = Node(styles, "dialog-footer");
        footer.AddChild(Node(styles, "btn"));
        footer.AddChild(Node(styles, "btn"));

        dialog.AddChild(title);
        dialog.AddChild(body);
        dialog.AddChild(footer);
        overlay.AddChild(dialog);
        viewport.AddChild(overlay);

        ReactUI.Layout.YogaLayout.Calculate(viewport, 1920, 1080);

        // Overlay fills viewport
        AssertApprox(1920, overlay.ComputedWidth, "Overlay fills");
        AssertApprox(1080, overlay.ComputedHeight, "Overlay fills");

        // Dialog centered: (1920-480)/2, (1080-320)/2
        AssertApprox(720, dialog.ComputedX - viewport.ComputedX, "Dialog centered X");
        AssertApprox(380, dialog.ComputedY - viewport.ComputedY, "Dialog centered Y");

        // Dialog body fills: 320 - 48(pad) - 28(title) - 16(gap) - 16(gap) - 40(footer) = 172
        AssertApprox(172, body.ComputedHeight, "Body fills remaining");
    }

    // ══════════════════════════════════════════════
    //  ReactUI.Layout.LayoutBridge.ToRect
    // ══════════════════════════════════════════════

    [Fact]
    public void ToRect_ReturnsCorrectValues()
    {
        var styles = new StyleSheet
        {
            [".box"] = new S
            {
                Width = StyleValue.Px(100),
                Height = StyleValue.Px(50),
            }
        };

        var root = new LayoutNode { Width = 400, Height = 300 };
        var child = Node(styles, "box");
        root.AddChild(child);

        ReactUI.Layout.YogaLayout.Calculate(root, 400, 300);

        var (x, y, w, h) = ReactUI.Layout.LayoutBridge.ToRect(child);
        AssertApprox(100, w, "Rect width");
        AssertApprox(50, h, "Rect height");
    }

    // ══════════════════════════════════════════════
    //  Reverse direction from stylesheet
    // ══════════════════════════════════════════════

    [Fact]
    public void Stylesheet_RowReverse()
    {
        var styles = new StyleSheet
        {
            [".rtl-row"] = new S
            {
                FlexDirection = FlexDirection.RowReverse,
                Width = StyleValue.Px(300),
                Height = StyleValue.Px(50),
            },
            [".item"] = new S
            {
                Width = StyleValue.Px(50),
            }
        };

        var root = Node(styles, "rtl-row");
        root.AddChild(Node(styles, "item"));
        root.AddChild(Node(styles, "item"));

        ReactUI.Layout.YogaLayout.Calculate(root, 800, 600);

        float rootX = root.ComputedX;
        AssertApprox(250, root.Children[0].ComputedX - rootX, "First at right");
        AssertApprox(200, root.Children[1].ComputedX - rootX, "Second next to first");
    }

    // ══════════════════════════════════════════════
    //  Global style registry
    // ══════════════════════════════════════════════

    [Fact]
    public void GlobalStyles_RegisterAndResolve()
    {
        GlobalStyles.Clear();
        GlobalStyles.Register(new StyleSheet
        {
            [".g-box"] = new S
            {
                Width = StyleValue.Px(250),
                Height = StyleValue.Px(125),
            }
        });

        var node = new LayoutNode();
        ReactUI.Layout.LayoutBridge.ApplyStyle(node, GlobalStyles.Resolve("g-box"));

        ReactUI.Layout.YogaLayout.Calculate(node, 800, 600);

        AssertApprox(250, node.ComputedWidth, "Global style width");
        AssertApprox(125, node.ComputedHeight, "Global style height");

        GlobalStyles.Clear();
    }
}
