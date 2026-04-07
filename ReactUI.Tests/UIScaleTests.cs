using ReactUI.Core;
using ReactUI.Input;
using ReactUI.Layout;
using Xunit;

using FlexDirection = ReactUI.Layout.FlexDirection;
using Overflow = ReactUI.Style.Overflow;

namespace ReactUI.Tests;

/// <summary>
/// Tests that UI layout, hit testing, and scrollable containers work correctly
/// across different screen resolutions and scale factors.
/// </summary>
public class UIScaleTests
{
    private const float Tolerance = 0.5f;
    private const float ToggleHeight = 40f;
    private const float ToggleCount = 20;

    private static void AssertApprox(float expected, float actual, string label = "")
    {
        Assert.True(
            System.MathF.Abs(expected - actual) <= Tolerance,
            $"{label} expected {expected} but was {actual}");
    }

    // ══════════════════════════════════════════════
    //  UIScale factor at various resolutions
    // ══════════════════════════════════════════════

    [Theory]
    [InlineData(1920, 1080, 1.0f)]      // 1080p — reference
    [InlineData(2560, 1440, 1440f/1080)] // 1440p
    [InlineData(3840, 2160, 2.0f)]       // 4K
    [InlineData(1280, 720, 720f/1080)]   // 720p
    [InlineData(1024, 576, 576f/1080)]   // small window
    [InlineData(640, 360, 0.5f)]         // very small — hits the 0.5 clamp
    public void ScaleFactor_MatchesExpected(int screenW, int screenH, float expectedFactor)
    {
        float factor = screenH / 1080f;
        if (factor < 0.5f) factor = 0.5f;
        AssertApprox(expectedFactor, factor, $"Scale factor at {screenW}x{screenH}");
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    [InlineData(3840, 2160)]
    [InlineData(1280, 720)]
    public void LogicalViewport_Always1080pEquivalent(int screenW, int screenH)
    {
        float factor = screenH / 1080f;
        float logicalW = screenW / factor;
        float logicalH = screenH / factor;
        AssertApprox(1080f, logicalH, $"Logical height at {screenW}x{screenH}");
        // Logical width varies by aspect ratio but should be consistent
        AssertApprox((float)screenW / screenH * 1080f, logicalW, $"Logical width at {screenW}x{screenH}");
    }

    // ══════════════════════════════════════════════
    //  Scrollable container with 20 items — layout
    // ══════════════════════════════════════════════

    /// <summary>
    /// Build a scrollable container: full viewport height, 20 children of 40px each.
    /// Total content = 800px, container = viewport height. Items beyond the container
    /// should still get laid out at their correct Y positions.
    /// </summary>
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    [InlineData(3840, 2160)]
    [InlineData(1280, 720)]
    public void ScrollableContainer_LayoutsAllItems(int screenW, int screenH)
    {
        float factor = screenH / 1080f;
        float logicalW = screenW / factor;
        float logicalH = screenH / factor;

        var container = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            Width = 300,
            Height = logicalH,
        };

        var scrollArea = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            FlexGrow = 1,
        };

        for (int i = 0; i < ToggleCount; i++)
            scrollArea.AddChild(new LayoutNode { Height = ToggleHeight, Width = 300 });

        container.AddChild(scrollArea);
        YogaLayout.Calculate(container, logicalW, logicalH);

        // All 20 items should be laid out sequentially
        for (int i = 0; i < (int)ToggleCount; i++)
        {
            var item = scrollArea.Children[i];
            AssertApprox(ToggleHeight, item.ComputedHeight, $"Item {i} height");
            AssertApprox(i * ToggleHeight, item.ComputedY - scrollArea.ComputedY, $"Item {i} relative Y");
        }

