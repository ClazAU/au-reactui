using System;
using System.Linq;
using ReactUI.Core;
using ReactUI.Theme;
using S = ReactUI.Style;
using static ReactUI.UI;

namespace ReactUI.Example;

/// <summary>
/// A demo panel showcasing ReactUI features (state, effects, layout, input, transitions) styled
/// entirely with the <see cref="Shadcn"/> theme, so it doubles as the theme's reference sheet.
/// Press F9 to toggle visibility.
/// </summary>
public static class DemoPanel
{
    private static readonly Func<VNode> Render_ = Component(RenderRoot);
    public static VNode Render() => Render_();

    private static readonly string[] TabNames = { "Counter", "Theme", "Styles", "List" };

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

        return Div(ClassName("card", new S.Style
        {
            Position = S.PositionType.Absolute,
            Inset = new S.EdgeValues(posY, float.NaN, float.NaN, posX),
            Width = 440,
            Cursor = S.CursorType.Grab,
        }),
            Header(tab, setTab),
            Div(ClassName("card-content"),
                tab switch
                {
                    0 => CounterTab(),
                    1 => ThemeTab(),
                    2 => StyleShowcase(),
                    3 => ListTab(),
                    _ => Div()
                }
            )
        );
    }

    // ── Header with tabs ──────────────────────────────────────────

    private static VNode Header(int tab, Action<int> setTab)
    {
        return Div(ClassName("card-header"),
            Text("ReactUI Demo", ClassName("card-title")),
            Text("shadcn theme reference — drag to move, F9 to hide", ClassName("card-description")),
            Div(new S.Style
            {
                FlexDirection = S.FlexDirection.Row,
                Gap = Shadcn.Space1,
                Margin = new S.EdgeValues(Shadcn.Space2, 0f, 0f, 0f),
            },
                TabNames.Select((name, i) => TabButton(name, i, tab, setTab)).ToArray()
            )
        );
    }

    private static VNode TabButton(string label, int index, int activeTab, Action<int> setTab)
    {
        bool active = index == activeTab;
        return Button(label, () => setTab(index), ClassName(
            active ? "btn btn-sm" : "btn btn-sm btn-ghost",
            new S.Style
            {
                FlexGrow = 1,
                Transitions = new[] { new S.Transition { Property = "all", Duration = 0.15f, Easing = S.EasingType.Ease } },
            }));
    }

    // ── Tab 1: Counter ────────────────────────────────────────────

    private static readonly Func<VNode> CounterTab_ = Component(RenderCounter);
    private static VNode CounterTab() => CounterTab_();

    private static VNode RenderCounter()
    {
        var (count, setCount) = UseState(0);
        var (name, setName) = UseState("");

        return Div(new S.Style { Gap = Shadcn.Space3 },
            Div(ClassName("popover", new S.Style { AlignItems = S.AlignItems.Center, Gap = Shadcn.Space3 }),
                Text($"Count: {count}", new S.Style
                {
                    FontSize = 32,
                    FontWeight = Shadcn.WeightSemibold,
                    Color = count >= 0 ? Shadcn.Foreground : Shadcn.Destructive,
                }),
                Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = Shadcn.Space2 },
                    Button("- 5", () => setCount(count - 5), ClassName("btn btn-sm btn-destructive")),
                    Button("- 1", () => setCount(count - 1), ClassName("btn btn-sm btn-outline")),
                    Button("Reset", () => setCount(0), ClassName("btn btn-sm btn-secondary")),
                    Button("+ 1", () => setCount(count + 1), ClassName("btn btn-sm btn-outline")),
                    Button("+ 5", () => setCount(count + 5), ClassName("btn btn-sm"))
                )
            ),

            Div(ClassName("popover"),
                Text("Your name", ClassName("label")),
                UI.Input(name, setName, ClassName("input", new S.Style
                {
                    Transitions = new[] { new S.Transition { Property = "border-color", Duration = 0.15f, Easing = S.EasingType.Ease } },
                }), placeholder: "Type something..."),
                name.Length > 0
                    ? Badge($"Hello, {name}!", destructive: false)
                    : Text("The greeting appears once you type.", ClassName("muted"))
            )
        );
    }

    // ── Tab 2: Theme gallery ──────────────────────────────────────

    private static readonly Func<VNode> ThemeTab_ = Component(RenderThemeTab);
    private static VNode ThemeTab() => ThemeTab_();

    private static VNode RenderThemeTab()
    {
        var (query, setQuery) = UseState("");

        return Div(new S.Style { Gap = Shadcn.Space3 },
            GallerySection("Buttons",
                Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = Shadcn.Space2 },
                    Button("Default", NoOp, ClassName("btn btn-sm")),
                    Button("Secondary", NoOp, ClassName("btn btn-sm btn-secondary")),
                    Button("Outline", NoOp, ClassName("btn btn-sm btn-outline")),
                    Button("Ghost", NoOp, ClassName("btn btn-sm btn-ghost"))
                ),
                Div(new S.Style { FlexDirection = S.FlexDirection.Row, AlignItems = S.AlignItems.Center, Gap = Shadcn.Space2 },
                    Button("Destructive", NoOp, ClassName("btn btn-sm btn-destructive")),
                    Button("Disabled", NoOp, ClassName("btn btn-sm btn-secondary btn-disabled")),
                    Button("✕", NoOp, ClassName("btn btn-icon btn-outline"))
                ),
                Button("Full-height default button", NoOp, ClassName("btn"))
            ),

            GallerySection("Form",
                Text("Label", ClassName("label")),
                UI.Input(query, setQuery, ClassName("input"), placeholder: "Search components..."),
                Text("Muted helper text sits under the field.", ClassName("muted"))
            ),

            GallerySection("Badges, separator, tokens",
                Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = Shadcn.Space2 },
                    Badge("Badge", destructive: false),
                    Badge("Destructive", destructive: true)
                ),
                Div(ClassName("separator")),
                Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = Shadcn.Space2 },
                    Swatch("Primary", Shadcn.Primary, Shadcn.PrimaryForeground),
                    Swatch("Secondary", Shadcn.Secondary, Shadcn.SecondaryForeground),
                    Swatch("Muted", Shadcn.Muted, Shadcn.MutedForeground),
                    Swatch("Destructive", Shadcn.Destructive, Shadcn.DestructiveForeground)
                )
            )
        );
    }

    private static void NoOp() { }

    private static VNode GallerySection(string title, params VNode[] children)
    {
        return Div(ClassName("popover"),
            Text(title, ClassName("label")),
            Div(new S.Style { Gap = Shadcn.Space2 }, children)
        );
    }

    private static VNode Badge(string label, bool destructive)
    {
        return Div(ClassName(destructive ? "badge badge-destructive" : "badge"),
            Text(label, new S.Style
            {
                FontSize = Shadcn.TextXs,
                FontWeight = Shadcn.WeightMedium,
                Color = destructive ? Shadcn.DestructiveForeground : Shadcn.SecondaryForeground,
            })
        );
    }

    private static VNode Swatch(string label, S.UIColor fill, S.UIColor ink)
    {
        return Div(new S.Style
        {
            FlexGrow = 1,
            Background = fill,
            BorderColor = Shadcn.Border,
            BorderWidth = 1f,
            BorderRadius = Shadcn.RadiusMd,
            Padding = new S.EdgeValues(Shadcn.Space2),
            AlignItems = S.AlignItems.Center,
        },
            Text(label, new S.Style { FontSize = Shadcn.TextXs, FontWeight = Shadcn.WeightMedium, Color = ink })
        );
    }

    // ── Tab 3: Style showcase — raw renderer capabilities ─────────

    private static readonly Func<VNode> StyleShowcase_ = Component(RenderStyleShowcase);
    private static VNode StyleShowcase() => StyleShowcase_();

    private static VNode RenderStyleShowcase()
    {
        // Animate gradient angle — read Time.time directly (no state needed)
        var angle = UnityEngine.Time.time * 90f % 360f;

        // Schedule re-render next frame to keep animation running
        Core.Scheduler.ScheduleRender(Hooks.HooksRuntime.Current.ComponentId);

        return Div(new S.Style { Gap = Shadcn.Space3 },
            Div(new S.Style
            {
                BackgroundGradient = new S.Gradient
                {
                    Type = S.GradientType.Linear,
                    Angle = angle,
                    ColorA = "#667eea",
                    ColorB = "#764ba2",
                },
                BorderRadii = new S.CornerRadius(Shadcn.RadiusLg, Shadcn.RadiusLg, Shadcn.RadiusLg, 0f),
                Padding = new S.EdgeValues(Shadcn.Space4),
                Gap = Shadcn.Space1,
                BoxShadow = new S.BoxShadow { Blur = 8, Spread = 2, Color = "rgba(118,75,162,0.5)" },
            },
                Text("Animated gradient", new S.Style { FontSize = Shadcn.TextLg, FontWeight = Shadcn.WeightSemibold, Color = "#fff" }),
                Text("Gradients, shadows and per-corner radii stay inline — the theme has no opinion here.",
                    new S.Style { FontSize = Shadcn.TextXs, Color = "rgba(255,255,255,0.75)" })
            ),

            Div(ClassName("popover"),
                Text("Hover effects", ClassName("label")),
                Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = Shadcn.Space2 },
                    HoverBox("Grow", "#7c3aed", new S.Style
                    {
                        Opacity = 1f,
                        BoxShadow = new S.BoxShadow { Blur = 0, Spread = 6, Color = "#7c3aed" },
                    }),
                    HoverBox("Glow", "#3b82f6", new S.Style
                    {
                        Opacity = 1f,
                        BoxShadow = new S.BoxShadow { Blur = 20, Spread = 4, Color = "rgba(59,130,246,0.6)" },
                    }),
                    HoverBox("Dim", "#22c55e", new S.Style { Opacity = 0.4f })
                )
            )
        );
    }

    private static VNode HoverBox(string label, string color, S.Style hover)
    {
        return Div(new S.Style
        {
            FlexGrow = 1,
            Background = color,
            BorderRadius = Shadcn.RadiusMd,
            Padding = new S.EdgeValues(Shadcn.Space4),
            AlignItems = S.AlignItems.Center,
            Cursor = S.CursorType.Pointer,
            Opacity = 0.8f,
            Hover = hover,
            Transitions = new[] { new S.Transition { Property = "all", Duration = 0.2f, Easing = S.EasingType.Ease } },
        },
            Text(label, new S.Style { FontSize = Shadcn.TextXs, FontWeight = Shadcn.WeightSemibold, Color = "#fff" })
        );
    }

    // ── Tab 4: Dynamic list ───────────────────────────────────────

    private static readonly Func<VNode> ListTab_ = Component(RenderListTab);
    private static VNode ListTab() => ListTab_();

    private static VNode RenderListTab()
    {
        var (items, setItems) = UseState(new[] { (id: 1, name: "Item 1"), (id: 2, name: "Item 2"), (id: 3, name: "Item 3") });
        var (nextId, setNextId) = UseState(4);

        return Div(new S.Style { Gap = Shadcn.Space3 },
            Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = Shadcn.Space2 },
                Button("Add item", () =>
                {
                    setItems(items.Append((id: nextId, name: $"Item {nextId}")).ToArray());
                    setNextId(nextId + 1);
                }, ClassName("btn btn-sm")),
                Button("Remove last", () =>
                {
                    if (items.Length > 0)
                        setItems(items.Take(items.Length - 1).ToArray());
                }, ClassName("btn btn-sm btn-outline")),
                Button("Shuffle", () =>
                {
                    var rng = new Random();
                    setItems(items.OrderBy(_ => rng.Next()).ToArray());
                }, ClassName("btn btn-sm btn-secondary"))
            ),

            ScrollView(ClassName("popover", new S.Style
            {
                MaxHeight = 200,
                Padding = new S.EdgeValues(Shadcn.Space2),
                Gap = Shadcn.Space1,
            }),
                items.Select((item, i) =>
                {
                    var node = Div(new S.Style
                    {
                        FlexDirection = S.FlexDirection.Row,
                        JustifyContent = S.JustifyContent.SpaceBetween,
                        AlignItems = S.AlignItems.Center,
                        FlexShrink = 0,
                        Padding = new S.EdgeValues(Shadcn.Space1, Shadcn.Space2),
                        Background = i % 2 == 0 ? Shadcn.Background : Shadcn.Muted,
                        BorderRadius = Shadcn.RadiusSm,
                        Hover = new S.Style { Background = Shadcn.Accent },
                        Transitions = new[] { new S.Transition { Property = "background", Duration = 0.1f, Easing = S.EasingType.Ease } },
                    },
                        Text(item.name, new S.Style { FontSize = Shadcn.TextSm, Color = Shadcn.Foreground, FlexGrow = 1 }),
                        Div(new S.Style { FlexDirection = S.FlexDirection.Row, Gap = Shadcn.Space1 },
                            Button("+", () =>
                            {
                                var newItems = items.ToList();
                                newItems.Insert(i + 1, (id: nextId, name: item.name));
                                setItems(newItems.ToArray());
                                setNextId(nextId + 1);
                            }, ClassName("btn btn-icon btn-outline", new S.Style
                            {
                                Width = S.StyleValue.Px(28),
                                Height = S.StyleValue.Px(28),
                            })),
                            Button("✕", () =>
                            {
                                setItems(items.Where((_, j) => j != i).ToArray());
                            }, ClassName("btn btn-icon btn-destructive", new S.Style
                            {
                                Width = S.StyleValue.Px(28),
                                Height = S.StyleValue.Px(28),
                            }))
                        )
                    );
                    node.Key = item.id.ToString();
                    return node;
                }).ToArray()
            ),

            Text($"{items.Length} items", ClassName("muted"))
        );
    }
}
