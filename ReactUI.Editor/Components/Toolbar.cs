using System;
using static ReactUI.UI;

namespace ReactUI.Editor.Components;

public static class Toolbar
{
    public static Core.VNode Render(string fileName, bool dirty, bool autoRun,
        Action onNew, Action onSave, Action onRun, Action onToggleAutoRun)
    {
        return Div(ClassName("toolbar"),
            Button("New", onNew, ClassName("toolbar-btn")),
            Button(dirty ? "Save *" : "Save", onSave, ClassName("toolbar-btn")),
            Button("Run", onRun, ClassName("toolbar-btn", new Style.Style
            {
                Background = Style.UIColor.FromHex("#7c3aed"),
                Color = Style.UIColor.FromHex("#fff"),
                Hover = new Style.Style { Background = Style.UIColor.FromHex("#6d28d9") },
            })),
            Button(autoRun ? "Auto: ON" : "Auto: OFF", onToggleAutoRun,
                ClassName(autoRun ? "toolbar-btn toolbar-btn-active" : "toolbar-btn")),
            Text(fileName + (dirty ? " *" : ""), ClassName("toolbar-title"))
        );
    }
}