        // Total content height = 20 * 40 = 800
        float lastItemBottom = scrollArea.Children[(int)ToggleCount - 1].ComputedY
                             + scrollArea.Children[(int)ToggleCount - 1].ComputedHeight;
        AssertApprox(ToggleCount * ToggleHeight, lastItemBottom - scrollArea.ComputedY, "Total content height");
    }

    // ══════════════════════════════════════════════
    //  Hit testing on scrollable items
    // ══════════════════════════════════════════════

    /// <summary>
    /// Create UINodes that simulate a scrollable list of 20 toggles.
    /// Verify hit testing finds the correct item at different scroll positions.
    /// </summary>
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    [InlineData(3840, 2160)]
    [InlineData(1280, 720)]
    public void HitTest_FindsCorrectToggle_NoScroll(int screenW, int screenH)
    {
        float factor = screenH / 1080f;
        float logicalW = screenW / factor;
        float logicalH = screenH / factor;

        var root = BuildToggleList(logicalW, logicalH, scrollOffset: 0);

        // Click center of each visible toggle
        var scrollArea = root.Children[0];
        int visibleCount = (int)(logicalH / ToggleHeight);

        for (int i = 0; i < visibleCount && i < (int)ToggleCount; i++)
        {
            float clickX = 150; // center of 300px wide container
            float clickY = i * ToggleHeight + ToggleHeight / 2; // center of item

            var hit = HitTesting.HitTest(root, clickX, clickY);
            Assert.NotNull(hit);
            Assert.Equal($"toggle-{i}", hit.Key);
        }
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    [InlineData(3840, 2160)]
    [InlineData(1280, 720)]
    public void HitTest_FindsCorrectToggle_Scrolled(int screenW, int screenH)
    {
        float factor = screenH / 1080f;
        float logicalW = screenW / factor;
        float logicalH = screenH / factor;

        // Scroll down by 200px — items 0-4 are above the viewport
        float scrollOffset = 200;
        var root = BuildToggleList(logicalW, logicalH, scrollOffset);

        // After scrolling 200px, item 5 should be near the top
        // Item 5's original Y = 200, after scroll offset = 0 on screen
        var scrollArea = root.Children[0];

        // Click at Y=20 (logical) — should hit item 5 (first visible after scroll)
        var hit = HitTesting.HitTest(root, 150, 20);
        Assert.NotNull(hit);
        Assert.Equal("toggle-5", hit.Key);

        // Click at Y=60 (logical) — should hit item 6
        hit = HitTesting.HitTest(root, 150, 60);
        Assert.NotNull(hit);
        Assert.Equal("toggle-6", hit.Key);

        // Click near the bottom of visible area
        int lastVisibleIdx = 5 + (int)(logicalH / ToggleHeight) - 1;
        if (lastVisibleIdx >= (int)ToggleCount) lastVisibleIdx = (int)ToggleCount - 1;
        float lastY = (lastVisibleIdx - 5) * ToggleHeight + ToggleHeight / 2;
        hit = HitTesting.HitTest(root, 150, lastY);
        Assert.NotNull(hit);
        Assert.Equal($"toggle-{lastVisibleIdx}", hit.Key);
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    [InlineData(3840, 2160)]
    [InlineData(1280, 720)]
    public void HitTest_ScrolledToBottom_FindsLastItems(int screenW, int screenH)
    {
        float factor = screenH / 1080f;
        float logicalW = screenW / factor;
        float logicalH = screenH / factor;

        // Scroll to show the last items
        float totalContent = ToggleCount * ToggleHeight;
        float maxScroll = totalContent - logicalH;
        if (maxScroll < 0) maxScroll = 0;

        var root = BuildToggleList(logicalW, logicalH, maxScroll);

        // Last item should be visible near the bottom
        float lastItemScreenY = (ToggleCount - 1) * ToggleHeight - maxScroll;
        var hit = HitTesting.HitTest(root, 150, lastItemScreenY + ToggleHeight / 2);
        Assert.NotNull(hit);
        Assert.Equal($"toggle-{(int)ToggleCount - 1}", hit.Key);
    }

    [Fact]
    public void HitTest_OutsideContainer_ReturnsNull()
    {
        var root = BuildToggleList(1920, 1080, scrollOffset: 0);

        // Click outside the 300px wide container
        var hit = HitTesting.HitTest(root, 400, 20);
        Assert.Null(hit);

        // Click below the container
        hit = HitTesting.HitTest(root, 150, 1100);
        Assert.Null(hit);
    }

    // ══════════════════════════════════════════════
    //  Scroll offset clamping
    // ══════════════════════════════════════════════

    [Theory]
    [InlineData(1080)]
    [InlineData(1440)]
    [InlineData(2160)]
    public void ScrollOffset_ClampedToContentBounds(int screenH)
    {
        float factor = screenH / 1080f;
        float logicalH = screenH / factor; // always 1080

        float totalContent = ToggleCount * ToggleHeight; // 800
        float maxScroll = totalContent - logicalH;

        // When content fits in viewport, max scroll should be <= 0
        Assert.True(maxScroll <= 0, $"20 items of 40px = 800px fits in 1080px viewport, maxScroll={maxScroll}");
    }

    [Fact]
    public void ScrollOffset_ContentTallerThanViewport()
    {
        float logicalH = 400; // short viewport
        float totalContent = ToggleCount * ToggleHeight; // 800
        float maxScroll = totalContent - logicalH;

        Assert.True(maxScroll > 0, "Content exceeds viewport");
        AssertApprox(400, maxScroll, "Max scroll = 800 - 400");
    }

    // ══════════════════════════════════════════════
    //  Mouse coordinate scaling
    // ══════════════════════════════════════════════

    [Theory]
    [InlineData(1080, 960, 540, 960, 540)]       // 1080p: no scaling
    [InlineData(1440, 1280, 720, 960, 540)]       // 1440p: center maps to logical center
    [InlineData(2160, 1920, 1080, 960, 540)]      // 4K: center maps to logical center
    [InlineData(720, 640, 360, 960, 540)]          // 720p: center maps to logical center
    public void MouseCoordinate_ScalesToLogical(
        int screenH,
        float screenMouseX, float screenMouseY,
        float expectedLogicalX, float expectedLogicalY)
    {
        float factor = screenH / 1080f;
        float logicalX = screenMouseX / factor;
        float logicalY = screenMouseY / factor;

        AssertApprox(expectedLogicalX, logicalX, "Logical mouse X");
        AssertApprox(expectedLogicalY, logicalY, "Logical mouse Y");
    }

    // ══════════════════════════════════════════════
    //  Layout consistency across resolutions
    // ══════════════════════════════════════════════

    [Fact]
    public void Layout_IdenticalAtAllResolutions()
    {
        int[] heights = { 720, 1080, 1440, 2160 };
        float[][]? firstLayout = null;

        foreach (int h in heights)
        {
            float factor = h / 1080f;
            float logicalW = (h * 16f / 9f) / factor; // 16:9 aspect
            float logicalH = h / factor;

            var container = new LayoutNode
            {
                FlexDirection = FlexDirection.Column, Width = 300, Height = logicalH
            };
            var scrollArea = new LayoutNode { FlexDirection = FlexDirection.Column, FlexGrow = 1 };
            for (int i = 0; i < ToggleCount; i++)
                scrollArea.AddChild(new LayoutNode { Height = ToggleHeight, Width = 300 });
            container.AddChild(scrollArea);

            YogaLayout.Calculate(container, logicalW, logicalH);

            var layout = new float[(int)ToggleCount][];
            for (int i = 0; i < ToggleCount; i++)
            {
                var item = scrollArea.Children[i];
                layout[i] = new[] { item.ComputedX, item.ComputedY, item.ComputedWidth, item.ComputedHeight };
            }

            if (firstLayout == null)
            {
                firstLayout = layout;
            }
            else
            {
                for (int i = 0; i < ToggleCount; i++)
                {
                    AssertApprox(firstLayout[i][0], layout[i][0], $"Item {i} X at {h}p");
                    AssertApprox(firstLayout[i][1], layout[i][1], $"Item {i} Y at {h}p");
                    AssertApprox(firstLayout[i][2], layout[i][2], $"Item {i} W at {h}p");
                    AssertApprox(firstLayout[i][3], layout[i][3], $"Item {i} H at {h}p");
                }
            }
        }
    }

    // ══════════════════════════════════════════════
    //  Helper: build a UINode tree with scrollable toggle list
    // ══════════════════════════════════════════════

    /// <summary>
    /// Creates a UINode tree: root container with a scroll area containing 20 toggle items.
    /// Runs layout and applies scroll offset, setting up ScreenRect and ClipRect
    /// so hit testing works correctly.
    /// </summary>
    private static UINode BuildToggleList(float viewportW, float viewportH, float scrollOffset)
    {
        // Build layout tree
        var containerLayout = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            Width = 300,
            Height = viewportH,
        };
        var scrollAreaLayout = new LayoutNode
        {
            FlexDirection = FlexDirection.Column,
            FlexGrow = 1,
        };
        for (int i = 0; i < ToggleCount; i++)
            scrollAreaLayout.AddChild(new LayoutNode { Height = ToggleHeight, Width = 300 });

        containerLayout.AddChild(scrollAreaLayout);
        YogaLayout.Calculate(containerLayout, viewportW, viewportH);

        // Build UINode tree mirroring the layout
        var root = new UINode("div")
        {
            ComputedStyle = new ReactUI.Style.Style { Overflow = Overflow.Scroll },
            ScreenRect = new Rect(0, 0, 300, viewportH),
            ClipRect = new Rect(0, 0, 300, viewportH),
            ScrollOffsetY = scrollOffset,
        };

        var scrollArea = new UINode("div")
        {
            Parent = root,
            ComputedStyle = new ReactUI.Style.Style { Overflow = Overflow.Scroll },
            ScreenRect = new Rect(
                scrollAreaLayout.ComputedX,
                scrollAreaLayout.ComputedY,
                scrollAreaLayout.ComputedWidth,
                scrollAreaLayout.ComputedHeight),
            ClipRect = new Rect(0, 0, 300, viewportH),
        };
        root.Children.Add(scrollArea);

        for (int i = 0; i < ToggleCount; i++)
        {
            var itemLayout = scrollAreaLayout.Children[i];
            // Apply scroll offset to screen position (matches RenderPipeline behavior)
            var item = new UINode("div")
            {
                Key = $"toggle-{i}",
                Parent = scrollArea,
                ComputedStyle = new ReactUI.Style.Style(),
                ScreenRect = new Rect(
                    itemLayout.ComputedX,
                    itemLayout.ComputedY - scrollOffset,
                    itemLayout.ComputedWidth,
                    itemLayout.ComputedHeight),
                ClipRect = new Rect(0, 0, 300, viewportH),
            };
            scrollArea.Children.Add(item);
        }

        return root;
    }
}
