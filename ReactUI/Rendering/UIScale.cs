namespace ReactUI.Rendering;

using UnityEngine;

/// <summary>
/// Global UI scale factor based on screen resolution.
/// All layout and rendering operates in logical pixels; the scale factor
/// maps them to physical screen pixels. Reference resolution is 1080p.
/// </summary>
public static class UIScale
{
    private const float ReferenceHeight = 1080f;

    /// <summary>
    /// Current scale factor. 1.0 at 1080p, higher at larger resolutions.
    /// </summary>
    public static float Factor { get; private set; } = 1f;

    /// <summary>
    /// Logical viewport width (screen width / scale factor).
    /// </summary>
    public static float LogicalWidth { get; private set; }

    /// <summary>
    /// Logical viewport height (screen height / scale factor).
    /// </summary>
    public static float LogicalHeight { get; private set; }

    /// <summary>
    /// Call once per frame to update the scale factor from current screen resolution.
    /// </summary>
    public static void Update()
    {
        Factor = Screen.height / ReferenceHeight;
        if (Factor < 0.5f) Factor = 0.5f; // clamp for very small windows
        LogicalWidth = Screen.width / Factor;
        LogicalHeight = Screen.height / Factor;
    }
}
