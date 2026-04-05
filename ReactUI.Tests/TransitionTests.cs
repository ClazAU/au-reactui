using System;
using ReactUI.Animation;
using ReactUI.Core;
using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

public class TransitionTests : IDisposable
{
    public TransitionTests()
    {
        TransitionEngine.Reset();
    }

    public void Dispose()
    {
        TransitionEngine.Reset();
    }

    static Transition[] MakeTransition(string prop, float duration, EasingType easing = EasingType.Linear, float delay = 0f) =>
        new[] { new Transition { Property = prop, Duration = duration, Delay = delay, Easing = easing } };

    static void AssertApprox(float expected, float actual, float tolerance = 0.02f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    // 1. No transitions returns target
    [Fact]
    public void GetAnimatedValue_NullTransitions_ReturnsTarget()
    {
        var node = new UINode("div");
        var result = TransitionEngine.GetAnimatedValue(node, "opacity", 0.5f, null);
        Assert.Equal(0.5f, result);
    }

    // 2. Empty transitions array returns target
    [Fact]
    public void GetAnimatedValue_EmptyTransitions_ReturnsTarget()
    {
        var node = new UINode("div");
        var result = TransitionEngine.GetAnimatedValue(node, "opacity", 0.5f, System.Array.Empty<Transition>());
        Assert.Equal(0.5f, result);
    }

    // 3. First call stores value without animating
    [Fact]
    public void GetAnimatedValue_FirstCall_ReturnsTargetWithoutAnimation()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f);
        var result = TransitionEngine.GetAnimatedValue(node, "opacity", 0.8f, tr);
        Assert.Equal(0.8f, result);
    }

    // 4. Second call with different value starts animation
    [Fact]
    public void GetAnimatedValue_SecondCallDifferentValue_StartsAnimation()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f);

        // First call: stores 0.0f
        TransitionEngine.GetAnimatedValue(node, "opacity", 0.0f, tr);

        // Second call: target changes to 1.0f, animation starts from 0.0f
        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1.0f, tr)!;

        // At elapsed=0, we should get the from value (0.0f)
        AssertApprox(0.0f, result);
    }

    // 5. Tick advances animation
    [Fact]
    public void Tick_AdvancesAnimation_ReturnsInterpolatedValue()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f);

        TransitionEngine.GetAnimatedValue(node, "opacity", 0.0f, tr);
        TransitionEngine.GetAnimatedValue(node, "opacity", 1.0f, tr);

        TransitionEngine.Tick(0.5f);

        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1.0f, tr)!;
        AssertApprox(0.5f, result);
    }

    // 6. Animation completes after duration
    [Fact]
    public void Tick_PastDuration_ReturnsTargetValue()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f);

        TransitionEngine.GetAnimatedValue(node, "opacity", 0.0f, tr);
        TransitionEngine.GetAnimatedValue(node, "opacity", 1.0f, tr);

        // Tick past the full duration
        TransitionEngine.Tick(1.5f);

        // After completion, animation is cleaned up; next call sees no active animation
        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1.0f, tr)!;
        Assert.Equal(1.0f, result);
    }

    // 7. Delay postpones animation
    [Fact]
    public void Delay_PostponesAnimation_ReturnsFromValueDuringDelay()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f, delay: 0.5f);

        TransitionEngine.GetAnimatedValue(node, "opacity", 0.0f, tr);
        TransitionEngine.GetAnimatedValue(node, "opacity", 1.0f, tr);

        TransitionEngine.Tick(0.3f);

        // Still within delay period, should return from value
        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1.0f, tr)!;
        AssertApprox(0.0f, result);
    }

    // 8. Float interpolation at midpoint
    [Fact]
    public void FloatInterpolation_AtMidpoint_ReturnsHalfway()
    {
        var node = new UINode("div");
        var tr = MakeTransition("width", 1.0f);

        TransitionEngine.GetAnimatedValue(node, "width", 0f, tr);
        TransitionEngine.GetAnimatedValue(node, "width", 100f, tr);

        TransitionEngine.Tick(0.5f);

        var result = (float)TransitionEngine.GetAnimatedValue(node, "width", 100f, tr)!;
        AssertApprox(50f, result);
    }

    // 9. UIColor interpolation
    [Fact]
    public void UIColorInterpolation_BlackToWhite_ReturnsGray()
    {
        var node = new UINode("div");
        var tr = MakeTransition("background", 1.0f);

        TransitionEngine.GetAnimatedValue(node, "background", UIColor.Black, tr);
        TransitionEngine.GetAnimatedValue(node, "background", UIColor.White, tr);

        TransitionEngine.Tick(0.5f);

        var result = (UIColor)TransitionEngine.GetAnimatedValue(node, "background", UIColor.White, tr)!;
        AssertApprox(0.5f, result.R);
        AssertApprox(0.5f, result.G);
        AssertApprox(0.5f, result.B);
    }

    // 10. EdgeValues interpolation
    [Fact]
    public void EdgeValuesInterpolation_ZeroToTwenty_ReturnsTen()
    {
        var node = new UINode("div");
        var tr = MakeTransition("padding", 1.0f);

        var from = new EdgeValues(0f);
        var to = new EdgeValues(20f);

        TransitionEngine.GetAnimatedValue(node, "padding", from, tr);
        TransitionEngine.GetAnimatedValue(node, "padding", to, tr);

        TransitionEngine.Tick(0.5f);

        var result = (EdgeValues)TransitionEngine.GetAnimatedValue(node, "padding", to, tr)!;
        AssertApprox(10f, result.Top);
        AssertApprox(10f, result.Right);
        AssertApprox(10f, result.Bottom);
        AssertApprox(10f, result.Left);
    }

    // 11. "all" property matches any property name
    [Fact]
    public void AllProperty_MatchesAnyPropertyName()
    {
        var node = new UINode("div");
        var tr = MakeTransition("all", 1.0f);

        TransitionEngine.GetAnimatedValue(node, "opacity", 0f, tr);
        TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr);

        TransitionEngine.Tick(0.5f);

        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr)!;
        AssertApprox(0.5f, result);
    }

    // 12. Unmatched property returns target immediately
    [Fact]
    public void UnmatchedProperty_ReturnsTargetImmediately()
    {
        var node = new UINode("div");
        var tr = MakeTransition("background", 1.0f);

        // "opacity" does not match "background" transition
        var result = TransitionEngine.GetAnimatedValue(node, "opacity", 0.7f, tr);
        Assert.Equal(0.7f, result);
    }

    // 13. RemoveNode clears animations
    [Fact]
    public void RemoveNode_ClearsAnimations_SubsequentCallsStartFresh()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f);

        TransitionEngine.GetAnimatedValue(node, "opacity", 0f, tr);
        TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr);

        TransitionEngine.RemoveNode(node);

        // After removal, first call stores value without animating
        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr)!;
        Assert.Equal(1f, result);
    }

    // 14. Reset clears everything
    [Fact]
    public void Reset_ClearsAllState()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f);

        TransitionEngine.GetAnimatedValue(node, "opacity", 0f, tr);
        TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr);

        TransitionEngine.Reset();

        // After reset, first call stores value without animating
        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr)!;
        Assert.Equal(1f, result);
    }

    // 15. Re-targeting mid-animation
    [Fact]
    public void RetargetMidAnimation_AnimatesFromCurrentToNewTarget()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f);

        // Set initial value
        TransitionEngine.GetAnimatedValue(node, "opacity", 0f, tr);
        // Start animating to 1.0
        TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr);

        // Advance to midpoint: current interpolated = 0.5
        TransitionEngine.Tick(0.5f);

        // Verify we are at ~0.5
        var mid = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr)!;
        AssertApprox(0.5f, mid);

        // Re-target to 0.0 (from current ~0.5)
        var retargeted = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 0f, tr)!;
        // Just started the new animation from ~0.5, elapsed=0 -> returns from value (~0.5)
        AssertApprox(0.5f, retargeted);

        // Advance new animation to midpoint
        TransitionEngine.Tick(0.5f);
        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 0f, tr)!;
        // Midpoint between ~0.5 and 0.0 -> ~0.25
        AssertApprox(0.25f, result);
    }

    // 16. Delay then animation completes correctly
    [Fact]
    public void DelayThenAnimation_CompletesCorrectly()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f, delay: 0.5f);

        TransitionEngine.GetAnimatedValue(node, "opacity", 0f, tr);
        TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr);

        // Tick past delay (0.5) and halfway through animation (0.5 more)
        TransitionEngine.Tick(1.0f);

        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr)!;
        AssertApprox(0.5f, result);
    }

    // 17. Same value does not restart animation
    [Fact]
    public void SameValue_DoesNotRestartAnimation()
    {
        var node = new UINode("div");
        var tr = MakeTransition("opacity", 1.0f);

        TransitionEngine.GetAnimatedValue(node, "opacity", 0f, tr);
        TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr);

        TransitionEngine.Tick(0.5f);

        // Call again with same target (1f) -- should NOT restart
        var result = (float)TransitionEngine.GetAnimatedValue(node, "opacity", 1f, tr)!;
        AssertApprox(0.5f, result);
    }

    // 18. BoxShadow interpolation
    [Fact]
    public void BoxShadowInterpolation_LerpsAllFields()
    {
        var node = new UINode("div");
        var tr = MakeTransition("box-shadow", 1.0f);

        var from = new BoxShadow { OffsetX = 0, OffsetY = 0, Blur = 0, Spread = 0, Color = UIColor.Black };
        var to = new BoxShadow { OffsetX = 10, OffsetY = 20, Blur = 30, Spread = 4, Color = UIColor.White };

        TransitionEngine.GetAnimatedValue(node, "box-shadow", from, tr);
        TransitionEngine.GetAnimatedValue(node, "box-shadow", to, tr);

        TransitionEngine.Tick(0.5f);

        var result = (BoxShadow)TransitionEngine.GetAnimatedValue(node, "box-shadow", to, tr)!;
        AssertApprox(5f, result.OffsetX);
        AssertApprox(10f, result.OffsetY);
        AssertApprox(15f, result.Blur);
        AssertApprox(2f, result.Spread);
        AssertApprox(0.5f, result.Color.R);
    }
}
