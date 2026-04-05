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
        var (tab, setTab) = UseState(0);
        var (posX, setPosX) = UseState(0f);
        var (posY, setPosY) = UseState(0f);

        if (Input.KeyToggle.Get(UnityEngine.KeyCode.F9)) return Div();

        // Register this component as draggable — InputSystem tracks drag every frame
        var capturedPosX = posX; var capturedPosY = posY;
        ReactUI.Input.InputSystem.RegisterDraggable(
            ReactUI.Hooks.HooksRuntime.Current.ComponentId,
            () => capturedPosX, () => capturedPosY, setPosX, setPosY);

        return Div(new S.Style
        {
            Position = S.PositionType.Absolute,
            Inset = new S.EdgeValues(posY, float.NaN, float.NaN, posX),
            Width = 420,
            // Height auto-sizes to content
            Background = "#1a1a2eE8",
            BorderRadius = 16,
            BorderColor = "#ffffff15",
            BorderWidth = 1,
            BoxShadow = new S.BoxShadow { Blur = 12, Color = "rgba(0,0,0,0.5)" },
            Padding = new S.EdgeValues(20),
            Cursor = S.CursorType.Pointer,
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
            FlexGrow = 1,
            MinWidth = S.StyleValue.Px(50),
            MinHeight = S.StyleValue.Px(50),
            Padding = new S.EdgeValues(6, 14),
            Background = active ? "#7c3aed" : "#2d2a33",
            Color = active ? "#ffffff" : "#a0a0a0",
            BorderRadius = 8,
            FontSize = 13,
            FontWeight = active ? 600 : 400,
            AlignItems = S.AlignItems.Center,
            JustifyContent = S.JustifyContent.Center,
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
        // Animate gradient angle — read Time.time directly (no state needed)
        var angle = UnityEngine.Time.time * 90f % 360f;

        // Schedule re-render next frame to keep animation running
        Core.Scheduler.ScheduleRender(Hooks.HooksRuntime.Current.ComponentId);

        return Div(new S.Style { Gap = 12 },
            Div(new S.Style
            {
                BackgroundGradient = new S.Gradient
                {
                    Type = S.GradientType.Linear,
                    Angle = angle,
                    ColorA = "#667eea",
                    ColorB = "#764ba2",
                },
                BorderRadii = new S.CornerRadius(12, 12, 12, 0),
                Padding = new S.EdgeValues(20),
                BoxShadow = new S.BoxShadow {Blur = 8, Spread = 2, Color = "rgba(118,75,162,0.5)" },
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
                    // Scale: spread shadow creates "grow" illusion on hover
                    Div(new S.Style
                    {
                        FlexGrow = 1, Background = "#7c3aed", BorderRadius = 8,
                        Padding = new S.EdgeValues(16), AlignItems = S.AlignItems.Center,
                        Cursor = S.CursorType.Pointer, Opacity = 0.8f,
                        Hover = new S.Style { Opacity = 1f, BoxShadow = new S.BoxShadow { Blur = 0, Spread = 6, Color = "#7c3aed" } },
                        Transitions = new[] { new S.Transition { Property = "all", Duration = 0.2f, Easing = S.EasingType.Ease } },
                    }, Text("Scale", new S.Style { FontSize = 13, FontWeight = 600, Color = "#fff" })),
                    // Glow: colored shadow appears on hover
                    Div(new S.Style
                    {
                        FlexGrow = 1, Background = "#3b82f6", BorderRadius = 8,
                        Padding = new S.EdgeValues(16), AlignItems = S.AlignItems.Center,
                        Cursor = S.CursorType.Pointer, Opacity = 0.8f,
                        Hover = new S.Style { Opacity = 1f, BoxShadow = new S.BoxShadow { Blur = 20, Spread = 4, Color = "rgba(59,130,246,0.6)" } },
                        Transitions = new[] { new S.Transition { Property = "all", Duration = 0.2f, Easing = S.EasingType.Ease } },
                    }, Text("Glow", new S.Style { FontSize = 13, FontWeight = 600, Color = "#fff" })),
                    // Dim: fades out on hover
                    Div(new S.Style
                    {
                        FlexGrow = 1, Background = "#22c55e", BorderRadius = 8,
                        Padding = new S.EdgeValues(16), AlignItems = S.AlignItems.Center,
                        Cursor = S.CursorType.Pointer, Opacity = 1f,
                        Hover = new S.Style { Opacity = 0.4f },
                        Transitions = new[] { new S.Transition { Property = "opacity", Duration = 0.2f, Easing = S.EasingType.Ease } },
                    }, Text("Dim", new S.Style { FontSize = 13, FontWeight = 600, Color = "#fff" }))
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
            Cursor = S.CursorType.Pointer,
            Opacity = 0.85f,
            Hover = new S.Style { Opacity = 1f, BorderWidth = 2, BorderColor = "#ffffff40" },
            Transitions = new[] { new S.Transition { Property = "all", Duration = 0.15f, Easing = S.EasingType.Ease } },
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
            Opacity = 0.6f,
            Cursor = S.CursorType.Pointer,
            Hover = new S.Style
            {
                Opacity = 1f,
                BoxShadow = new S.BoxShadow { Blur = 16, Spread = 2, Color = color },
            },
            Transitions = new[] { new S.Transition { Property = "all", Duration = 0.2f, Easing = S.EasingType.Ease } },
        },
            Text(label, new S.Style { FontSize = 13, FontWeight = 600, Color = "#fff" })
        );
    }

    // ── Tab 3: Dynamic List ───────────────────────────────────────

    private static readonly Func<VNode> ListTab_ = Component(RenderListTab);
    private static VNode ListTab() => ListTab_();

    private static VNode RenderListTab()
    {
        var (items, setItems) = UseState(new[] { (id: 1, name: "Item 1"), (id: 2, name: "Item 2"), (id: 3, name: "Item 3") });
        var (nextId, setNextId) = UseState(4);

        return Div(new S.Style { Gap = 12 },
            Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = 8 },
                Button("Add Item", () =>
                {
                    setItems(items.Append((id: nextId, name: $"Item {nextId}")).ToArray());
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
                        FlexShrink = 0,
                        Padding = new S.EdgeValues(8, 12),
                        Background = i % 2 == 0 ? "#1a1a2e" : "#242236",
                        BorderRadius = 6,
                        Hover = new S.Style { Background = "#3d3a43" },
                        Transitions = new[] { new S.Transition { Property = "background", Duration = 0.1f, Easing = S.EasingType.Ease } },
                    },
                        Text(item.name, new S.Style { FontSize = 14, Color = "#e0e0e0", FlexGrow = 1 }),
                        Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = 4 },
                        Button("+", () =>
                        {
                            var newItems = items.ToList();
                            newItems.Insert(i + 1, (id: nextId, name: item.name));
                            setItems(newItems.ToArray());
                            setNextId(nextId + 1);
                        }, new S.Style
                        {
                            Width = S.StyleValue.Px(28),
                            Height = S.StyleValue.Px(28),
                            Background = "#3b82f6",
                            BorderRadius = 6,
                            Color = "#fff",
                            FontSize = 13,
                            FontWeight = 700,
                            TextAlign = S.TextAlign.Center,
                            Padding = new S.EdgeValues(0),
                            Cursor = S.CursorType.Pointer,
                            Opacity = 0.7f,
                            Hover = new S.Style { Opacity = 1f, Background = "#2563eb" },
                        }),
                        Button("\u2715", () =>
                        {
                            setItems(items.Where((_, j) => j != i).ToArray());
                        }, new S.Style
                        {
                            Width = S.StyleValue.Px(28),
                            Height = S.StyleValue.Px(28),
                            Background = "#ef4444",
                            BorderRadius = 6,
                            Color = "#fff",
                            FontSize = 13,
                            FontWeight = 700,
                            AlignItems = S.AlignItems.Center,
                            JustifyContent = S.JustifyContent.Center,
                            TextAlign = S.TextAlign.Center,
                            Padding = new S.EdgeValues(0),
                            Cursor = S.CursorType.Pointer,
                            Opacity = 0.7f,
                            Hover = new S.Style { Opacity = 1f, Background = "#dc2626" },
                        })
                        ) // close button row Div
                    );
                    node.Key = item.id.ToString();
                    return node;
                }).ToArray()
            ),

            Text($"{items.Length} items", new S.Style { FontSize = 12, Color = "#6b7280" })
        );
    }
}
