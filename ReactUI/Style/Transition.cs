namespace ReactUI.Style;

public enum EasingType { Linear, Ease, EaseIn, EaseOut, EaseInOut }

public struct Transition
{
    public string Property;
    public float Duration;
    public float Delay;
    public EasingType Easing;

    /// <summary>
    /// Parses a CSS-like transition string.
    /// Example: "background 0.15s ease, color 0.15s ease 0.05s"
    /// Format per entry: property duration [easing] [delay]
    /// </summary>
    public static Transition[] Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return System.Array.Empty<Transition>();

        var entries = s.Split(',');
        var result = new Transition[entries.Length];
        var inv = System.Globalization.CultureInfo.InvariantCulture;

        for (int i = 0; i < entries.Length; i++)
        {
            var parts = entries[i].Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var t = new Transition
            {
                Property = parts.Length > 0 ? parts[0] : "all",
                Duration = parts.Length > 1 ? ParseSeconds(parts[1], inv) : 0f,
                Easing = EasingType.Ease,
                Delay = 0f,
            };

            // Remaining parts can be easing and/or delay in any order
            for (int p = 2; p < parts.Length; p++)
            {
                var easing = ParseEasingType(parts[p]);
                if (easing.HasValue)
                {
                    t.Easing = easing.Value;
                }
                else
                {
                    t.Delay = ParseSeconds(parts[p], inv);
                }
            }

            result[i] = t;
        }

        return result;
    }

    private static float ParseSeconds(string s, System.Globalization.CultureInfo inv)
    {
        s = s.Trim();
        if (s.EndsWith("ms"))
            return float.Parse(s.Substring(0, s.Length - 2), System.Globalization.NumberStyles.Float, inv) / 1000f;
        if (s.EndsWith("s"))
            return float.Parse(s.Substring(0, s.Length - 1), System.Globalization.NumberStyles.Float, inv);
        return float.TryParse(s, System.Globalization.NumberStyles.Float, inv, out var v) ? v : 0f;
    }

    private static EasingType? ParseEasingType(string s) => s.Trim().ToLowerInvariant() switch
    {
        "linear" => EasingType.Linear,
        "ease" => EasingType.Ease,
        "ease-in" => EasingType.EaseIn,
        "ease-out" => EasingType.EaseOut,
        "ease-in-out" => EasingType.EaseInOut,
        _ => null,
    };
}
