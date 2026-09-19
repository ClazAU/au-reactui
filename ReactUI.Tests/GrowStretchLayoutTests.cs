using ReactUI.Layout;
using Xunit;

namespace ReactUI.Tests;

/// <summary>
/// A row whose height comes from flex-grow in a column parent. Its children stretch on the cross axis and have
/// to stop at the row's bottom padding, the same as when the row's height is set explicitly.
/// </summary>
public class GrowStretchLayoutTests
{
    private const float Tolerance = 0.01f;

    private static (LayoutNode Row, LayoutNode Card) Build(LayoutNode row)
    {
        var board = new LayoutNode { Width = 400, Height = 300 };
        board.AddChild(new LayoutNode { Height = 40 });
        board.AddChild(row);

        var card = new LayoutNode { FlexGrow = 1 };
        card.AddChild(new LayoutNode { Height = 50 });
        row.AddChild(card);

        YogaLayout.Calculate(board, 800, 600);
        return (row, card);
    }

    private static LayoutNode PaddedRow() => new()
    {
        FlexDirection = FlexDirection.Row,
        PaddingTop = 20, PaddingBottom = 20, PaddingLeft = 20, PaddingRight = 20,
    };

    [Fact]
    public void GrownRow_TakesTheRemainingHeight()
    {
        var row = PaddedRow();
        row.FlexGrow = 1;
        var (grown, _) = Build(row);

        Assert.InRange(grown.ComputedHeight, 260 - Tolerance, 260 + Tolerance);
    }

    [Fact]
    public void ChildOfGrownRow_StretchesToTheInnerHeight()
    {
        var row = PaddedRow();
        row.FlexGrow = 1;
        var (grown, card) = Build(row);

        Assert.InRange(card.ComputedHeight, 220 - Tolerance, 220 + Tolerance);
        Assert.True(card.ComputedY + card.ComputedHeight <= grown.ComputedY + grown.ComputedHeight - 20 + Tolerance,
            $"card bottom {card.ComputedY + card.ComputedHeight} passes the row's bottom padding at {grown.ComputedY + grown.ComputedHeight - 20}");
    }

    [Fact]
    public void ChildOfExplicitHeightRow_MatchesTheGrownCase()
    {
        var row = PaddedRow();
        row.Height = 260;
        var (_, card) = Build(row);

        Assert.InRange(card.ComputedHeight, 220 - Tolerance, 220 + Tolerance);
    }

    // The Apothecary board: padding and a gap on the column, a fixed title above the growing row, a hint below it,
    // and a fixed-width shelf beside cards that share the rest of the row.
    [Fact]
    public void GrownRowBetweenSiblingsWithGap_StaysInsideTheColumn()
    {
        var board = new LayoutNode { Width = 400, Height = 300, PaddingTop = 10, PaddingBottom = 10, PaddingLeft = 10, PaddingRight = 10, Gap = 14 };
        var title = new LayoutNode { Height = 40, AlignSelf = AlignSelf.Center, Width = 100 };
        var row = PaddedRow();
        row.FlexGrow = 1;
        row.Gap = 8;
        var hint = new LayoutNode { Height = 20 };
        board.AddChild(title);
        board.AddChild(row);
        board.AddChild(hint);

        var shelf = new LayoutNode { Width = 80 };
        var cards = new LayoutNode { FlexDirection = FlexDirection.Row, FlexGrow = 1, Gap = 8 };
        row.AddChild(shelf);
        row.AddChild(cards);
        var card = new LayoutNode { FlexGrow = 1, FlexBasis = 0 };
        card.AddChild(new LayoutNode { Height = 50 });
        card.AddChild(new LayoutNode { FlexGrow = 1 });
        card.AddChild(new LayoutNode { Height = 30 });
        cards.AddChild(card);

        YogaLayout.Calculate(board, 800, 600);

        // 300 - 20 padding - 40 title - 20 hint - 2 gaps of 14 = 192
        Assert.InRange(row.ComputedHeight, 192 - Tolerance, 192 + Tolerance);
        Assert.InRange(hint.ComputedY + hint.ComputedHeight, 290 - Tolerance, 290 + Tolerance);
        Assert.InRange(shelf.ComputedHeight, 152 - Tolerance, 152 + Tolerance);
        Assert.InRange(cards.ComputedHeight, 152 - Tolerance, 152 + Tolerance);
        Assert.InRange(card.ComputedHeight, 152 - Tolerance, 152 + Tolerance);
    }
}