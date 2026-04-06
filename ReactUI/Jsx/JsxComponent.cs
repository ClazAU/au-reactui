using System;
using ReactUI.Core;

namespace ReactUI.Jsx;

/// <summary>
/// Represents a loaded JSX component with its associated state.
/// </summary>
public class JsxComponent
{
    /// <summary>Absolute file path to the .jsx file.</summary>
    public string FilePath { get; }

    /// <summary>Stable component ID derived from the file path. Used for hook context.</summary>
    public int ComponentId { get; }

    /// <summary>The render handle returned by Scheduler.Mount.</summary>
    public RenderHandle? Handle { get; set; }

    /// <summary>Cached transformed JS source (JSX → plain JS).</summary>
    public string? TransformedSource { get; set; }

    /// <summary>Hash of the state hook declarations for mismatch detection on hot reload.</summary>
    public int HookHash { get; set; }

    /// <summary>Associated CSS file path (if any).</summary>
    public string? CssFilePath { get; set; }

    /// <summary>Parsed stylesheet from the associated CSS file.</summary>
    public Style.StyleSheet? StyleSheet { get; set; }

    public JsxComponent(string filePath)
    {
        FilePath = filePath;
        ComponentId = filePath.GetHashCode();
    }
}
