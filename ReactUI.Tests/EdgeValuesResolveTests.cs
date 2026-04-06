using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

public class EdgeValuesResolveTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    // ──────────────────────────────────────────────
    //  EdgeValues.Resolve
    // ──────────────────────────────────────────────

    [Fact]
    public void Resolve_PxUnit_ReturnsValue()
    {
        var result = EdgeValues.Resolve(StyleValue.Px(20), 400);
        AssertApprox(20f, result);
    }

    [Fact]
    public void Resolve_PercentUnit_ResolvesAgainstParent()
    {
        var result = EdgeValues.Resolve(StyleValue.Percent(50), 400);
        AssertApprox(200f, result);
    }

    [Fact]
    public void Resolve_PercentUnit_ZeroParent_ReturnsZero()
    {
        var result = EdgeValues.Resolve(StyleValue.Percent(50), 0);
        AssertApprox(0f, result);
    }

    [Fact]
    public void Resolve_Undefined_ReturnsZero()
    {
        var result = EdgeValues.Resolve(StyleValue.Undefined, 400);
        AssertApprox(0f, result);
    }

    // ──────────────────────────────────────────────
    //  StyleValue-based constructors
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_StyleValues_SetsAllEdges()
    {
        var e = new EdgeValues(
            StyleValue.Px(10),
            StyleValue.Percent(20),
            StyleValue.Px(30),
            StyleValue.Auto
        );

        Assert.Equal(StyleUnit.Px, e.Top.Unit);
        AssertApprox(10f, e.Top.Value);
        Assert.Equal(StyleUnit.Percent, e.Right.Unit);
        AssertApprox(20f, e.Right.Value);
        Assert.Equal(StyleUnit.Px, e.Bottom.Unit);
        AssertApprox(30f, e.Bottom.Value);
        Assert.Equal(StyleUnit.Auto, e.Left.Unit);
    }

    // ──────────────────────────────────────────────
    //  StyleValue basics
    // ──────────────────────────────────────────────

    [Fact]
    public void StyleValue_Px_CreatesCorrectUnit()
    {
        var sv = StyleValue.Px(42);
        Assert.Equal(StyleUnit.Px, sv.Unit);
        AssertApprox(42f, sv.Value);
        Assert.True(sv.IsDefined);
    }

    [Fact]
    public void StyleValue_Percent_CreatesCorrectUnit()
    {
        var sv = StyleValue.Percent(75);
        Assert.Equal(StyleUnit.Percent, sv.Unit);
        AssertApprox(75f, sv.Value);
        Assert.True(sv.IsDefined);
    }

    [Fact]
    public void StyleValue_Auto_CreatesCorrectUnit()
    {
        var sv = StyleValue.Auto;
        Assert.Equal(StyleUnit.Auto, sv.Unit);
        Assert.True(sv.IsDefined);
    }

    [Fact]
    public void StyleValue_Lerp_AtMidpoint()
    {
        var a = StyleValue.Px(0);
        var b = StyleValue.Px(100);
        var result = StyleValue.Lerp(a, b, 0.5f);

        Assert.Equal(StyleUnit.Px, result.Unit);
        AssertApprox(50f, result.Value);
    }

    [Fact]
    public void StyleValue_Lerp_AtZero_ReturnsStart()
    {
        var a = StyleValue.Px(10);
        var b = StyleValue.Px(90);
        var result = StyleValue.Lerp(a, b, 0f);
        AssertApprox(10f, result.Value);
    }

    [Fact]
    public void StyleValue_Lerp_AtOne_ReturnsEnd()
    {
        var a = StyleValue.Px(10);
        var b = StyleValue.Px(90);
        var result = StyleValue.Lerp(a, b, 1f);
        AssertApprox(90f, result.Value);
    }

    // ──────────────────────────────────────────────
    //  EdgeValues with StyleValue Lerp
    // ──────────────────────────────────────────────

    [Fact]
    public void EdgeValues_Lerp_WithStyleValues()
    {
        var a = new EdgeValues(
            StyleValue.Px(0), StyleValue.Px(0),
            StyleValue.Px(0), StyleValue.Px(0)
        );
        var b = new EdgeValues(
            StyleValue.Px(10), StyleValue.Px(20),
            StyleValue.Px(30), StyleValue.Px(40)
        );
        var result = EdgeValues.Lerp(a, b, 0.5f);

        AssertApprox(5f, result.Top.Value);
        AssertApprox(10f, result.Right.Value);
        AssertApprox(15f, result.Bottom.Value);
        AssertApprox(20f, result.Left.Value);
    }

    // ──────────────────────────────────────────────
    //  EdgeValues.ToString
    // ──────────────────────────────────────────────

    [Fact]
    public void EdgeValues_ToString_ContainsValues()
    {
        var e = new EdgeValues(5, 10, 15, 20);
        var s = e.ToString();
        Assert.NotNull(s);
        Assert.NotEmpty(s);
    }
}
