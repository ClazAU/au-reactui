using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

public class CornerRadiusTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    [Fact]
    public void Constructor_SingleValue_AllCornersEqual()
    {
        var cr = new CornerRadius(10);
        AssertApprox(10f, cr.TopLeft);
        AssertApprox(10f, cr.TopRight);
        AssertApprox(10f, cr.BottomRight);
        AssertApprox(10f, cr.BottomLeft);
    }

    [Fact]
    public void Constructor_FourValues_IndividualCorners()
    {
        var cr = new CornerRadius(1, 2, 3, 4);
        AssertApprox(1f, cr.TopLeft);
        AssertApprox(2f, cr.TopRight);
        AssertApprox(3f, cr.BottomRight);
        AssertApprox(4f, cr.BottomLeft);
    }

    [Fact]
    public void ImplicitFromFloat_AllCornersEqual()
    {
        CornerRadius cr = 15f;
        AssertApprox(15f, cr.TopLeft);
        AssertApprox(15f, cr.TopRight);
        AssertApprox(15f, cr.BottomRight);
        AssertApprox(15f, cr.BottomLeft);
    }

    [Fact]
    public void ImplicitFromTuple_IndividualCorners()
    {
        CornerRadius cr = (5f, 10f, 15f, 20f);
        AssertApprox(5f, cr.TopLeft);
        AssertApprox(10f, cr.TopRight);
        AssertApprox(15f, cr.BottomRight);
        AssertApprox(20f, cr.BottomLeft);
    }

    [Fact]
    public void Constructor_Zero_AllCornersZero()
    {
        var cr = new CornerRadius(0);
        AssertApprox(0f, cr.TopLeft);
        AssertApprox(0f, cr.TopRight);
        AssertApprox(0f, cr.BottomRight);
        AssertApprox(0f, cr.BottomLeft);
    }
}
