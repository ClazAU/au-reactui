using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ReactUI.UI;
using S = ReactUI.Style.Style;

namespace ReactUI.Editor.Components;

/// <summary>
/// Multi-line code editor with syntax highlighting on all lines.
/// Uses transparent Input + absolute overlay with PointerEvents=false.
/// </summary>
public static class CodeEditor
{
    private static readonly Func<(string[] lines, Action<string[]> onChange, SyntaxHighlighter.Language lang, float zoom), Core.VNode> Render_ =
        Component<(string[] lines, Action<string[]> onChange, SyntaxHighlighter.Language lang, float zoom)>(RenderInternal);

    public static Core.VNode Render(string[] lines, Action<string[]> onChange, SyntaxHighlighter.Language lang = SyntaxHighlighter.Language.Jsx, float zoom = 1f) =>
        Render_((lines, onChange, lang, zoom));

    private static Core.VNode RenderInternal((string[] lines, Action<string[]> onChange, SyntaxHighlighter.Language lang, float zoom) props)
    {
        var (lines, onChange, lang, zoom) = props;
        var (focusedLine, setFocusedLine) = UseState(-1);
        float fontSize = 13 * zoom;
        float lineHeight = 22 * zoom;
        float lineNumWidth = 40 * zoom;

        if (focusedLine >= lines.Length) focusedLine = lines.Length - 1;

        var children = new Core.VNode[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            var lineIdx = i;
            var lineText = lines[i];
            var isFocused = i == focusedLine;

            Action<string> onLineChange = (newText) =>
            {
                var newLines = (string[])lines.Clone();
                newLines[lineIdx] = newText;
                onChange(newLines);
            };

            Action<KeyCode> onKeyDown = (kc) =>
            {
                switch (kc)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                    {
                        var newLines = new string[lines.Length + 1];
                        for (int j = 0; j <= lineIdx; j++)
                            newLines[j] = lines[j];
                        newLines[lineIdx + 1] = "";
                        for (int j = lineIdx + 1; j < lines.Length; j++)
                            newLines[j + 1] = lines[j];
                        onChange(newLines);
                        setFocusedLine(lineIdx + 1);
                        global::ReactUI.Input.FocusManager.SetFocusByKey($"editor-line-{lineIdx + 1}");
                        break;
                    }
                    case KeyCode.Backspace when lineText.Length == 0 && lineIdx > 0:
                    {
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

            // Line number
            var lineNumNode = Text((lineIdx + 1).ToString(), new S
            {
                Width = Style.StyleValue.Px(lineNumWidth),
                FlexShrink = 0,
                TextAlign = Style.TextAlign.Right,
                FontSize = fontSize,
                Color = Style.UIColor.FromHex(isFocused ? "#888" : "#444"),
                Padding = new Style.EdgeValues(0, 8 * zoom, 0, 0),
            });

            // Syntax highlighted spans
            var tokens = SyntaxHighlighter.Tokenize(lineText, lang);
            var spans = new List<Core.VNode>();
            foreach (var token in tokens)
            {
                spans.Add(Text(token.Text, new S
                {
                    FontSize = fontSize,
                    Color = SyntaxHighlighter.GetColor(token.Type),
                }));
            }

            // Overlay: syntax colored text (pointer-events:none so clicks reach input)
            var overlay = Div(new S
            {
                Position = Style.PositionType.Absolute,
                Inset = new Style.EdgeValues(0),
                FlexDirection = Style.FlexDirection.Row,
                AlignItems = Style.AlignItems.Center,
                JustifyContent = Style.JustifyContent.FlexStart,
                Padding = new Style.EdgeValues(2 * zoom, 4 * zoom),
                PointerEvents = false,
            }, spans.ToArray());

            // Input: transparent text, handles keyboard input
            var inputNode = UI.Input(lineText, onLineChange, new S
            {
                FlexGrow = 1,
                FlexShrink = 0,
                FontSize = fontSize,
                Color = Style.UIColor.Transparent,
                Background = isFocused ? Style.UIColor.FromHex("#ffffff08") : Style.UIColor.Transparent,
                Padding = new Style.EdgeValues(2 * zoom, 4 * zoom),
                BorderWidth = 0,
                TextAlign = Style.TextAlign.Left,
            });
            inputNode.Props["onKeyDown"] = onKeyDown;

            // Container: input underneath, overlay on top
            var editArea = Div(new S
            {
                FlexGrow = 1,
                FlexShrink = 0,
                Position = Style.PositionType.Relative,
                Cursor = Style.CursorType.Text,
            },
                inputNode,
                overlay
            );

            // Click unfocused line to focus it
            if (!isFocused)
            {
                var capturedIdx = lineIdx;
                editArea.Props["onClick"] = (Action)(() =>
                {
                    setFocusedLine(capturedIdx);
                    global::ReactUI.Input.FocusManager.SetFocusByKey($"editor-line-{capturedIdx}");
                });
            }

            // Min width from content length
            float charWidth = fontSize * 0.6f;
            float lineMinWidth = lineNumWidth + (lineText.Length * charWidth) + 20;
            if (lineMinWidth < 300) lineMinWidth = 300;

            var lineNode = Div(new S
            {
                FlexDirection = Style.FlexDirection.Row,
                AlignItems = Style.AlignItems.Center,
                MinHeight = Style.StyleValue.Px(lineHeight),
                MinWidth = Style.StyleValue.Px(lineMinWidth),
                FlexShrink = 0,
            },
                lineNumNode,
                editArea
            );
            lineNode.Key = $"editor-line-{lineIdx}";

            children[i] = lineNode;
        }

        return ScrollView(ClassName("code-scroll"), children);
    }
}
