using System.Collections.Generic;
using ReactUI.Jsx;
using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

public class StyleConverterTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    [Fact]
    public void ConvertsFontSize()
    {
        var props = new Dictionary<string, object?> { ["fontSize"] = 24.0 };
        var style = StyleConverter.Convert(props);
        AssertApprox(24, style.FontSize!.Value);
    }

    [Fact]
    public void ConvertsBackgroundColor()
    {
        var props = new Dictionary<string, object?> { ["background"] = "#ff0000" };
        var style = StyleConverter.Convert(props);
        AssertApprox(1f, style.Background!.Value.R);
        AssertApprox(0f, style.Background!.Value.G);
    }

    [Fact]
    public void ConvertsBackgroundGradient()
    {
        var props = new Dictionary<string, object?> { ["background"] = "linear-gradient(90deg, #000, #fff)" };
        var style = StyleConverter.Convert(props);
        Assert.NotNull(style.BackgroundGradient);
        Assert.Equal(GradientType.Linear, style.BackgroundGradient!.Value.Type);
    }

    [Fact]
    public void ConvertsPaddingFromNumber()
    {
        var props = new Dictionary<string, object?> { ["padding"] = 20.0 };
        var style = StyleConverter.Convert(props);
        AssertApprox(20, style.Padding!.Value.Top.Value);
        AssertApprox(20, style.Padding!.Value.Right.Value);
    }

    [Fact]
    public void ConvertsPaddingFromArray()
    {
        var props = new Dictionary<string, object?> { ["padding"] = new List<object?> { 8.0, 16.0 } };
        var style = StyleConverter.Convert(props);
        AssertApprox(8, style.Padding!.Value.Top.Value);
        AssertApprox(16, style.Padding!.Value.Right.Value);
        AssertApprox(8, style.Padding!.Value.Bottom.Value);
        AssertApprox(16, style.Padding!.Value.Left.Value);
    }

    [Fact]
    public void ConvertsPaddingFromFourArray()
    {
        var props = new Dictionary<string, object?> { ["padding"] = new List<object?> { 1.0, 2.0, 3.0, 4.0 } };
        var style = StyleConverter.Convert(props);
        AssertApprox(1, style.Padding!.Value.Top.Value);
        AssertApprox(2, style.Padding!.Value.Right.Value);
        AssertApprox(3, style.Padding!.Value.Bottom.Value);
        AssertApprox(4, style.Padding!.Value.Left.Value);
    }

    [Fact]
    public void ConvertsPaddingFromString()
    {
        var props = new Dictionary<string, object?> { ["padding"] = "8 16" };
        var style = StyleConverter.Convert(props);
        AssertApprox(8, style.Padding!.Value.Top.Value);
        AssertApprox(16, style.Padding!.Value.Right.Value);
    }

    [Fact]
    public void ConvertsFlexDirection_KebabCase()
    {
        var props = new Dictionary<string, object?> { ["flexDirection"] = "row-reverse" };
        var style = StyleConverter.Convert(props);
        Assert.Equal(FlexDirection.RowReverse, style.FlexDirection);
    }

    [Fact]
    public void ConvertsFlexDirection_CamelCase()
    {
        var props = new Dictionary<string, object?> { ["flexDirection"] = "row" };
        var style = StyleConverter.Convert(props);
        Assert.Equal(FlexDirection.Row, style.FlexDirection);
    }

    [Fact]
    public void ConvertsJustifyContent()
    {
        var props = new Dictionary<string, object?> { ["justifyContent"] = "space-between" };
        var style = StyleConverter.Convert(props);
        Assert.Equal(JustifyContent.SpaceBetween, style.JustifyContent);
    }

    [Fact]
    public void ConvertsPosition()
    {
        var props = new Dictionary<string, object?> { ["position"] = "absolute" };
        var style = StyleConverter.Convert(props);
        Assert.Equal(PositionType.Absolute, style.Position);
    }

    [Fact]
    public void ConvertsCursor()
    {
        var props = new Dictionary<string, object?> { ["cursor"] = "pointer" };
        var style = StyleConverter.Convert(props);
        Assert.Equal(CursorType.Pointer, style.Cursor);
    }

    [Fact]
    public void ConvertsOpacity()
    {
        var props = new Dictionary<string, object?> { ["opacity"] = 0.5 };
        var style = StyleConverter.Convert(props);
        AssertApprox(0.5f, style.Opacity!.Value);
    }

    [Fact]
    public void ConvertsBorderRadius_Number()
    {
        var props = new Dictionary<string, object?> { ["borderRadius"] = 12.0 };
        var style = StyleConverter.Convert(props);
        AssertApprox(12, style.BorderRadius!.Value);
    }

    [Fact]
    public void ConvertsBorderRadius_FourCorners()
    {
        var props = new Dictionary<string, object?> { ["borderRadius"] = new List<object?> { 1.0, 2.0, 3.0, 4.0 } };
        var style = StyleConverter.Convert(props);
        Assert.NotNull(style.BorderRadii);
        AssertApprox(1, style.BorderRadii!.Value.TopLeft);
        AssertApprox(2, style.BorderRadii!.Value.TopRight);
        AssertApprox(3, style.BorderRadii!.Value.BottomRight);
        AssertApprox(4, style.BorderRadii!.Value.BottomLeft);
    }

    [Fact]
    public void ConvertsWidthAsPercent()
    {
        var props = new Dictionary<string, object?> { ["width"] = "50%" };
        var style = StyleConverter.Convert(props);
        Assert.Equal(StyleUnit.Percent, style.Width!.Value.Unit);
        AssertApprox(50, style.Width!.Value.Value);
    }

    [Fact]
    public void ConvertsWidthAsNumber()
    {
        var props = new Dictionary<string, object?> { ["width"] = 200.0 };
        var style = StyleConverter.Convert(props);
        Assert.Equal(StyleUnit.Px, style.Width!.Value.Unit);
        AssertApprox(200, style.Width!.Value.Value);
    }

    [Fact]
    public void ConvertsTransition()
    {
        var props = new Dictionary<string, object?> { ["transition"] = "opacity 0.15s ease" };
        var style = StyleConverter.Convert(props);
        Assert.NotNull(style.Transitions);
        Assert.Single(style.Transitions!);
        Assert.Equal("opacity", style.Transitions[0].Property);
    }

    [Fact]
    public void ConvertsHoverPseudoState()
    {
        var hoverProps = new Dictionary<string, object?> { ["opacity"] = 0.8 };
        var props = new Dictionary<string, object?> { ["hover"] = hoverProps };
        var style = StyleConverter.Convert(props);
        Assert.NotNull(style.Hover);
        AssertApprox(0.8f, style.Hover!.Opacity!.Value);
    }

    [Fact]
    public void ConvertsBoxShadow()
    {
        var props = new Dictionary<string, object?> { ["boxShadow"] = "0 4px 12px rgba(0,0,0,0.3)" };
        var style = StyleConverter.Convert(props);
        Assert.NotNull(style.BoxShadow);
        AssertApprox(4, style.BoxShadow!.Value.OffsetY);
        AssertApprox(12, style.BoxShadow!.Value.Blur);
    }

    [Fact]
    public void MultipleProperties_AllConverted()
    {
        var props = new Dictionary<string, object?>
        {
            ["background"] = "#1a1a2e",
            ["padding"] = 20.0,
            ["borderRadius"] = 16.0,
            ["fontSize"] = 14.0,
            ["color"] = "#e0e0e0",
            ["flexDirection"] = "column",
            ["gap"] = 12.0,
        };
        var style = StyleConverter.Convert(props);

        Assert.NotNull(style.Background);
        Assert.NotNull(style.Padding);
        AssertApprox(16, style.BorderRadius!.Value);
        AssertApprox(14, style.FontSize!.Value);
        Assert.Equal(FlexDirection.Column, style.FlexDirection);
        AssertApprox(12, style.Gap!.Value);
    }

    [Fact]
    public void NullValues_Skipped()
    {
        var props = new Dictionary<string, object?> { ["fontSize"] = null, ["color"] = "#fff" };
        var style = StyleConverter.Convert(props);
        Assert.Null(style.FontSize);
        Assert.NotNull(style.Color);
    }

    [Fact]
    public void UnknownProperty_Ignored()
    {
        var props = new Dictionary<string, object?> { ["unknownThing"] = "value" };
        var style = StyleConverter.Convert(props);
        // Should not throw, just ignore
        Assert.Null(style.FontSize);
    }
}
