using System;
using System.Linq;
using UnityEngine;
using static ReactUI.UI;
using S = ReactUI.Style.Style;

namespace ReactUI.Editor.Components;

/// <summary>
/// Multi-line code editor built from stacked single-line Inputs.
/// Supports Enter (new line), Backspace-at-start (merge lines), Arrow Up/Down (navigate).
/// </summary>
public static class CodeEditor
{
    private static readonly Func<(string[] lines, Action<string[]> onChange), Core.VNode> Render_ =
        Component<(string[] lines, Action<string[]> onChange)>(RenderInternal);

    public static Core.VNode Render(string[] lines, Action<string[]> onChange) =>
        Render_((lines, onChange));

    private static Core.VNode RenderInternal((string[] lines, Action<string[]> onChange) props)
    {
        var (lines, onChange) = props;
        var (focusedLine, setFocusedLine) = UseState(0);

        // Clamp focused line to valid range
        if (focusedLine >= lines.Length)
            focusedLine = lines.Length - 1;
        if (focusedLine < 0)
            focusedLine = 0;

        var children = new Core.VNode[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            var lineIdx = i;
            var lineText = lines[i];
            var isFocused = i == focusedLine;

            // Line change handler
            Action<string> onLineChange = (newText) =>
            {
                var newLines = (string[])lines.Clone();
                newLines[lineIdx] = newText;
                onChange(newLines);
            };

            // Key handler for Enter, Backspace-at-start, ArrowUp/Down
            Action<KeyCode> onKeyDown = (kc) =>
            {
                switch (kc)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                    {
                        // Insert new line below
                        var newLines = new string[lines.Length + 1];
                        for (int j = 0; j <= lineIdx; j++)
                            newLines[j] = lines[j];
                        newLines[lineIdx + 1] = "";
                        for (int j = lineIdx + 1; j < lines.Length; j++)
                            newLines[j + 1] = lines[j];
                        onChange(newLines);
                        setFocusedLine(lineIdx + 1);
                        // Focus the new input
                        global::ReactUI.Input.FocusManager.SetFocusByKey($"editor-line-{lineIdx + 1}");
                        break;
                    }

                    case KeyCode.Backspace when lineText.Length == 0 && lineIdx > 0:
                    {
                        // Merge empty line with previous
                        var newLines = lines.Where((_, idx) => idx != lineIdx).ToArray();
                        onChange(newLines);
                        setFocusedLine(lineIdx - 1);
                        global::ReactUI.Input.FocusManager.SetFocusByKey($"editor-line-{lineIdx - 1}");
                        break;
                    }

                    case KeyCode.UpArrow when lineIdx > 0:
                        setFocusedLine(lineIdx - 1);
                        global::ReactUI.Input.FocusManager.SetFocusByKey($"editor-line-{lineIdx - 1}");
                        break;

                    case KeyCode.DownArrow when lineIdx < lines.Length - 1:
                        setFocusedLine(lineIdx + 1);
                        global::ReactUI.Input.FocusManager.SetFocusByKey($"editor-line-{lineIdx + 1}");
                        break;
                }
            };

            // Build the line
            var lineNumStyle = ClassName("line-number");
            var inputStyle = isFocused
                ? ClassName("line-input").Merge(ClassName("line-input-focused"))
                : ClassName("line-input");

            var lineNode = Div(ClassName("code-line"),
                Text((lineIdx + 1).ToString(), lineNumStyle),
                UI.Input(lineText, onLineChange, inputStyle)
            );
            lineNode.Key = $"editor-line-{lineIdx}";

            // Attach onKeyDown to the input
            if (lineNode.Children != null && lineNode.Children.Length > 1)
                lineNode.Children[1].Props["onKeyDown"] = onKeyDown;

            children[i] = lineNode;
        }

        return ScrollView(ClassName("code-scroll"), children);
    }
}
