using System;
using System.Linq;
using ReactUI.Core;
using S = ReactUI.Style;
using static ReactUI.UI;

namespace ReactUI.Example;

/// <summary>
/// Left-side speaking players overlay. Non-draggable, auto-resizes, stays centered vertically.
/// For testing: randomly adds/removes players every ~1 second.
/// </summary>
public static class SpeakingOverlay
{
    private static readonly Func<VNode> Render_ = Component(RenderRoot);
    public static VNode Render() => Render_();

    static readonly string[] AllPlayers = { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Iris", "Jake" };
    static readonly string[] Colors = { "#e74c3c", "#3498db", "#2ecc71", "#f39c12", "#9b59b6", "#1abc9c", "#e67e22", "#e91e63", "#00bcd4", "#8bc34a" };

    private static VNode RenderRoot()
    {
        var (speakers, setSpeakers) = UseState(new[] { "Alice", "Charlie", "Frank" });
        var lastChange = UseRef(0f);

        // Randomly add/remove a speaker every ~1.2 seconds
        float now = UnityEngine.Time.time;
        if (now - lastChange.Current > 1.2f)
        {
            lastChange.Current = now;
            var rng = new Random((int)(now * 1000));
            var current = speakers.ToList();

            // 50% chance add, 50% chance remove
            if (current.Count > 1 && (current.Count >= 6 || rng.Next(2) == 0))
            {
                current.RemoveAt(rng.Next(current.Count));
            }
            else
            {
                var available = AllPlayers.Where(p => !current.Contains(p)).ToArray();
                if (available.Length > 0)
                    current.Add(available[rng.Next(available.Length)]);
            }
            setSpeakers(current.ToArray());
        }

        // Schedule re-render to keep the timer ticking
        Core.Scheduler.ScheduleRender(Hooks.HooksRuntime.Current.ComponentId);

        if (speakers.Length == 0) return Div();

        // Full-height wrapper on the left, centered vertically
        return Div(new S.Style
        {
            Position = S.PositionType.Absolute,
            Inset = new S.EdgeValues(0, float.NaN, 0, 0),
            Width = 100,
            JustifyContent = S.JustifyContent.Center,
            Padding = new S.EdgeValues(8),
        },
            // Inner container for the pills
            Div(new S.Style { Gap = 4 },
            speakers.Select((name, i) =>
            {
                int colorIdx = Array.IndexOf(AllPlayers, name) % Colors.Length;
                var node = SpeakerPill(name, Colors[colorIdx]);
                node.Key = name;
                return node;
            }).ToArray()
            )
        );
    }

    private static VNode SpeakerPill(string name, string color)
    {
        var avatar = Div(new S.Style
        {
            Width = S.StyleValue.Px(28),
            Height = S.StyleValue.Px(28),
            FlexShrink = 0,
            BorderRadius = 14,
            Background = color,
            AlignItems = S.AlignItems.Center,
            JustifyContent = S.JustifyContent.Center,
        },
            Text(name.Substring(0, 1), new S.Style { FontSize = 13, FontWeight = 700, Color = "#fff" })
        );

        return Div(new S.Style
        {
            FlexDirection = S.FlexDirection.Row,
            AlignItems = S.AlignItems.Center,
            Gap = 8,
            BackgroundGradient = new S.Gradient
            {
                Type = S.GradientType.Linear,
                Angle = 0,
                ColorA = "rgba(0,0,0,0.7)",
                ColorB = "rgba(0,0,0,0)",
            },
            BorderRadius = 20,
            Padding = new S.EdgeValues(4, 16, 4, 4),
            FlexShrink = 0,
        },
            avatar,
            Text(name, new S.Style { FontSize = 13, FontWeight = 600, Color = "#fff" })
        );
    }
}
