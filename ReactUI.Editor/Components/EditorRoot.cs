using System;
using System.IO;
using System.Linq;
using ReactUI.Editor.Services;
using ReactUI.Hooks;
using static ReactUI.UI;
using S = ReactUI.Style.Style;

namespace ReactUI.Editor.Components;

/// <summary>
/// Root component of the in-game JSX editor.
/// Draggable from toolbar, resizable from bottom-right corner, with zoom.
/// </summary>
public static class EditorRoot
{
    private static readonly Func<Core.VNode> Render_ = Component(RenderEditor);
    public static Core.VNode Render() => Render_();

    private static string _watchDir = "";
    private static bool _visible = true;

    public static void SetWatchDirectory(string dir) => _watchDir = dir;
    public static void ToggleVisible()
    {
        _visible = !_visible;
        Core.Scheduler.ScheduleRenderAll();
    }

    internal const float MinWidth = 400;
    internal const float MinHeight = 300;
    private const float MinZoom = 0.6f;
    private const float MaxZoom = 2.0f;
    private const float FullscreenMargin = 30f;

    private static Core.VNode RenderEditor()
    {
        var (posX, setPosX) = UseState(60f);
        var (posY, setPosY) = UseState(40f);
        var (width, setWidth) = UseState(900f);
        var (height, setHeight) = UseState(600f);
        var (zoom, setZoom) = UseState(1.0f);
        var (isFullscreen, setIsFullscreen) = UseState(false);

        // Saved windowed position/size for fullscreen restore
        var (savedX, setSavedX) = UseState(60f);
        var (savedY, setSavedY) = UseState(40f);
        var (savedW, setSavedW) = UseState(900f);
        var (savedH, setSavedH) = UseState(600f);

        // File/editor state
        var (openFile, setOpenFile) = UseState<string?>(null);
        var (lines, setLines) = UseState(new[] { "" });
        var (files, setFiles) = UseState(Array.Empty<string>());
        var (error, setError) = UseState<string?>(null);
        var (autoRun, setAutoRun) = UseState(false);
        var (dirty, setDirty) = UseState(false);

        UseEffect(() =>
        {
            if (!string.IsNullOrEmpty(_watchDir))
                setFiles(EditorFileService.ListFiles(_watchDir));
        }, new object[] { _watchDir });

        Action<string> onFileSelect = (relPath) =>
        {
            var fullPath = Path.Combine(_watchDir, relPath);
            var content = EditorFileService.ReadFile(fullPath);
            setOpenFile(relPath);
            setLines(content.Split('\n'));
            setError(null);
            setDirty(false);
        };

        Action<string[]> onCodeChange = (newLines) =>
        {
            setLines(newLines);
            setDirty(true);
            if (autoRun) RunEval(newLines, setError);
        };

        Action onSave = () =>
        {
            if (openFile == null) return;
            EditorFileService.WriteFile(Path.Combine(_watchDir, openFile), string.Join("\n", lines));
            setDirty(false);
        };

        Action onRun = () => RunEval(lines, setError);

        Action onNew = () =>
        {
            var path = EditorFileService.CreateNewFile(_watchDir, "NewComponent");
            setFiles(EditorFileService.ListFiles(_watchDir));
            onFileSelect(Path.GetRelativePath(_watchDir, path).Replace('\\', '/'));
        };

        Action zoomIn = () => setZoom(Math.Min(zoom + 0.1f, MaxZoom));
        Action zoomOut = () => setZoom(Math.Max(zoom - 0.1f, MinZoom));

        Action toggleFullscreen = () =>
        {
            if (!isFullscreen)
            {
                // Save current windowed state
                setSavedX(posX); setSavedY(posY);
                setSavedW(width); setSavedH(height);
                // Go fullscreen
                setPosX(FullscreenMargin);
                setPosY(FullscreenMargin);
                setWidth(ReactUI.Rendering.UIScale.LogicalWidth - FullscreenMargin * 2);
                setHeight(ReactUI.Rendering.UIScale.LogicalHeight - FullscreenMargin * 2);
            }
            else
            {
                // Restore saved windowed state
                setPosX(savedX); setPosY(savedY);
                setWidth(savedW); setHeight(savedH);
            }
            setIsFullscreen(!isFullscreen);
        };

        // Hidden — all hooks called above, return empty div
        if (!_visible) return Div();

        var fileName = openFile != null ? Path.GetFileName(openFile) : "No file open";

        return Div(new S
            {
                Position = Style.PositionType.Absolute,
                Inset = new Style.EdgeValues(posY, float.NaN, float.NaN, posX),
                Width = Style.StyleValue.Px(width),
                Height = Style.StyleValue.Px(height),
                Background = Style.UIColor.FromHex("#0d0d14"),
                BorderRadius = 8,
                BorderWidth = 1,
                BorderColor = Style.UIColor.FromHex("#ffffff10"),
                BoxShadow = new Style.BoxShadow { Blur = 32, Color = Style.UIColor.FromRgba("rgba(0,0,0,0.6)") },
                Overflow = Style.Overflow.Hidden,
            },

            // Toolbar — this is the drag handle
            DragBar(fileName, dirty, autoRun, zoom, isFullscreen,
                onNew, onSave, onRun, () => setAutoRun(!autoRun),
                zoomOut, zoomIn, toggleFullscreen,
                posX, posY, setPosX, setPosY),

            // Main area
            Div(ClassName("main-area"),
                FileBrowser.Render(files, openFile, onFileSelect),
                Div(ClassName("editor-area"),
                    CodeEditor.Render(lines, onCodeChange, SyntaxHighlighter.DetectLanguage(openFile), zoom),
                    error != null
                        ? Div(ClassName("error-panel"), Text(error, ClassName("error-text")))
                        : Div()
                )
            ),

            // Status bar
            Div(ClassName("status-bar"),
                Text($"{lines.Length} lines", ClassName("status-text")),
                Text(openFile ?? "", ClassName("status-text")),
                Div(new S { FlexGrow = 1 }),
                Text($"{(int)width}x{(int)height}", ClassName("status-text"))
            ),

            // Resize handle (bottom-right corner)
            ResizeCorner(width, height, setWidth, setHeight)
        );
    }

