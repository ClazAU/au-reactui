using ReactUI.Style;
using Xunit;
using S = ReactUI.Style.Style;

namespace ReactUI.Tests;

public class StyleSheetTests
{
    private static void AssertApprox(float expected, float actual, float tolerance = 0.01f)
    {
        Assert.True(
            System.Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected} +/- {tolerance}, but got {actual}");
    }

    [Fact]
    public void SetAndGet_ByName()
    {
        var sheet = new StyleSheet();
        sheet[".panel"] = new S { FontSize = 22 };

        var style = sheet[".panel"];
        AssertApprox(22, style.FontSize!.Value);
    }

    [Fact]
    public void Get_WithoutDot_StillResolves()
    {
        var sheet = new StyleSheet();
        sheet["panel"] = new S { FontSize = 22 };

        // Both with and without dot should work
        Assert.True(sheet.Contains(".panel"));
        Assert.True(sheet.Contains("panel"));
        AssertApprox(22, sheet[".panel"].FontSize!.Value);
        AssertApprox(22, sheet["panel"].FontSize!.Value);
    }

    [Fact]
    public void Get_Undefined_ReturnsEmptyStyle()
    {
        var sheet = new StyleSheet();
        var style = sheet[".nonexistent"];
        Assert.Null(style.FontSize);
    }

    [Fact]
    public void Resolve_SingleClass()
    {
        var sheet = new StyleSheet();
        sheet[".title"] = new S { FontSize = 32, FontWeight = 700 };

        var result = sheet.Resolve("title");
        AssertApprox(32, result.FontSize!.Value);
        Assert.Equal(700, result.FontWeight);
    }

    [Fact]
    public void Resolve_MultipleClasses_MergesLeftToRight()
    {
        var sheet = new StyleSheet();
        sheet[".base"] = new S { FontSize = 14, Color = UIColor.White };
        sheet[".large"] = new S { FontSize = 24 };

        var result = sheet.Resolve("base large");
        AssertApprox(24, result.FontSize!.Value); // large wins
        AssertApprox(1f, result.Color!.Value.R);   // base preserved
    }

    [Fact]
    public void Resolve_WithInlineOverride()
    {
        var sheet = new StyleSheet();
        sheet[".btn"] = new S { Background = UIColor.White, BorderRadius = 6 };

        var inline = new S { Background = UIColor.Black };
        var result = sheet.Resolve("btn", inline);

        AssertApprox(0f, result.Background!.Value.R); // inline wins
        AssertApprox(6f, result.BorderRadius!.Value);  // class preserved
    }

    [Fact]
    public void Resolve_NullClassName_ReturnsInline()
    {
        var sheet = new StyleSheet();
        var inline = new S { FontSize = 18 };

        var result = sheet.Resolve(null, inline);
        AssertApprox(18, result.FontSize!.Value);
    }

    [Fact]
    public void Resolve_NullInline_ReturnsClass()
    {
        var sheet = new StyleSheet();
        sheet[".btn"] = new S { FontSize = 16 };

        var result = sheet.Resolve("btn", null);
        AssertApprox(16, result.FontSize!.Value);
    }

    [Fact]
    public void Merge_CombinesSheets()
    {
        var a = new StyleSheet();
        a[".a"] = new S { FontSize = 10 };

        var b = new StyleSheet();
        b[".b"] = new S { FontSize = 20 };

        a.Merge(b);

        Assert.True(a.Contains(".a"));
        Assert.True(a.Contains(".b"));
    }

    [Fact]
    public void Merge_OverridesOnConflict()
    {
        var a = new StyleSheet();
        a[".btn"] = new S { FontSize = 10 };

        var b = new StyleSheet();
        b[".btn"] = new S { FontSize = 20 };

        a.Merge(b);

        AssertApprox(20, a[".btn"].FontSize!.Value);
    }

    [Fact]
    public void GlobalStyles_RegisterAndResolve()
    {
        GlobalStyles.Clear();

        GlobalStyles.Register("card", new S { BorderRadius = 12, Padding = new EdgeValues(16) });

        var style = GlobalStyles.Resolve("card");
        AssertApprox(12, style.BorderRadius!.Value);
        AssertApprox(16, style.Padding!.Value.Top.Value);
    }

    [Fact]
    public void GlobalStyles_RegisterSheet()
    {
        GlobalStyles.Clear();

        var sheet = new StyleSheet();
        sheet[".a"] = new S { FontSize = 10 };
        sheet[".b"] = new S { FontSize = 20 };

        GlobalStyles.Register(sheet);

        AssertApprox(10, GlobalStyles.Resolve("a").FontSize!.Value);
        AssertApprox(20, GlobalStyles.Resolve("b").FontSize!.Value);
    }
}
