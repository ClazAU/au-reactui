using ReactUI.Style;
using Xunit;
using S = ReactUI.Style.Style;

namespace ReactUI.Tests;

public class StyleResolverTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    // ──────────────────────────────────────────────
    //  DefaultStyle
    // ──────────────────────────────────────────────

    [Fact]
    public void DefaultStyle_FlexDirection_IsColumn()
    {
        var s = StyleResolver.DefaultStyle();
        Assert.Equal(FlexDirection.Column, s.FlexDirection);
    }

    [Fact]
    public void DefaultStyle_AlignItems_IsStretch()
    {
        var s = StyleResolver.DefaultStyle();
        Assert.Equal(AlignItems.Stretch, s.AlignItems);
    }

    [Fact]
    public void DefaultStyle_Opacity_IsOne()
    {
        var s = StyleResolver.DefaultStyle();
        AssertApprox(1f, s.Opacity!.Value);
    }

    [Fact]
    public void DefaultStyle_Position_IsRelative()
    {
        var s = StyleResolver.DefaultStyle();
        Assert.Equal(PositionType.Relative, s.Position);
    }

    [Fact]
    public void DefaultStyle_FontSize_Is14()
    {
        var s = StyleResolver.DefaultStyle();
        AssertApprox(14f, s.FontSize!.Value);
    }

    [Fact]
    public void DefaultStyle_PointerEvents_IsTrue()
    {
        var s = StyleResolver.DefaultStyle();
        Assert.True(s.PointerEvents);
    }

    // ──────────────────────────────────────────────
    //  Resolve - basic
    // ──────────────────────────────────────────────

    [Fact]
    public void Resolve_NullElementStyle_ReturnsDefaults()
    {
        var result = StyleResolver.Resolve(null, null, false, false, false);
        Assert.Equal(FlexDirection.Column, result.FlexDirection);
        AssertApprox(1f, result.Opacity!.Value);
    }

    [Fact]
    public void Resolve_ElementStyleOverridesDefaults()
    {
        var element = new S
        {
            FlexDirection = FlexDirection.Row,
            Opacity = 0.5f,
        };

        var result = StyleResolver.Resolve(element, null, false, false, false);
        Assert.Equal(FlexDirection.Row, result.FlexDirection);
        AssertApprox(0.5f, result.Opacity!.Value);
    }

    // ──────────────────────────────────────────────
    //  Resolve - text inheritance
    // ──────────────────────────────────────────────

    [Fact]
    public void Resolve_InheritsColorFromParent()
    {
        var parent = new S
        {
            Color = new UIColor(1, 0, 0, 1),
        };

        var result = StyleResolver.Resolve(new S(), parent, false, false, false);

        Assert.NotNull(result.Color);
        AssertApprox(1f, result.Color!.Value.R);
        AssertApprox(0f, result.Color!.Value.G);
    }

    [Fact]
    public void Resolve_InheritsFontSizeFromParent()
    {
        var parent = new S
        {
            FontSize = 24f,
        };

        var result = StyleResolver.Resolve(new S(), parent, false, false, false);
        AssertApprox(24f, result.FontSize!.Value);
    }

    [Fact]
    public void Resolve_ElementStyleOverridesInheritedColor()
    {
        var parent = new S
        {
            Color = new UIColor(1, 0, 0, 1),
        };
        var element = new S
        {
            Color = new UIColor(0, 1, 0, 1),
        };

        var result = StyleResolver.Resolve(element, parent, false, false, false);
        AssertApprox(0f, result.Color!.Value.R);
        AssertApprox(1f, result.Color!.Value.G);
    }

    // ──────────────────────────────────────────────
    //  Resolve - pseudo-states
    // ──────────────────────────────────────────────

    [Fact]
    public void Resolve_HoverStyle_AppliedWhenHovered()
    {
        var element = new S
        {
            Opacity = 1f,
            Hover = new S { Opacity = 0.8f },
        };

        var result = StyleResolver.Resolve(element, null, isHovered: true, false, false);
        AssertApprox(0.8f, result.Opacity!.Value);
    }

    [Fact]
    public void Resolve_HoverStyle_NotAppliedWhenNotHovered()
    {
        var element = new S
        {
            Opacity = 1f,
            Hover = new S { Opacity = 0.8f },
        };

        var result = StyleResolver.Resolve(element, null, isHovered: false, false, false);
        AssertApprox(1f, result.Opacity!.Value);
    }

    [Fact]
    public void Resolve_ActiveStyle_AppliedWhenActive()
    {
        var element = new S
        {
            Background = UIColor.White,
            Active = new S { Background = UIColor.Black },
        };

        var result = StyleResolver.Resolve(element, null, false, isActive: true, false);
        AssertApprox(0f, result.Background!.Value.R);
    }

    [Fact]
    public void Resolve_FocusStyle_AppliedWhenFocused()
    {
        var element = new S
        {
            BorderColor = UIColor.Black,
            Focus = new S { BorderColor = new UIColor(0, 0, 1, 1) },
        };

        var result = StyleResolver.Resolve(element, null, false, false, isFocused: true);
        AssertApprox(0f, result.BorderColor!.Value.R);
        AssertApprox(0f, result.BorderColor!.Value.G);
        AssertApprox(1f, result.BorderColor!.Value.B);
    }

    [Fact]
    public void Resolve_ActiveOverridesHover()
    {
        var element = new S
        {
            Opacity = 1f,
            Hover = new S { Opacity = 0.8f },
            Active = new S { Opacity = 0.6f },
        };

        var result = StyleResolver.Resolve(element, null, isHovered: true, isActive: true, false);
        AssertApprox(0.6f, result.Opacity!.Value);
    }

    // ──────────────────────────────────────────────
    //  Resolve - non-inherited properties stay default
    // ──────────────────────────────────────────────

    [Fact]
    public void Resolve_DoesNotInheritPaddingFromParent()
    {
        var parent = new S
        {
            Padding = new EdgeValues(20),
        };

        var result = StyleResolver.Resolve(new S(), parent, false, false, false);
        var defaults = StyleResolver.DefaultStyle();
        Assert.Equal(defaults.Padding, result.Padding);
    }
}
