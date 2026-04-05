namespace ReactUI.Elements;

public static class GridElement
{
    public static Core.VNode Create(int columns, float cellWidth, float cellHeight, float gap, Style.Style? style, params Core.VNode[] children)
    {
        var s = style ?? new Style.Style();
        s.FlexDirection = Style.FlexDirection.Row;
        s.FlexWrap = Style.FlexWrap.Wrap;
        s.Gap = gap;

        // Set children widths/heights to cell dimensions
        foreach (var child in children)
        {
            child.Style ??= new Style.Style();
            if (cellWidth > 0) child.Style.Width = cellWidth;
            if (cellHeight > 0) child.Style.Height = cellHeight;
        }

        return new Core.VNode("div") { Style = s, Children = children };
    }
}
