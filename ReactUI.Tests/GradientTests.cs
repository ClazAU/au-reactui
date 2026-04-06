using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

public class GradientTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    // ──────────────────────────────────────────────
    //  Implicit string conversion
    // ──────────────────────────────────────────────

    [Fact]
    public void ImplicitFromString_LinearGradient_ParsesCorrectly()
    {
        Gradient g = "linear-gradient(90deg, #ff0000, #0000ff)";
        Assert.Equal(GradientType.Linear, g.Type);
        AssertApprox(90f, g.Angle);
    }

    [Fact]
    public void ImplicitFromString_RadialGradient_ParsesCorrectly()
    {
        Gradient g = "radial-gradient(#fff, #000)";
        Assert.Equal(GradientType.Radial, g.Type);
    }

    [Fact]
    public void ImplicitFromString_Empty_ReturnsNone()
    {
        Gradient g = "";
        Assert.Equal(GradientType.None, g.Type);
    }

    // ──────────────────────────────────────────────
    //  ToString
    // ──────────────────────────────────────────────

    [Fact]
    public void ToString_None_ContainsNone()
    {
        var g = new Gradient { Type = GradientType.None };
        Assert.Contains("none", g.ToString().ToLower());
    }

    [Fact]
    public void ToString_Linear_ContainsLinearGradient()
    {
        var g = new Gradient
        {
            Type = GradientType.Linear,
            Angle = 45f,
            ColorA = UIColor.White,
            ColorB = UIColor.Black
        };
        var s = g.ToString();
        Assert.Contains("linear-gradient", s.ToLower());
    }

    [Fact]
    public void ToString_Radial_ContainsRadialGradient()
    {
        var g = new Gradient
        {
            Type = GradientType.Radial,
            ColorA = UIColor.White,
            ColorB = UIColor.Black
        };
        var s = g.ToString();
        Assert.Contains("radial-gradient", s.ToLower());
    }

    // ──────────────────────────────────────────────
    //  Color parsing
    // ──────────────────────────────────────────────

    [Fact]
    public void LinearGradient_ParsesColors()
    {
        Gradient g = "linear-gradient(0deg, #ff0000, #00ff00)";
        AssertApprox(1f, g.ColorA.R);
        AssertApprox(0f, g.ColorA.G);
        AssertApprox(0f, g.ColorB.R);
        AssertApprox(1f, g.ColorB.G);
    }

    [Fact]
    public void LinearGradient_ToRight_Is90Degrees()
    {
        Gradient g = "linear-gradient(to right, #000, #fff)";
        Assert.Equal(GradientType.Linear, g.Type);
        AssertApprox(90f, g.Angle);
    }

    [Fact]
    public void LinearGradient_ToBottom_Is180Degrees()
    {
        Gradient g = "linear-gradient(to bottom, #000, #fff)";
        Assert.Equal(GradientType.Linear, g.Type);
        AssertApprox(180f, g.Angle);
    }

    [Fact]
    public void LinearGradient_ToLeft_Is270Degrees()
    {
        Gradient g = "linear-gradient(to left, #000, #fff)";
        Assert.Equal(GradientType.Linear, g.Type);
        AssertApprox(270f, g.Angle);
    }

    [Fact]
    public void LinearGradient_ToTop_Is0Degrees()
    {
        Gradient g = "linear-gradient(to top, #000, #fff)";
        Assert.Equal(GradientType.Linear, g.Type);
        AssertApprox(0f, g.Angle);
    }
}
