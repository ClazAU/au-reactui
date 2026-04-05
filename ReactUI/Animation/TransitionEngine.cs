using System;
using System.Collections.Generic;

namespace ReactUI.Animation;

/// <summary>
/// Tracks an in-flight animation for a single CSS property on a single node.
/// </summary>
public class PropertyAnimation
{
    public string Property = "";
    public object? FromValue;
    public object? ToValue;
    public float StartTime;
    public float Duration;
    public float Delay;
    public Style.EasingType Easing;
    public bool IsActive;
    public float Elapsed;
}

/// <summary>
/// Manages per-property transitions for all UINodes. When a property's target value changes
/// and a matching Transition is configured, an animation is started that interpolates
/// from the old value to the new value over the specified duration.
/// </summary>
public static class TransitionEngine
{
    // Keyed by UINode hash code, then by property name
    static readonly Dictionary<int, Dictionary<string, PropertyAnimation>> _animations = new();

    /// <summary>
    /// Advance all active animations by deltaTime.
    /// </summary>
    public static void Tick(float deltaTime)
    {
        // Collect finished animation keys to remove
        List<int>? emptyNodes = null;

        foreach (var kvp in _animations)
        {
            List<string>? finished = null;
            foreach (var anim in kvp.Value)
            {
                if (!anim.Value.IsActive) continue;
                anim.Value.Elapsed += deltaTime;
                if (anim.Value.Elapsed >= anim.Value.Duration + anim.Value.Delay)
                {
                    anim.Value.IsActive = false;
                    (finished ??= new List<string>()).Add(anim.Key);
                }
            }
            if (finished != null)
            {
                foreach (var key in finished)
                    kvp.Value.Remove(key);
            }
            if (kvp.Value.Count == 0)
                (emptyNodes ??= new List<int>()).Add(kvp.Key);
        }

        if (emptyNodes != null)
        {
            foreach (var key in emptyNodes)
                _animations.Remove(key);
        }
    }

    /// <summary>
    /// Check if a property value changed and start a transition if configured.
    /// Returns the interpolated value if an animation is active, otherwise the target value.
    /// </summary>
    public static object? GetAnimatedValue(Core.UINode node, string property, object? targetValue,
                                           Style.Transition[]? transitions)
    {
        if (transitions == null || transitions.Length == 0)
            return targetValue;

        // Find matching transition for this property
        Style.Transition? matchedTransition = null;
        foreach (var tr in transitions)
        {
            if (tr.Property == property || tr.Property == "all")
            {
                matchedTransition = tr;
                break;
            }
        }

        if (matchedTransition == null)
            return targetValue;

        var tr2 = matchedTransition.Value;
        int nodeKey = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(node);

        if (!_animations.TryGetValue(nodeKey, out var propAnims))
        {
            propAnims = new Dictionary<string, PropertyAnimation>();
            _animations[nodeKey] = propAnims;
        }

        if (!propAnims.TryGetValue(property, out var anim))
        {
            // No existing animation — store the target as the "current" without animating
            anim = new PropertyAnimation
            {
                Property = property,
                FromValue = targetValue,
                ToValue = targetValue,
                Duration = tr2.Duration,
                Delay = tr2.Delay,
                Easing = tr2.Easing,
                IsActive = false,
                Elapsed = 0,
            };
            propAnims[property] = anim;
            return targetValue;
        }

        // Check if the target changed
        if (!ValuesEqual(anim.ToValue, targetValue))
        {
            // Start a new animation from current interpolated value to new target
            object? currentValue = anim.IsActive ? GetCurrentInterpolated(anim) : anim.ToValue;
            anim.FromValue = currentValue;
            anim.ToValue = targetValue;
            anim.Duration = tr2.Duration;
            anim.Delay = tr2.Delay;
            anim.Easing = tr2.Easing;
            anim.Elapsed = 0;
            anim.IsActive = true;
        }

        if (!anim.IsActive)
            return targetValue;

        return GetCurrentInterpolated(anim);
    }

    /// <summary>
    /// Removes all animation state for a node (call on destroy).
    /// </summary>
    public static void RemoveNode(Core.UINode node)
    {
        int nodeKey = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(node);
        _animations.Remove(nodeKey);
    }

    /// <summary>
    /// Clears all animation state.
    /// </summary>
    public static void Reset()
    {
        _animations.Clear();
    }

    static object? GetCurrentInterpolated(PropertyAnimation anim)
    {
        float elapsed = anim.Elapsed - anim.Delay;
        if (elapsed < 0) return anim.FromValue;
        float rawT = anim.Duration > 0 ? System.Math.Clamp(elapsed / anim.Duration, 0f, 1f) : 1f;
        float t = Easing.Evaluate(anim.Easing, rawT);
        return Interpolate(anim.FromValue, anim.ToValue, t);
    }

    static object? Interpolate(object? from, object? to, float t)
    {
        if (from == null || to == null) return t >= 1f ? to : from;

        // UIColor
        if (from is Style.UIColor colorFrom && to is Style.UIColor colorTo)
            return Style.UIColor.Lerp(colorFrom, colorTo, t);

        // float
        if (from is float floatFrom && to is float floatTo)
            return floatFrom + (floatTo - floatFrom) * t;

        // EdgeValues
        if (from is Style.EdgeValues edgeFrom && to is Style.EdgeValues edgeTo)
            return Style.EdgeValues.Lerp(edgeFrom, edgeTo, t);

        // BoxShadow
        if (from is Style.BoxShadow shadowFrom && to is Style.BoxShadow shadowTo)
            return Style.BoxShadow.Lerp(shadowFrom, shadowTo, t);

        // int (font weight, etc) — lerp as float and round
        if (from is int intFrom && to is int intTo)
            return (int)(intFrom + (intTo - intFrom) * t);

        // Non-interpolable: snap at halfway
        return t >= 0.5f ? to : from;
    }

    static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.Equals(b);
    }
}
