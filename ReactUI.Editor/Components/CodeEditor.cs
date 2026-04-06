using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ReactUI.UI;
using S = ReactUI.Style.Style;

namespace ReactUI.Editor.Components;

/// <summary>
/// Multi-line code editor with syntax highlighting.
/// Each line: [line number] [highlighted overlay + transparent input]
/// </summary>
public static class CodeEditor
{
    private static readonly Func<(string[] lines, Action<string[]> onChange, SyntaxHighlighter.Language lang), Core.VNode> Render_ =
        Component<(string[] lines, Action<string[]> onChange, SyntaxHighlighter.Language lang)>(RenderInternal);

    public static Core.VNode Render(string[] lines, Action<string[]> onChange, SyntaxHighlighter.Language lang = SyntaxHighlighter.Language.Jsx) =>
        Render_((lines, onChange, lang));

    private static Core.VNode RenderInternal((string[] lines, Action<string[]> onChange, SyntaxHighlighter.Language lang) props)
    {
        var (lines, onChange, lang) = props;
        var (focusedLine, setFocusedLine) = UseState(0);

        if (focusedLine >= lines.Length) focusedLine = lines.Length - 1;
        if (focusedLine < 0) focusedLine = 0;

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

            var lineNumStyle = ClassName("line-number");

            // Build syntax-highlighted text spans
            var tokens = SyntaxHighlighter.Tokenize(lineText, lang);
            var highlightedSpans = new List<Core.VNode>();
            foreach (var token in tokens)
            {
                var color = SyntaxHighlighter.GetColor(token.Type);
                highlightedSpans.Add(Text(token.Text, new S
                {
                    FontSize = 13,
                    Color = color,
                }));
            }

            // The highlighted overlay (positioned absolute over the input)
            var overlay = Div(new S
            {
                Position = Style.PositionType.Absolute,
                Inset = new Style.EdgeValues(0),
                FlexDirection = Style.FlexDirection.Row,
                AlignItems = Style.AlignItems.Center,
                Padding = new Style.EdgeValues(2, 4),
                PointerEvents = false, // clicks pass through to input below
            }, highlightedSpans.ToArray());

            // The actual input (transparent text — editing happens here)
            var inputStyle = new S
            {
                FlexGrow = 1,
                FontSize = 13,
                Color = isFocused ? Style.UIColor.FromHex("#d4d4d4") : Style.UIColor.Transparent,
                Background = isFocused ? Style.UIColor.FromHex("#ffffff08") : Style.UIColor.Transparent,
                Padding = new Style.EdgeValues(2, 4),
                BorderWidth = 0,
            };

            // Container for the input + overlay stack
            var editArea = Div(new S
            {
                FlexGrow = 1,
                Position = Style.PositionType.Relative,
            },
                UI.Input(lineText, onLineChange, inputStyle),
                isFocused ? Div() : overlay  // Hide overlay when focused (show input caret instead)
            );

            // Attach onKeyDown to the input inside editArea
            if (editArea.Children != null && editArea.Children.Length > 0)
                editArea.Children[0].Props["onKeyDown"] = onKeyDown;

            var lineNode = Div(ClassName("code-line"),
                Text((lineIdx + 1).ToString(), lineNumStyle),
                editArea
            );
            lineNode.Key = $"editor-line-{lineIdx}";

            children[i] = lineNode;
        }

        return ScrollView(ClassName("code-scroll"), children);
    }
}
