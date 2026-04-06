using ReactUI.Core;
using Xunit;

namespace ReactUI.Tests;

public class RectTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    // ──────────────────────────────────────────────
    //  Constructor & properties
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsAllFields()
    {
        var r = new Rect(10, 20, 100, 50);
        AssertApprox(10f, r.X);
        AssertApprox(20f, r.Y);
        AssertApprox(100f, r.Width);
        AssertApprox(50f, r.Height);
    }

    [Fact]
    public void Right_ReturnsXPlusWidth()
    {
        var r = new Rect(10, 0, 100, 50);
        AssertApprox(110f, r.Right);
    }

    [Fact]
    public void Bottom_ReturnsYPlusHeight()
    {
        var r = new Rect(0, 20, 100, 50);
        AssertApprox(70f, r.Bottom);
    }

    [Fact]
    public void Zero_IsAllZeroes()
    {
        var r = Rect.Zero;
        AssertApprox(0f, r.X);
        AssertApprox(0f, r.Y);
        AssertApprox(0f, r.Width);
        AssertApprox(0f, r.Height);
    }

    // ──────────────────────────────────────────────
    //  Contains
    // ──────────────────────────────────────────────

    [Fact]
    public void Contains_PointInside_ReturnsTrue()
    {
        var r = new Rect(0, 0, 100, 100);
        Assert.True(r.Contains(50, 50));
    }

    [Fact]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var r = new Rect(0, 0, 100, 100);
        Assert.False(r.Contains(150, 50));
    }

    [Fact]
    public void Contains_PointOnLeftEdge_ReturnsTrue()
    {
        var r = new Rect(10, 10, 100, 100);
        Assert.True(r.Contains(10, 50));
    }

    [Fact]
    public void Contains_PointOnTopEdge_ReturnsTrue()
    {
        var r = new Rect(10, 10, 100, 100);
        Assert.True(r.Contains(50, 10));
    }

    [Fact]
    public void Contains_PointOnRightEdge_ReturnsFalse()
    {
        var r = new Rect(10, 10, 100, 100);
        // CSS/browser: right edge (X=110) is exclusive — point at exactly X+Width is outside
        // e.g. elementFromPoint(110, 50) does NOT hit a 10,10,100,100 rect
        Assert.False(r.Contains(110, 50));
    }

    [Fact]
    public void Contains_PointOnBottomEdge_ReturnsFalse()
    {
        var r = new Rect(10, 10, 100, 100);
        // CSS/browser: bottom edge (Y=110) is exclusive — point at exactly Y+Height is outside
        Assert.False(r.Contains(50, 110));
    }

    [Fact]
    public void Contains_PointAbove_ReturnsFalse()
    {
        var r = new Rect(10, 10, 100, 100);
        Assert.False(r.Contains(50, 5));
    }

    [Fact]
    public void Contains_PointLeftOf_ReturnsFalse()
    {
        var r = new Rect(10, 10, 100, 100);
        Assert.False(r.Contains(5, 50));
    }

    [Fact]
    public void Contains_ZeroSizeRect_ContainsNothing()
    {
        // A zero-size element has no area — nothing should hit it
        var r = new Rect(50, 50, 0, 0);
        Assert.False(r.Contains(50, 50));
    }

    // ──────────────────────────────────────────────
    //  ToString
    // ──────────────────────────────────────────────

    [Fact]
    public void ToString_ContainsAllValues()
    {
        var r = new Rect(10, 20, 100, 50);
        var s = r.ToString();
        Assert.Contains("10", s);
        Assert.Contains("20", s);
        Assert.Contains("100", s);
        Assert.Contains("50", s);
    }
}