    /// <summary>Toolbar that doubles as a drag handle.</summary>
    private static Core.VNode DragBar(
        string fileName, bool dirty, bool autoRun, float zoom, bool isFullscreen,
        Action onNew, Action onSave, Action onRun, Action onToggleAutoRun,
        Action zoomOut, Action zoomIn, Action toggleFullscreen,
        float posX, float posY, Action<float> setPosX, Action<float> setPosY)
    {
        // Register this element as a drag handle
        var cx = posX; var cy = posY;
        ReactUI.Input.InputSystem.RegisterDragHandle(
            "editor-toolbar", () => cx, () => cy, setPosX, setPosY);

        var node = Div(new S
            {
                FlexDirection = Style.FlexDirection.Row,
                Padding = new Style.EdgeValues(6, 12),
                Gap = 6,
                Background = Style.UIColor.FromHex("#16161e"),
                AlignItems = Style.AlignItems.Center,
                Cursor = Style.CursorType.Grab,
            },
            Button("New", onNew, ClassName("toolbar-btn")),
            Button(dirty ? "Save *" : "Save", onSave, ClassName("toolbar-btn")),
            Button("Run", onRun, ClassName("toolbar-btn toolbar-btn-primary")),
            Button(autoRun ? "Auto: ON" : "Auto: OFF", onToggleAutoRun,
                ClassName(autoRun ? "toolbar-btn toolbar-btn-active" : "toolbar-btn")),
            Div(new S { FlexGrow = 1 }),
            Button("-", zoomOut, ClassName("toolbar-btn")),
            Text($"{(int)(zoom * 100)}%", new S { FontSize = 11, Color = Style.UIColor.FromHex("#888"), Width = Style.StyleValue.Px(36), TextAlign = Style.TextAlign.Center }),
            Button("+", zoomIn, ClassName("toolbar-btn")),
            Button(isFullscreen ? "Exit FS" : "Fullscreen", toggleFullscreen, ClassName("toolbar-btn")),
            Text(fileName + (dirty ? " *" : ""), new S
            {
                FontSize = 12, FontWeight = 600,
                Color = Style.UIColor.FromHex("#7c3aed"),
                Padding = new Style.EdgeValues(0, 0, 0, 8),
            })
        );
        node.Key = "editor-toolbar";

        return node;
    }

    private static Core.VNode ResizeCorner(float width, float height, Action<float> setWidth, Action<float> setHeight)
    {
        var node = Div(new S
        {
            Position = Style.PositionType.Absolute,
            Inset = new Style.EdgeValues(float.NaN, 0, 0, float.NaN),
            Width = Style.StyleValue.Px(18),
            Height = Style.StyleValue.Px(18),
            Cursor = Style.CursorType.Pointer,
            Opacity = 0.4f,
            Hover = new S { Opacity = 0.8f },
        },
            Text("\u22f1", new S { FontSize = 12, Color = Style.UIColor.FromHex("#888"), TextAlign = Style.TextAlign.Center })
        );

        var cw = width; var ch = height;
        node.Props["onMouseDown"] = (Action)(() =>
        {
            ResizeTracker.Start(cw, ch, setWidth, setHeight);
        });

        return node;
    }

    private static void RunEval(string[] lines, Action<string?> setError)
    {
        try
        {
            var source = string.Join("\n", lines);
            Jsx.JsxTransformer.Transform(source);
            setError(null);
        }
        catch (Exception ex) { setError(ex.Message); }
    }
}

/// <summary>
/// Resize tracker for the editor window.
/// </summary>
public static class ResizeTracker
{
    private static bool _active;
    private static float _startMouseX, _startMouseY;
    private static float _startW, _startH;
    private static Action<float>? _setW;
    private static Action<float>? _setH;

    public static void Start(float currentW, float currentH, Action<float> setW, Action<float> setH)
    {
        _active = true;
        float s = ReactUI.Rendering.UIScale.Factor;
        _startMouseX = UnityEngine.Input.mousePosition.x / s;
        _startMouseY = (UnityEngine.Screen.height - UnityEngine.Input.mousePosition.y) / s;
        _startW = currentW;
        _startH = currentH;
        _setW = setW;
        _setH = setH;
    }

    public static void Tick()
    {
        if (!_active) return;
        if (!UnityEngine.Input.GetMouseButton(0)) { _active = false; return; }

        float s = ReactUI.Rendering.UIScale.Factor;
        float dx = UnityEngine.Input.mousePosition.x / s - _startMouseX;
        float dy = (UnityEngine.Screen.height - UnityEngine.Input.mousePosition.y) / s - _startMouseY;

        _setW?.Invoke(Math.Max(EditorRoot.MinWidth, _startW + dx));
        _setH?.Invoke(Math.Max(EditorRoot.MinHeight, _startH + dy));
    }
}
