using System;
using ReactUI.Core;
using ReactUI.Jsx;

namespace ReactUI.Editor.Services;

/// <summary>
/// Evaluates JSX source for the live preview pane.
/// Uses a separate JintBridge instance so preview doesn't interfere
/// with production-mounted components.
/// </summary>
public class JsxEvaluator
{
    private readonly JintBridge _bridge = new();
    private RenderHandle? _currentHandle;
    private static int _nextPreviewId = 900000;

    /// <summary>Last error message, or null if evaluation succeeded.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Evaluate JSX source and mount the result.
    /// Returns the render function, or null on error (check LastError).
    /// </summary>
    public Func<VNode>? Evaluate(string jsxSource)
    {
        try
        {
            LastError = null;

            var transformed = JsxTransformer.Transform(jsxSource);
            var prepared = JintBridge.PrepareSource(transformed);
            var componentId = _nextPreviewId++;

            var renderFunc = _bridge.Evaluate(prepared, componentId);
            if (renderFunc == null)
            {
                LastError = "No default export found. Add: export default YourComponent;";
                return null;
            }

            return renderFunc;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Mount the evaluated JSX in the preview. Disposes the previous preview.
    /// </summary>
    public RenderHandle? MountPreview(string jsxSource)
    {
        _currentHandle?.Dispose();
        _currentHandle = null;

        var renderFunc = Evaluate(jsxSource);
        if (renderFunc == null) return null;

        try
        {
            _currentHandle = Scheduler.Mount(renderFunc);
            return _currentHandle;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    /// <summary>Dispose the current preview.</summary>
    public void DisposePreview()
    {
        _currentHandle?.Dispose();
        _currentHandle = null;
    }
}
