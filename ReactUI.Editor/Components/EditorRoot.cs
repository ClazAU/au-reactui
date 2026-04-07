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
/// Draggable, resizable, with zoom control.
/// </summary>
public static class EditorRoot
{
    private static readonly Func<Core.VNode> Render_ = Component(RenderEditor);
    public static Core.VNode Render() => Render_();

    private static string _watchDir = "";
    public static void SetWatchDirectory(string dir) => _watchDir = dir;

    internal const float MinWidth = 400;
    internal const float MinHeight = 300;
    private const float MinZoom = 0.6f;
    private const float MaxZoom = 2.0f;
    private const float FullscreenMargin = 30f;

    private static Core.VNode RenderEditor()
    {
        // Position and size state
        var (posX, setPosX) = UseState(60f);
        var (posY, setPosY) = UseState(40f);
        var (width, setWidth) = UseState(900f);
        var (height, setHeight) = UseState(600f);
        var (zoom, setZoom) = UseState(1.0f);

        // Register draggable for the title bar
        var ctx = HooksRuntime.Current;
        if (ctx != null)
        {
            var cx = posX; var cy = posY;
            ReactUI.Input.InputSystem.RegisterDraggable(
                ctx.ComponentId,
                () => cx, () => cy, setPosX, setPosY);
        }

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
            var fullPath = Path.Combine(_watchDir, openFile);
            EditorFileService.WriteFile(fullPath, string.Join("\n", lines));
            setDirty(false);
        };

        Action onRun = () => RunEval(lines, setError);

        Action onNew = () =>
        {
            var name = "NewComponent";
            var path = EditorFileService.CreateNewFile(_watchDir, name);
            setFiles(EditorFileService.ListFiles(_watchDir));
            onFileSelect(Path.GetRelativePath(_watchDir, path).Replace('\\', '/'));
        };

        // Zoom handlers
        Action zoomIn = () => setZoom(Math.Min(zoom + 0.1f, MaxZoom));
        Action zoomOut = () => setZoom(Math.Max(zoom - 0.1f, MinZoom));

        // Fullscreen toggle
        var (isFullscreen, setIsFullscreen) = UseState(false);
        Action toggleFullscreen = () =>
        {
            if (!isFullscreen)
            {
                setPosX(FullscreenMargin);
                setPosY(FullscreenMargin);
                setWidth(UnityEngine.Screen.width - FullscreenMargin * 2);
                setHeight(UnityEngine.Screen.height - FullscreenMargin * 2);
            }
            else
            {
                setPosX(60f);
                setPosY(40f);
                setWidth(900f);
                setHeight(600f);
            }
            setIsFullscreen(!isFullscreen);
        };

        var fileName = openFile != null ? Path.GetFileName(openFile) : "No file open";
        var zoomPct = (int)(zoom * 100);

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

            // Toolbar (drag handle)
            Div(new S
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
                Button(autoRun ? "Auto: ON" : "Auto: OFF", () => setAutoRun(!autoRun),
                    ClassName(autoRun ? "toolbar-btn toolbar-btn-active" : "toolbar-btn")),

                // Spacer
                Div(new S { FlexGrow = 1 }),

                // Zoom controls
                Button("-", zoomOut, ClassName("toolbar-btn")),
                Text($"{zoomPct}%", new S { FontSize = 11, Color = Style.UIColor.FromHex("#888"), Width = Style.StyleValue.Px(36), TextAlign = Style.TextAlign.Center }),
                Button("+", zoomIn, ClassName("toolbar-btn")),
                Button(isFullscreen ? "Exit FS" : "Fullscreen", toggleFullscreen, ClassName("toolbar-btn")),

                // Title
                Text(fileName + (dirty ? " *" : ""), new S
                {
                    FontSize = 12, FontWeight = 600,
                    Color = Style.UIColor.FromHex("#7c3aed"),
                    Padding = new Style.EdgeValues(0, 0, 0, 8),
                })
            ),

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

            // Status bar with resize info
            Div(ClassName("status-bar"),
                Text($"{lines.Length} lines", ClassName("status-text")),
                Text(openFile ?? "", ClassName("status-text")),
                Div(new S { FlexGrow = 1 }),
                Text($"{(int)width}x{(int)height}", ClassName("status-text"))
            ),

            // Resize handle (bottom-right corner)
            ResizeHandle(width, height, setWidth, setHeight)
        );
    }

    private static Core.VNode ResizeHandle(float width, float height, Action<float> setWidth, Action<float> setHeight)
    {
        // A small draggable corner indicator
        var node = Div(new S
        {
            Position = Style.PositionType.Absolute,
            Inset = new Style.EdgeValues(float.NaN, 0, 0, float.NaN),
            Width = Style.StyleValue.Px(16),
            Height = Style.StyleValue.Px(16),
            Cursor = Style.CursorType.Pointer,
            Opacity = 0.4f,
            Hover = new S { Opacity = 0.8f },
        },
            // Diagonal lines to indicate resize
            Text("⋱", new S { FontSize = 12, Color = Style.UIColor.FromHex("#888"), TextAlign = Style.TextAlign.Center })
        );
        node.Props["onMouseDown"] = (Action)(() =>
        {
            // Start resize tracking
            ResizeTracker.Start(width, height, setWidth, setHeight);
        });
        return node;
    }

    private static void RunEval(string[] lines, Action<string?> setError)
    {
        try
        {
            var source = string.Join("\n", lines);
            var transformed = Jsx.JsxTransformer.Transform(source);
            var prepared = Jsx.JintBridge.PrepareSource(transformed);
            setError(null);
        }
        catch (Exception ex)
        {
            setError(ex.Message);
        }
    }
}

/// <summary>
/// Tracks mouse delta for resize operations.
/// Called from Update via ReactUIBehaviour.OnUpdate.
/// </summary>
public static class ResizeTracker
{
    private static bool _active;
    private static float _startMouseX, _startMouseY;
    private static float _startWidth, _startHeight;
    private static Action<float>? _setWidth;
    private static Action<float>? _setHeight;

    public static void Start(float currentWidth, float currentHeight, Action<float> setWidth, Action<float> setHeight)
    {
        _active = true;
        _startMouseX = UnityEngine.Input.mousePosition.x;
        _startMouseY = UnityEngine.Screen.height - UnityEngine.Input.mousePosition.y;
        _startWidth = currentWidth;
        _startHeight = currentHeight;
        _setWidth = setWidth;
        _setHeight = setHeight;
    }

    /// <summary>Call from Update loop.</summary>
    public static void Tick()
    {
        if (!_active) return;

        if (!UnityEngine.Input.GetMouseButton(0))
        {
            _active = false;
            return;
        }

        float mouseX = UnityEngine.Input.mousePosition.x;
        float mouseY = UnityEngine.Screen.height - UnityEngine.Input.mousePosition.y;
        float dx = mouseX - _startMouseX;
        float dy = mouseY - _startMouseY;

        float newWidth = Math.Max(EditorRoot.MinWidth, _startWidth + dx);
        float newHeight = Math.Max(EditorRoot.MinHeight, _startHeight + dy);

        _setWidth?.Invoke(newWidth);
        _setHeight?.Invoke(newHeight);
    }
}
