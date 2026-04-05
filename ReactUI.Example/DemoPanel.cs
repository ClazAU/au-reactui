using System;
using System.Linq;
using ReactUI.Core;
using S = ReactUI.Style;
using static ReactUI.UI;

namespace ReactUI.Example;

/// <summary>
/// A demo panel showcasing ReactUI features: state, effects, styling, transitions.
/// Press F9 to toggle visibility.
/// </summary>
public static class DemoPanel
{
    // Toggle key
    private static readonly Func<VNode> Render_ = Component(RenderRoot);
    public static VNode Render() => Render_();

    private static VNode RenderRoot()
    {
        var (visible, setVisible) = UseState(true);
        var (tab, setTab) = UseState(0);

        // F9 toggle — check each frame via UseEffect with null deps (runs every render)
        UseEffect(() =>
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F9))
                setVisible(!visible);
        }, null);

        if (!visible) return Div(); // empty — hidden

        return Div(new S.Style
        {
            Position = S.PositionType.Absolute,
            Inset = new S.EdgeValues(80, float.NaN, float.NaN, 80),
            Width = 420,
            Background = "#1a1a2eE8",
            BorderRadius = 16,
            BorderColor = "#ffffff15",
            BorderWidth = 1,
            BoxShadow = new S.BoxShadow { OffsetY = 8, Blur = 32, Color = "rgba(0,0,0,0.5)" },
            Padding = new S.EdgeValues(20),
        },
            Header(tab, setTab),
            tab switch
            {
                0 => CounterTab(),
                1 => StyleShowcase(),
                2 => ListTab(),
                _ => Div()
            }
        );
    }

    // ── Header with tabs ──────────────────────────────────────────

    private static VNode Header(int tab, Action<int> setTab)
    {
        return Div(new S.Style
        {
            FlexDirection = S.FlexDirection.Column,
            Gap = 12,
            Margin = new S.EdgeValues(0, 0, 16, 0),
        },
            Text("ReactUI Demo", new S.Style
            {
                FontSize = 22,
                FontWeight = 700,
                Color = "#e0e0e0",
            }),
            Div(new S.Style
            {
                FlexDirection = S.FlexDirection.Row,
                Gap = 4,
            },
                TabButton("Counter", 0, tab, setTab),
                TabButton("Styles", 1, tab, setTab),
                TabButton("List", 2, tab, setTab)
            )
        );
    }

    private static VNode TabButton(string label, int index, int activeTab, Action<int> setTab)
    {
        bool active = index == activeTab;
        return Button(label, () => setTab(index), new S.Style
        {
            Padding = new S.EdgeValues(6, 14),
            Background = active ? "#7c3aed" : "#2d2a33",
            Color = active ? "#ffffff" : "#a0a0a0",
            BorderRadius = 8,
            FontSize = 13,
            FontWeight = active ? 600 : 400,
            Cursor = S.CursorType.Pointer,
            Hover = new S.Style { Background = active ? "#6d28d9" : "#3d3a43" },
            Transitions = new[] { new S.Transition { Property = "all", Duration = 0.15f, Easing = S.EasingType.Ease } },
        });
    }

    // ── Tab 1: Counter ────────────────────────────────────────────

    private static readonly Func<VNode> CounterTab_ = Component(RenderCounter);
    private static VNode CounterTab() => CounterTab_();

    private static VNode RenderCounter()
    {
        var (count, setCount) = UseState(0);
        var (name, setName) = UseState("");

        return Div(new S.Style { Gap = 12 },
            Div(new S.Style
            {
                Background = "#2d2a33",
                BorderRadius = 12,
                Padding = new S.EdgeValues(16),
                AlignItems = S.AlignItems.Center,
                Gap = 12,
            },
                Text($"Count: {count}", new S.Style
                {
                    FontSize = 32,
                    FontWeight = 700,
                    Color = count >= 0 ? "#7c3aed" : "#ef4444",
                }),
                Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = 8 },
                    ActionButton("- 5", () => setCount(count - 5), "#ef4444"),
                    ActionButton("- 1", () => setCount(count - 1), "#f97316"),
                    ActionButton("Reset", () => setCount(0), "#6b7280"),
                    ActionButton("+ 1", () => setCount(count + 1), "#22c55e"),
                    ActionButton("+ 5", () => setCount(count + 5), "#3b82f6")
                )
            ),

            Div(new S.Style
            {
                Background = "#2d2a33",
                BorderRadius = 12,
                Padding = new S.EdgeValues(16),
                Gap = 8,
            },
                Text("Text Input:", new S.Style { FontSize = 13, Color = "#a0a0a0" }),
                UI.Input(name, setName, new S.Style
                {
                    Padding = new S.EdgeValues(8, 12),
                    Background = "#1a1a2e",
                    BorderRadius = 8,
                    BorderWidth = 1,
                    BorderColor = "#444",
                    Color = "#e0e0e0",
                    FontSize = 14,
                    Focus = new S.Style { BorderColor = "#7c3aed" },
                    Transitions = new[] { new S.Transition { Property = "border-color", Duration = 0.15f, Easing = S.EasingType.Ease } },
                }, placeholder: "Type something..."),
                name.Length > 0
                    ? Text($"Hello, {name}!", new S.Style { FontSize = 14, Color = "#7c3aed" })
                    : Div()
            )
        );
    }

    private static VNode ActionButton(string label, Action onClick, string color)
    {
        return Button(label, onClick, new S.Style
        {
            Padding = new S.EdgeValues(6, 12),
            Background = color,
            Color = "#ffffff",
            BorderRadius = 6,
            FontSize = 13,
            FontWeight = 600,
            Cursor = S.CursorType.Pointer,
            Opacity = 0.9f,
            Hover = new S.Style { Opacity = 1f },
            Transitions = new[] { new S.Transition { Property = "opacity", Duration = 0.1f, Easing = S.EasingType.Ease } },
        });
    }

    // ── Tab 2: Style Showcase ─────────────────────────────────────

    private static readonly Func<VNode> StyleShowcase_ = Component(RenderStyleShowcase);
    private static VNode StyleShowcase() => StyleShowcase_();

    private static VNode RenderStyleShowcase()
    {
        return Div(new S.Style { Gap = 12 },
            Div(new S.Style
            {
                BackgroundGradient = new S.Gradient
                {
                    Type = S.GradientType.Linear,
                    Angle = 135,
                    ColorA = "#667eea",
                    ColorB = "#764ba2",
                },
                BorderRadius = 12,
                Padding = new S.EdgeValues(20),
                BoxShadow = new S.BoxShadow { OffsetY = 4, Blur = 16, Color = "rgba(102,126,234,0.3)" },
            },
                Text("Gradient Card", new S.Style { FontSize = 18, FontWeight = 700, Color = "#fff" }),
                Text("With box shadow and rounded corners", new S.Style { FontSize = 13, Color = "rgba(255,255,255,0.7)" })
            ),

            Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = 8 },
                ColorCard("Purple", "#7c3aed"),
                ColorCard("Blue", "#3b82f6"),
                ColorCard("Green", "#22c55e"),
                ColorCard("Orange", "#f97316")
            ),

            Div(new S.Style
            {
                Background = "#2d2a33",
                BorderRadius = 12,
                Padding = new S.EdgeValues(16),
                Gap = 8,
            },
                Text("Hover Effects", new S.Style { FontSize = 14, FontWeight = 600, Color = "#e0e0e0" }),
                Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = 8 },
                    HoverBox("Scale", "#7c3aed"),
                    HoverBox("Glow", "#3b82f6"),
                    HoverBox("Dim", "#22c55e")
                )
            )
        );
    }

    private static VNode ColorCard(string label, string color)
    {
        return Div(new S.Style
        {
            FlexGrow = 1,
            Background = color,
            BorderRadius = 8,
            Padding = new S.EdgeValues(12),
            AlignItems = S.AlignItems.Center,
        },
            Text(label, new S.Style { FontSize = 12, FontWeight = 600, Color = "#fff" })
        );
    }

    private static VNode HoverBox(string label, string color)
    {
        return Div(new S.Style
        {
            FlexGrow = 1,
            Background = color,
            BorderRadius = 8,
            Padding = new S.EdgeValues(16),
            AlignItems = S.AlignItems.Center,
            Opacity = 0.7f,
            Cursor = S.CursorType.Pointer,
            Hover = new S.Style { Opacity = 1f },
            Transitions = new[] { new S.Transition { Property = "opacity", Duration = 0.2f, Easing = S.EasingType.Ease } },
        },
            Text(label, new S.Style { FontSize = 13, FontWeight = 600, Color = "#fff" })
        );
    }

    // ── Tab 3: Dynamic List ───────────────────────────────────────

    private static readonly Func<VNode> ListTab_ = Component(RenderListTab);
    private static VNode ListTab() => ListTab_();

    private static VNode RenderListTab()
    {
        var (items, setItems) = UseState(new[] { "Item 1", "Item 2", "Item 3" });
        var (nextId, setNextId) = UseState(4);

        return Div(new S.Style { Gap = 12 },
            Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = 8 },
                Button("Add Item", () =>
                {
                    setItems(items.Append($"Item {nextId}").ToArray());
                    setNextId(nextId + 1);
                }, new S.Style
                {
                    Padding = new S.EdgeValues(6, 14),
                    Background = "#22c55e",
                    Color = "#fff",
                    BorderRadius = 6,
                    FontSize = 13,
                    Cursor = S.CursorType.Pointer,
                }),
                Button("Remove Last", () =>
                {
                    if (items.Length > 0)
                        setItems(items.Take(items.Length - 1).ToArray());
                }, new S.Style
                {
                    Padding = new S.EdgeValues(6, 14),
                    Background = "#ef4444",
                    Color = "#fff",
                    BorderRadius = 6,
                    FontSize = 13,
                    Cursor = S.CursorType.Pointer,
                }),
                Button("Shuffle", () =>
                {
                    var rng = new Random();
                    setItems(items.OrderBy(_ => rng.Next()).ToArray());
                }, new S.Style
                {
                    Padding = new S.EdgeValues(6, 14),
                    Background = "#3b82f6",
                    Color = "#fff",
                    BorderRadius = 6,
                    FontSize = 13,
                    Cursor = S.CursorType.Pointer,
                })
            ),

            ScrollView(new S.Style
            {
                MaxHeight = 200,
                Background = "#2d2a33",
                BorderRadius = 12,
                Padding = new S.EdgeValues(8),
                Gap = 4,
            },
                items.Select((item, i) =>
                {
                    var node = Div(new S.Style
                    {
                        FlexDirection = S.FlexDirection.Row,
                        JustifyContent = S.JustifyContent.SpaceBetween,
                        AlignItems = S.AlignItems.Center,
                        Padding = new S.EdgeValues(8, 12),
                        Background = i % 2 == 0 ? "#1a1a2e" : "#242236",
                        BorderRadius = 6,
                        Hover = new S.Style { Background = "#3d3a43" },
                        Transitions = new[] { new S.Transition { Property = "background", Duration = 0.1f, Easing = S.EasingType.Ease } },
                    },
                        Text(item, new S.Style { FontSize = 14, Color = "#e0e0e0" }),
                        Text($"#{i + 1}", new S.Style { FontSize = 12, Color = "#6b7280" })
                    );
                    node.Key = item;
                    return node;
                }).ToArray()
            ),

            Text($"{items.Length} items", new S.Style { FontSize = 12, Color = "#6b7280" })
        );
    }
}
