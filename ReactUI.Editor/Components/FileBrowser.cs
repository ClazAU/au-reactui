using System;
using System.IO;
using static ReactUI.UI;
using S = ReactUI.Style.Style;

namespace ReactUI.Editor.Components;

public static class FileBrowser
{
    public static Core.VNode Render(string[] files, string? selectedFile, Action<string> onFileSelect)
    {
        var children = new Core.VNode[files.Length + 1];

        children[0] = Text("FILES", ClassName("sidebar-title"));

        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var isSelected = file == selectedFile;
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var name = Path.GetFileName(file);

            var style = ClassName("file-item");
            if (isSelected)
                style = style.Merge(ClassName("file-item-active"));

            // Color code by file type
            var icon = ext == ".jsx" ? "JS " : "CSS";
            var iconStyle = ext == ".jsx" ? ClassName("file-item-jsx") : ClassName("file-item-css");

            var f = file; // capture for closure
            var node = Button(() => onFileSelect(f), style,
                Text(icon + " " + name, new S { FontSize = 12 })
            );
            node.Key = file;
            children[i + 1] = node;
        }

        return Div(ClassName("sidebar"), children);
    }
}
