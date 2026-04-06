using System;
using System.IO;
using System.Linq;
using ReactUI.Editor.Services;
using static ReactUI.UI;
using S = ReactUI.Style.Style;

namespace ReactUI.Editor.Components;

/// <summary>
/// Root component of the in-game JSX editor.
/// Layout: Toolbar | Sidebar + CodeEditor | StatusBar
/// </summary>
public static class EditorRoot
{
    private static readonly Func<Core.VNode> Render_ = Component(RenderEditor);
    public static Core.VNode Render() => Render_();

    private static string _watchDir = "";

    /// <summary>Set the directory to browse/edit JSX files from.</summary>
    public static void SetWatchDirectory(string dir) => _watchDir = dir;

    private static Core.VNode RenderEditor()
    {
        var (openFile, setOpenFile) = UseState<string?>(null);
        var (lines, setLines) = UseState(new[] { "" });
        var (files, setFiles) = UseState(Array.Empty<string>());
        var (error, setError) = UseState<string?>(null);
        var (autoRun, setAutoRun) = UseState(false);
        var (dirty, setDirty) = UseState(false);

        // Scan files on mount and when directory changes
        UseEffect(() =>
        {
            if (!string.IsNullOrEmpty(_watchDir))
                setFiles(EditorFileService.ListFiles(_watchDir));
        }, new object[] { _watchDir });

        // Open file handler
        Action<string> onFileSelect = (relPath) =>
        {
            var fullPath = Path.Combine(_watchDir, relPath);
            var content = EditorFileService.ReadFile(fullPath);
            setOpenFile(relPath);
            setLines(content.Split('\n'));
            setError(null);
            setDirty(false);
        };

        // Code change handler
        Action<string[]> onCodeChange = (newLines) =>
        {
            setLines(newLines);
            setDirty(true);
            if (autoRun)
                RunEval(newLines, setError);
        };

        // Save
        Action onSave = () =>
        {
            if (openFile == null) return;
            var fullPath = Path.Combine(_watchDir, openFile);
            EditorFileService.WriteFile(fullPath, string.Join("\n", lines));
            setDirty(false);
        };

        // Run
        Action onRun = () => RunEval(lines, setError);

        // New file
        Action onNew = () =>
        {
            var name = "NewComponent";
            var path = EditorFileService.CreateNewFile(_watchDir, name);
            setFiles(EditorFileService.ListFiles(_watchDir));
            onFileSelect(Path.GetRelativePath(_watchDir, path).Replace('\\', '/'));
        };

        var fileName = openFile != null ? Path.GetFileName(openFile) : "No file open";

        return Div(ClassName("editor-root"),
            // Toolbar
            Toolbar.Render(fileName, dirty, autoRun,
                onNew, onSave, onRun, () => setAutoRun(!autoRun)),

            // Main area: sidebar + editor
            Div(ClassName("main-area"),
                // Sidebar
                FileBrowser.Render(files, openFile, onFileSelect),

                // Editor area
                Div(ClassName("editor-area"),
                    // Code editor
                    CodeEditor.Render(lines, onCodeChange, SyntaxHighlighter.DetectLanguage(openFile)),

                    // Error panel
                    error != null
                        ? Div(ClassName("error-panel"),
                            Text(error, ClassName("error-text")))
                        : Div()
                )
            ),

            // Status bar
            Div(ClassName("status-bar"),
                Text($"{lines.Length} lines", ClassName("status-text")),
                Text(openFile ?? "", ClassName("status-text"))
            )
        );
    }

    private static void RunEval(string[] lines, Action<string?> setError)
    {
        try
        {
            var source = string.Join("\n", lines);
            var transformed = Jsx.JsxTransformer.Transform(source);
            var prepared = Jsx.JintBridge.PrepareSource(transformed);
            // Just validate — don't mount (preview is separate)
            setError(null);
        }
        catch (Exception ex)
        {
            setError(ex.Message);
        }
    }
}
