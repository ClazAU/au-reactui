using ReactUI.Animation;
using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

public class EasingTests
{
    private const float Tolerance = 0.001f;

    private static void AssertApprox(float expected, float actual, string label = "")
    {
        Assert.True(
            System.MathF.Abs(expected - actual) <= Tolerance,
            $"{label} expected {expected} but was {actual}");
    }

    // 1. Linear: exact values at 0, 0.5, 1
    [Fact]
    public void Linear_Zero() => AssertApprox(0f, Easing.Evaluate(EasingType.Linear, 0f));

    [Fact]
    public void Linear_Half() => AssertApprox(0.5f, Easing.Evaluate(EasingType.Linear, 0.5f));

    [Fact]
    public void Linear_One() => AssertApprox(1f, Easing.Evaluate(EasingType.Linear, 1f));

    // 2. All easing types return 0 at t=0 and 1 at t=1
    [Theory]
    [InlineData(EasingType.Linear)]
    [InlineData(EasingType.Ease)]
    [InlineData(EasingType.EaseIn)]
    [InlineData(EasingType.EaseOut)]
    [InlineData(EasingType.EaseInOut)]
    public void AllTypes_BoundaryZero(EasingType type)
    {
        AssertApprox(0f, Easing.Evaluate(type, 0f), $"{type} at t=0");
    }

    [Theory]
    [InlineData(EasingType.Linear)]
    [InlineData(EasingType.Ease)]
    [InlineData(EasingType.EaseIn)]
    [InlineData(EasingType.EaseOut)]
    [InlineData(EasingType.EaseInOut)]
    public void AllTypes_BoundaryOne(EasingType type)
    {
        AssertApprox(1f, Easing.Evaluate(type, 1f), $"{type} at t=1");
    }

    // 3. Ease at t=0.5 should be > 0.5
    [Fact]
    public void Ease_MidpointAboveHalf()
    {
        float val = Easing.Evaluate(EasingType.Ease, 0.5f);
        Assert.True(val > 0.5f, $"Ease(0.5) = {val}, expected > 0.5");
    }

    // 4. EaseIn at t=0.5 should be < 0.5 (starts slow)
    [Fact]
    public void EaseIn_MidpointBelowHalf()
    {
        float val = Easing.Evaluate(EasingType.EaseIn, 0.5f);
        Assert.True(val < 0.5f, $"EaseIn(0.5) = {val}, expected < 0.5");
    }

    // 5. EaseOut at t=0.5 should be > 0.5 (starts fast)
    [Fact]
    public void EaseOut_MidpointAboveHalf()
    {
        float val = Easing.Evaluate(EasingType.EaseOut, 0.5f);
        Assert.True(val > 0.5f, $"EaseOut(0.5) = {val}, expected > 0.5");
    }

    // 6. EaseInOut: at 0.25 < 0.25, at 0.75 > 0.75
    [Fact]
    public void EaseInOut_QuarterBelowQuarter()
    {
        float val = Easing.Evaluate(EasingType.EaseInOut, 0.25f);
        Assert.True(val < 0.25f, $"EaseInOut(0.25) = {val}, expected < 0.25");
    }

    [Fact]
    public void EaseInOut_ThreeQuarterAboveThreeQuarter()
    {
        float val = Easing.Evaluate(EasingType.EaseInOut, 0.75f);
        Assert.True(val > 0.75f, $"EaseInOut(0.75) = {val}, expected > 0.75");
    }

    // 7. CubicBezier with (0,0,1,1) approximates linear
    [Fact]
    public void CubicBezier_Linear()
    {
        for (float t = 0f; t <= 1f; t += 0.1f)
        {
            float val = Easing.CubicBezier(t, 0f, 0f, 1f, 1f);
            AssertApprox(t, val, $"CubicBezier(linear) at t={t}");
        }
    }

    // 8. Monotonicity: standard easings produce monotonically increasing output
    [Theory]
    [InlineData(EasingType.Linear)]
    [InlineData(EasingType.Ease)]
    [InlineData(EasingType.EaseIn)]
    [InlineData(EasingType.EaseOut)]
    [InlineData(EasingType.EaseInOut)]
    public void AllTypes_Monotonic(EasingType type)
    {
        float prev = 0f;
        for (int i = 1; i <= 100; i++)
        {
            float t = i / 100f;
            float val = Easing.Evaluate(type, t);
            Assert.True(val >= prev - Tolerance,
                $"{type} not monotonic at t={t}: {val} < {prev}");
            prev = val;
        }
    }

    // 9. Range: standard CSS easings stay within [0, 1]
    [Theory]
    [InlineData(EasingType.Linear)]
    [InlineData(EasingType.Ease)]
    [InlineData(EasingType.EaseIn)]
    [InlineData(EasingType.EaseOut)]
    [InlineData(EasingType.EaseInOut)]
    public void AllTypes_OutputInRange(EasingType type)
    {
        for (int i = 0; i <= 100; i++)
        {
            float t = i / 100f;
            float val = Easing.Evaluate(type, t);
            Assert.True(val >= -0.01f && val <= 1.01f,
                $"{type} out of range at t={t}: {val}");
        }
    }
}
