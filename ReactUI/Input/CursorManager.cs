namespace ReactUI.Input;

/// <summary>
/// Sets the system cursor based on the hovered element's Cursor style.
/// Unity's cursor API is limited under IL2CPP, so this provides a best-effort mapping.
/// </summary>
public static class CursorManager
{
    private static Style.CursorType _current = Style.CursorType.Default;

    public static void Update(Core.UINode? hovered)
    {
        var desired = Style.CursorType.Default;

        if (hovered?.ComputedStyle?.Cursor != null)
            desired = hovered.ComputedStyle.Cursor.Value;

        if (desired == _current) return;
        _current = desired;

        // Unity does not expose a rich cursor API in IL2CPP builds.
        // We set visibility (None hides cursor) and use the default for everything else.
        // Custom cursor textures could be assigned via UnityEngine.Cursor.SetCursor if needed.
        switch (desired)
        {
            case Style.CursorType.None:
                UnityEngine.Cursor.visible = false;
                break;
            default:
                UnityEngine.Cursor.visible = true;
                break;
        }
    }
}
