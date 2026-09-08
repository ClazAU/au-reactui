using ReactUI.Core;
using ReactUI.Input;
using ReactUI.Style;
using Xunit;

namespace ReactUI.Tests;

/// <summary>
/// Bare containers are transparent to the pointer. Roots are laid out full-screen,
/// so without this a later-mounted root's empty wrapper would swallow every hit
/// and the panels underneath could no longer be hovered, clicked or dragged.
/// </summary>
public class HitTestingTests
{
    private static UINode Node(string type, float x, float y, float w, float h, Style.Style? style = null)
    {
        var node = new UINode(type)
        {
            ScreenRect = new Rect(x, y, w, h),
            ComputedStyle = style ?? new Style.Style(),
        };
        return node;
    }

    private static UINode Root(params UINode[] children)
    {
        var root = Node("div", 0, 0, 1280, 720);
        foreach (var child in children)
        {
            child.Parent = root;
            root.Children.Add(child);
        }
        return root;
    }

    [Fact]
    public void EmptyFullScreenRoot_DoesNotCaptureHits()
    {
        var root = Root();
        Assert.Null(HitTesting.HitTest(root, 100, 100));
    }

    [Fact]
    public void PaintedChild_IsHit_ButBareParentIsNot()
    {
        var painted = Node("div", 100, 100, 200, 100, new Style.Style { Background = UIColor.FromHex("#202020") });
        var root = Root(painted);

        Assert.Same(painted, HitTesting.HitTest(root, 150, 150));
        Assert.Null(HitTesting.HitTest(root, 10, 10));
    }

    [Fact]
    public void LeafElementsAndInteractiveStyles_AreHittable()
    {
        Assert.True(HitTesting.IsHittable(Node("text", 0, 0, 10, 10)));
        Assert.True(HitTesting.IsHittable(Node("div", 0, 0, 10, 10, new Style.Style { Hover = new Style.Style() })));
        Assert.True(HitTesting.IsHittable(Node("div", 0, 0, 10, 10, new Style.Style { PointerEvents = true })));
        Assert.True(HitTesting.IsHittable(Node("div", 0, 0, 10, 10, new Style.Style { BoxShadow = new BoxShadow() })));
        Assert.False(HitTesting.IsHittable(Node("div", 0, 0, 10, 10)));
        Assert.False(HitTesting.IsHittable(Node("div", 0, 0, 10, 10, new Style.Style { Background = UIColor.Transparent })));
    }

    [Fact]
    public void PointerEventsNone_StillSkipsSubtree()
    {
        var child = Node("text", 0, 0, 100, 100);
        var blocked = Node("div", 0, 0, 200, 200, new Style.Style { PointerEvents = false });
        child.Parent = blocked;
        blocked.Children.Add(child);
        var root = Root(blocked);

        Assert.Null(HitTesting.HitTest(root, 50, 50));
    }
}
