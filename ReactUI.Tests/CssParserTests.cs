using ReactUI.Jsx;
using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

public class CssParserTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    [Fact]
    public void ParsesSingleClass()
    {
        var sheet = CssParser.Parse(".panel { padding: 20; background: #1a1a2e; }");
        var style = sheet[".panel"];
        AssertApprox(20, style.Padding!.Value.Top.Value);
        Assert.NotNull(style.Background);
    }

    [Fact]
    public void ParsesMultipleClasses()
    {
        var css = @"
            .title { font-size: 22; font-weight: 700; color: #e0e0e0; }
            .body { font-size: 14; color: #a0a0a0; }
        ";
        var sheet = CssParser.Parse(css);

        AssertApprox(22, sheet[".title"].FontSize!.Value);
        Assert.Equal(700, sheet[".title"].FontWeight);
        AssertApprox(14, sheet[".body"].FontSize!.Value);
    }

    [Fact]
    public void ParsesHoverPseudoState()
    {
        var css = @"
            .btn { background: #7c3aed; opacity: 1; }
            .btn:hover { opacity: 0.8; }
        ";
        var sheet = CssParser.Parse(css);
        var style = sheet[".btn"];

        AssertApprox(1f, style.Opacity!.Value);
        Assert.NotNull(style.Hover);
        AssertApprox(0.8f, style.Hover!.Opacity!.Value);
    }

    [Fact]
    public void ParsesActivePseudoState()
    {
        var css = @"
            .btn { background: #7c3aed; }
            .btn:active { opacity: 0.6; }
        ";
        var sheet = CssParser.Parse(css);
        Assert.NotNull(sheet[".btn"].Active);
        AssertApprox(0.6f, sheet[".btn"].Active!.Opacity!.Value);
    }

    [Fact]
    public void ParsesFocusPseudoState()
    {
        var css = @"
            .input { border-color: #444; }
            .input:focus { border-color: #7c3aed; }
        ";
        var sheet = CssParser.Parse(css);
        Assert.NotNull(sheet[".input"].Focus);
    }

    [Fact]
    public void ParsesKebabCaseProperties()
    {
        var css = ".card { flex-direction: row; justify-content: space-between; align-items: center; border-radius: 12; }";
        var sheet = CssParser.Parse(css);
        var style = sheet[".card"];

        Assert.Equal(FlexDirection.Row, style.FlexDirection);
        Assert.Equal(JustifyContent.SpaceBetween, style.JustifyContent);
        Assert.Equal(AlignItems.Center, style.AlignItems);
        AssertApprox(12, style.BorderRadius!.Value);
    }

    [Fact]
    public void ParsesTransition()
    {
        var css = ".animated { transition: opacity 0.15s ease; }";
        var sheet = CssParser.Parse(css);
        var style = sheet[".animated"];

        Assert.NotNull(style.Transitions);
        Assert.Single(style.Transitions!);
        Assert.Equal("opacity", style.Transitions![0].Property);
    }

    [Fact]
    public void ParsesPaddingShorthand()
    {
        var css = ".spaced { padding: 8 16; }";
        var sheet = CssParser.Parse(css);
        var style = sheet[".spaced"];

        AssertApprox(8, style.Padding!.Value.Top.Value);
        AssertApprox(16, style.Padding!.Value.Right.Value);
    }

    [Fact]
    public void ParsesBoxShadow()
    {
        var css = ".elevated { box-shadow: 0 4px 12px rgba(0,0,0,0.3); }";
        var sheet = CssParser.Parse(css);
        Assert.NotNull(sheet[".elevated"].BoxShadow);
        AssertApprox(4, sheet[".elevated"].BoxShadow!.Value.OffsetY);
    }

    [Fact]
    public void IgnoresComments()
    {
        var css = @"
            /* This is a comment */
            .panel { padding: 20; }
            // Line comment
            .title { font-size: 22; }
        ";
        var sheet = CssParser.Parse(css);
        Assert.True(sheet.Contains(".panel"));
        Assert.True(sheet.Contains(".title"));
    }

    [Fact]
    public void EmptyInput_ReturnsEmptySheet()
    {
        var sheet = CssParser.Parse("");
        Assert.False(sheet.Contains(".anything"));
    }

    [Fact]
    public void ParseInlineStyle_ReturnsStyle()
    {
        var style = CssParser.ParseInlineStyle("padding: 20; background: #1a1a2e; border-radius: 16");
        AssertApprox(20, style.Padding!.Value.Top.Value);
        AssertApprox(16, style.BorderRadius!.Value);
    }

    [Fact]
    public void ParsesPositionAbsolute()
    {
        var css = ".overlay { position: absolute; }";
        var sheet = CssParser.Parse(css);
        Assert.Equal(PositionType.Absolute, sheet[".overlay"].Position);
    }

    [Fact]
    public void ParsesCursorPointer()
    {
        var css = ".clickable { cursor: pointer; }";
        var sheet = CssParser.Parse(css);
        Assert.Equal(CursorType.Pointer, sheet[".clickable"].Cursor);
    }

    [Fact]
    public void RealWorldExample_FullTheme()
    {
        var css = @"
            .panel {
                padding: 20;
                background: #1a1a2eE8;
                border-radius: 16;
                border-width: 1;
                border-color: #ffffff15;
                box-shadow: 0 4px 24px rgba(0,0,0,0.3);
                gap: 12;
            }

            .title {
                font-size: 22;
                font-weight: 700;
                color: #e0e0e0;
            }

            .btn {
                padding: 6 12;
                border-radius: 6;
                font-size: 13;
                font-weight: 600;
                cursor: pointer;
                opacity: 0.9;
                transition: opacity 0.1s ease;
            }

            .btn:hover {
                opacity: 1;
            }

            .btn-primary {
                background: #7c3aed;
                color: #ffffff;
            }

            .btn-danger {
                background: #ef4444;
                color: #ffffff;
            }

            .input {
                padding: 8 12;
                background: #1a1a2e;
                border-radius: 8;
                border-width: 1;
                border-color: #444444;
                color: #e0e0e0;
                font-size: 14;
                transition: border-color 0.15s ease;
            }

            .input:focus {
                border-color: #7c3aed;
            }
        ";

        var sheet = CssParser.Parse(css);

        // Panel
        AssertApprox(20, sheet[".panel"].Padding!.Value.Top.Value);
        AssertApprox(16, sheet[".panel"].BorderRadius!.Value);
        AssertApprox(12, sheet[".panel"].Gap!.Value);

        // Title
        AssertApprox(22, sheet[".title"].FontSize!.Value);
        Assert.Equal(700, sheet[".title"].FontWeight);

        // Button with hover
        Assert.NotNull(sheet[".btn"].Transitions);
        Assert.Equal(CursorType.Pointer, sheet[".btn"].Cursor);
        Assert.NotNull(sheet[".btn"].Hover);
        AssertApprox(1f, sheet[".btn"].Hover!.Opacity!.Value);

        // Button variants
        Assert.NotNull(sheet[".btn-primary"].Background);
        Assert.NotNull(sheet[".btn-danger"].Background);

        // Input with focus
        Assert.NotNull(sheet[".input"].Transitions);
        Assert.NotNull(sheet[".input"].Focus);
    }
}
