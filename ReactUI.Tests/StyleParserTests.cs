using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

public class StyleParserTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    // ========================================================================
    // UIColor - FromHex
    // ========================================================================

    [Fact]
    public void FromHex_ShorthandWhite_ParsesCorrectly()
    {
        var c = UIColor.FromHex("#fff");
        AssertApprox(1f, c.R);
        AssertApprox(1f, c.G);
        AssertApprox(1f, c.B);
        AssertApprox(1f, c.A);
    }

    [Fact]
    public void FromHex_SixDigit_ParsesCorrectly()
    {
        var c = UIColor.FromHex("#1a1a2e");
        AssertApprox(0.102f, c.R);
        AssertApprox(0.102f, c.G);
        AssertApprox(0.180f, c.B);
        AssertApprox(1f, c.A);
    }

    [Fact]
    public void FromHex_EightDigitWithAlpha_ParsesCorrectly()
    {
        var c = UIColor.FromHex("#ff000080");
        AssertApprox(1f, c.R);
        AssertApprox(0f, c.G);
        AssertApprox(0f, c.B);
        AssertApprox(0.502f, c.A);
    }

    [Fact]
    public void FromHex_FourDigitShorthandWithAlpha_ParsesCorrectly()
    {
        var c = UIColor.FromHex("#f00f");
        AssertApprox(1f, c.R);
        AssertApprox(0f, c.G);
        AssertApprox(0f, c.B);
        AssertApprox(1f, c.A);
    }

    [Fact]
    public void FromHex_NullOrEmpty_ReturnsTransparent()
    {
        var c = UIColor.FromHex("");
        AssertApprox(0f, c.R);
        AssertApprox(0f, c.G);
        AssertApprox(0f, c.B);
        AssertApprox(0f, c.A);
    }

    // ========================================================================
    // UIColor - FromRgba
    // ========================================================================

    [Fact]
    public void FromRgba_WithAlpha_ParsesCorrectly()
    {
        var c = UIColor.FromRgba("rgba(255, 128, 0, 0.5)");
        AssertApprox(1f, c.R);
        AssertApprox(0.502f, c.G);
        AssertApprox(0f, c.B);
        AssertApprox(0.5f, c.A);
    }

    [Fact]
    public void FromRgba_WithoutAlpha_DefaultsToOne()
    {
        var c = UIColor.FromRgba("rgb(0, 255, 0)");
        AssertApprox(0f, c.R);
        AssertApprox(1f, c.G);
        AssertApprox(0f, c.B);
        AssertApprox(1f, c.A);
    }

    // ========================================================================
    // UIColor - Named colors via StyleParser.ParseColor
    // ========================================================================

    [Fact]
    public void ParseColor_Transparent_ReturnsTransparent()
    {
        var c = StyleParser.ParseColor("transparent");
        AssertApprox(0f, c.A);
    }

    [Fact]
    public void ParseColor_White_ReturnsWhite()
    {
        var c = StyleParser.ParseColor("white");
        AssertApprox(1f, c.R);
        AssertApprox(1f, c.G);
        AssertApprox(1f, c.B);
        AssertApprox(1f, c.A);
    }

    [Fact]
    public void ParseColor_Black_ReturnsBlack()
    {
        var c = StyleParser.ParseColor("black");
        AssertApprox(0f, c.R);
        AssertApprox(0f, c.G);
        AssertApprox(0f, c.B);
        AssertApprox(1f, c.A);
    }

    [Fact]
    public void ParseColor_HexString_DelegatesToFromHex()
    {
        var c = StyleParser.ParseColor("#ff0000");
        AssertApprox(1f, c.R);
        AssertApprox(0f, c.G);
        AssertApprox(0f, c.B);
    }

    [Fact]
    public void ParseColor_NullOrWhitespace_ReturnsTransparent()
    {
        var c = StyleParser.ParseColor("  ");
        AssertApprox(0f, c.A);
    }

    // ========================================================================
    // UIColor.Lerp
    // ========================================================================

    [Fact]
    public void Lerp_AtZero_ReturnsStart()
    {
        var result = UIColor.Lerp(UIColor.Black, UIColor.White, 0f);
        AssertApprox(0f, result.R);
        AssertApprox(0f, result.G);
        AssertApprox(0f, result.B);
        AssertApprox(1f, result.A);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsEnd()
    {
        var result = UIColor.Lerp(UIColor.Black, UIColor.White, 1f);
        AssertApprox(1f, result.R);
        AssertApprox(1f, result.G);
        AssertApprox(1f, result.B);
        AssertApprox(1f, result.A);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var result = UIColor.Lerp(UIColor.Black, UIColor.White, 0.5f);
        AssertApprox(0.5f, result.R);
        AssertApprox(0.5f, result.G);
        AssertApprox(0.5f, result.B);
        AssertApprox(1f, result.A);
    }

    // ========================================================================
    // StyleValue parsing
    // ========================================================================

    [Fact]
    public void ParseStyleValue_Auto_ReturnsAuto()
    {
        var v = StyleParser.ParseStyleValue("auto");
        Assert.Equal(StyleUnit.Auto, v.Unit);
    }

    [Fact]
    public void ParseStyleValue_Percent_ParsesCorrectly()
    {
        var v = StyleParser.ParseStyleValue("50%");
        Assert.Equal(StyleUnit.Percent, v.Unit);
        AssertApprox(50f, v.Value);
    }

    [Fact]
    public void ParseStyleValue_Px_ParsesCorrectly()
    {
        var v = StyleParser.ParseStyleValue("16px");
        Assert.Equal(StyleUnit.Px, v.Unit);
        AssertApprox(16f, v.Value);
    }

    [Fact]
    public void ParseStyleValue_PlainNumber_DefaultsToPx()
    {
        var v = StyleParser.ParseStyleValue("16");
        Assert.Equal(StyleUnit.Px, v.Unit);
        AssertApprox(16f, v.Value);
    }

    [Fact]
    public void ParseStyleValue_Empty_ReturnsUndefined()
    {
        var v = StyleParser.ParseStyleValue("");
        Assert.Equal(StyleUnit.Undefined, v.Unit);
    }

    [Fact]
    public void StyleValue_ImplicitFromFloat_ReturnsPx()
    {
        StyleValue v = 42f;
        Assert.Equal(StyleUnit.Px, v.Unit);
        AssertApprox(42f, v.Value);
    }

    [Fact]
    public void StyleValue_ImplicitFromString_Auto()
    {
        StyleValue v = "auto";
        Assert.Equal(StyleUnit.Auto, v.Unit);
    }

    [Fact]
    public void StyleValue_IsDefined_TrueForPx()
    {
        StyleValue v = StyleValue.Px(10);
        Assert.True(v.IsDefined);
    }

    [Fact]
    public void StyleValue_IsDefined_FalseForUndefined()
    {
        Assert.False(StyleValue.Undefined.IsDefined);
    }

    // ========================================================================
    // EdgeValues
    // ========================================================================

    [Fact]
    public void EdgeValues_SingleValue_AllSidesEqual()
    {
        var e = new EdgeValues(10);
        AssertApprox(10f, e.Top);
        AssertApprox(10f, e.Right);
        AssertApprox(10f, e.Bottom);
        AssertApprox(10f, e.Left);
    }

    [Fact]
    public void EdgeValues_TwoValues_VerticalAndHorizontal()
    {
        var e = new EdgeValues(8, 16);
        AssertApprox(8f, e.Top);
        AssertApprox(16f, e.Right);
        AssertApprox(8f, e.Bottom);
        AssertApprox(16f, e.Left);
    }

    [Fact]
    public void EdgeValues_FourValues_IndividualSides()
    {
        var e = new EdgeValues(1, 2, 3, 4);
        AssertApprox(1f, e.Top);
        AssertApprox(2f, e.Right);
        AssertApprox(3f, e.Bottom);
        AssertApprox(4f, e.Left);
    }

    [Fact]
    public void EdgeValues_Lerp_AtHalf()
    {
        var a = new EdgeValues(0, 0, 0, 0);
        var b = new EdgeValues(10, 20, 30, 40);
        var result = EdgeValues.Lerp(a, b, 0.5f);
        AssertApprox(5f, result.Top);
        AssertApprox(10f, result.Right);
        AssertApprox(15f, result.Bottom);
        AssertApprox(20f, result.Left);
    }

    [Fact]
    public void EdgeValues_ImplicitFromFloat()
    {
        EdgeValues e = 20f;
        AssertApprox(20f, e.Top);
        AssertApprox(20f, e.Right);
        AssertApprox(20f, e.Bottom);
        AssertApprox(20f, e.Left);
    }

    [Fact]
    public void EdgeValues_ImplicitFromTuple()
    {
        EdgeValues e = (8f, 16f);
        AssertApprox(8f, e.Top);
        AssertApprox(16f, e.Right);
        AssertApprox(8f, e.Bottom);
        AssertApprox(16f, e.Left);
    }

    [Fact]
    public void EdgeValues_ImplicitFromFourTuple()
    {
        EdgeValues e = (1f, 2f, 3f, 4f);
        AssertApprox(1f, e.Top);
        AssertApprox(2f, e.Right);
        AssertApprox(3f, e.Bottom);
        AssertApprox(4f, e.Left);
    }

    // ========================================================================
    // BoxShadow parsing
    // ========================================================================

    [Fact]
    public void ParseBoxShadow_WithRgbaColor_ParsesCorrectly()
    {
        var bs = StyleParser.ParseBoxShadow("0 4px 24px rgba(0,0,0,0.3)");
        AssertApprox(0f, bs.OffsetX);
        AssertApprox(4f, bs.OffsetY);
        AssertApprox(24f, bs.Blur);
        AssertApprox(0.3f, bs.Color.A);
        Assert.False(bs.Inset);
    }

    [Fact]
    public void ParseBoxShadow_Inset_ParsesCorrectly()
    {
        var bs = StyleParser.ParseBoxShadow("inset 2px 2px 4px #000");
        Assert.True(bs.Inset);
        AssertApprox(2f, bs.OffsetX);
        AssertApprox(2f, bs.OffsetY);
        AssertApprox(4f, bs.Blur);
        AssertApprox(0f, bs.Color.R);
        AssertApprox(0f, bs.Color.G);
        AssertApprox(0f, bs.Color.B);
    }

    [Fact]
    public void BoxShadow_Lerp_AtZero_ReturnsStart()
    {
        var a = new BoxShadow { OffsetX = 0, OffsetY = 0, Blur = 0, Color = UIColor.Black };
        var b = new BoxShadow { OffsetX = 10, OffsetY = 20, Blur = 30, Color = UIColor.White };
        var result = BoxShadow.Lerp(a, b, 0f);
        AssertApprox(0f, result.OffsetX);
        AssertApprox(0f, result.OffsetY);
        AssertApprox(0f, result.Blur);
    }

    [Fact]
    public void BoxShadow_Lerp_AtOne_ReturnsEnd()
    {
        var a = new BoxShadow { OffsetX = 0, OffsetY = 0, Blur = 0, Color = UIColor.Black };
        var b = new BoxShadow { OffsetX = 10, OffsetY = 20, Blur = 30, Color = UIColor.White };
        var result = BoxShadow.Lerp(a, b, 1f);
        AssertApprox(10f, result.OffsetX);
        AssertApprox(20f, result.OffsetY);
        AssertApprox(30f, result.Blur);
    }

    [Fact]
    public void ParseBoxShadow_Empty_ReturnsDefault()
    {
        var bs = StyleParser.ParseBoxShadow("");
        AssertApprox(0f, bs.OffsetX);
        AssertApprox(0f, bs.OffsetY);
        AssertApprox(0f, bs.Blur);
    }

    // ========================================================================
    // Gradient parsing
    // ========================================================================

    [Fact]
    public void ParseGradient_Linear_ParsesAngleAndColors()
    {
        var g = StyleParser.ParseGradient("linear-gradient(135deg, #667eea, #764ba2)");
        Assert.Equal(GradientType.Linear, g.Type);
        AssertApprox(135f, g.Angle);
    }

    [Fact]
    public void ParseGradient_Radial_ParsesType()
    {
        var g = StyleParser.ParseGradient("radial-gradient(#fff, #000)");
        Assert.Equal(GradientType.Radial, g.Type);
    }

    [Fact]
    public void ParseGradient_Radial_ParsesColors()
    {
        var g = StyleParser.ParseGradient("radial-gradient(#fff, #000)");
        AssertApprox(1f, g.ColorA.R);
        AssertApprox(1f, g.ColorA.G);
        AssertApprox(1f, g.ColorA.B);
        AssertApprox(0f, g.ColorB.R);
        AssertApprox(0f, g.ColorB.G);
        AssertApprox(0f, g.ColorB.B);
    }

    [Fact]
    public void ParseGradient_Empty_ReturnsNone()
    {
        var g = StyleParser.ParseGradient("");
        Assert.Equal(GradientType.None, g.Type);
    }

    [Fact]
    public void ParseGradient_LinearWithDirection_ParsesAngle()
    {
        var g = StyleParser.ParseGradient("linear-gradient(to right, #fff, #000)");
        Assert.Equal(GradientType.Linear, g.Type);
        AssertApprox(90f, g.Angle);
    }

    // ========================================================================
    // Transition parsing
    // ========================================================================

    [Fact]
    public void TransitionParse_Single_ParsesCorrectly()
    {
        var transitions = Transition.Parse("background 0.15s ease");
        Assert.Single(transitions);
        Assert.Equal("background", transitions[0].Property);
        AssertApprox(0.15f, transitions[0].Duration);
        Assert.Equal(EasingType.Ease, transitions[0].Easing);
    }

    [Fact]
    public void TransitionParse_Multiple_ParsesBoth()
    {
        var transitions = Transition.Parse("background 0.15s ease, color 0.2s ease-in-out");
        Assert.Equal(2, transitions.Length);
        Assert.Equal("background", transitions[0].Property);
        AssertApprox(0.15f, transitions[0].Duration);
        Assert.Equal(EasingType.Ease, transitions[0].Easing);
        Assert.Equal("color", transitions[1].Property);
        AssertApprox(0.2f, transitions[1].Duration);
        Assert.Equal(EasingType.EaseInOut, transitions[1].Easing);
    }

    [Fact]
    public void TransitionParse_Milliseconds_ConvertsToSeconds()
    {
        var transitions = Transition.Parse("opacity 200ms linear");
        Assert.Single(transitions);
        Assert.Equal("opacity", transitions[0].Property);
        AssertApprox(0.2f, transitions[0].Duration);
        Assert.Equal(EasingType.Linear, transitions[0].Easing);
    }

    [Fact]
    public void TransitionParse_WithDelay_ParsesDelay()
    {
        var transitions = Transition.Parse("transform 0.3s ease-out 0.1s");
        Assert.Single(transitions);
        AssertApprox(0.3f, transitions[0].Duration);
        Assert.Equal(EasingType.EaseOut, transitions[0].Easing);
        AssertApprox(0.1f, transitions[0].Delay);
    }

    [Fact]
    public void TransitionParse_Empty_ReturnsEmptyArray()
    {
        var transitions = Transition.Parse("");
        Assert.Empty(transitions);
    }

    // ========================================================================
    // Style.Merge
    // ========================================================================

    [Fact]
    public void Merge_OverlayWins_ForDefinedValues()
    {
        var baseStyle = new ReactUI.Style.Style
        {
            FontSize = 14f,
            Opacity = 1f,
            Background = UIColor.White,
        };

        var overlay = new ReactUI.Style.Style
        {
            FontSize = 18f,
            Background = UIColor.Black,
        };

        var merged = baseStyle.Merge(overlay);
        AssertApprox(18f, merged.FontSize!.Value);
        AssertApprox(0f, merged.Background!.Value.R);
        AssertApprox(0f, merged.Background!.Value.G);
        AssertApprox(0f, merged.Background!.Value.B);
    }

    [Fact]
    public void Merge_BasePreserved_ForUndefinedOverlay()
    {
        var baseStyle = new ReactUI.Style.Style
        {
            FontSize = 14f,
            Opacity = 0.8f,
        };

        var overlay = new ReactUI.Style.Style
        {
            FontSize = 18f,
            // Opacity is null in overlay
        };

        var merged = baseStyle.Merge(overlay);
        AssertApprox(18f, merged.FontSize!.Value);
        AssertApprox(0.8f, merged.Opacity!.Value);
    }

    [Fact]
    public void Merge_NullOverlay_ReturnsBase()
    {
        var baseStyle = new ReactUI.Style.Style
        {
            FontSize = 14f,
        };

        var merged = baseStyle.Merge(null!);
        AssertApprox(14f, merged.FontSize!.Value);
    }

    [Fact]
    public void Merge_LayoutProperties_OverlayWins()
    {
        var baseStyle = new ReactUI.Style.Style
        {
            FlexDirection = FlexDirection.Row,
            Gap = 8f,
            Padding = new EdgeValues(10),
        };

        var overlay = new ReactUI.Style.Style
        {
            FlexDirection = FlexDirection.Column,
            Padding = new EdgeValues(20),
        };

        var merged = baseStyle.Merge(overlay);
        Assert.Equal(FlexDirection.Column, merged.FlexDirection);
        AssertApprox(8f, merged.Gap!.Value);
        AssertApprox(20f, merged.Padding!.Value.Top);
    }
}
