using System;
using ReactUI.Core;

namespace ReactUI.Hooks;

/// <summary>
/// State stored in a hook slot for each UseEffect call.
/// </summary>
public class EffectState
{
    /// <summary>Dependencies from the last render. Null means "run every render".</summary>
    public object[]? Deps;

    /// <summary>Cleanup function returned by the last effect invocation.</summary>
    public Action? Cleanup;
}

/// <summary>
/// React-style useEffect hook. Effects are queued and run after render+paint.
/// Supports dependency arrays and cleanup functions.
/// IL2CPP safe: deps are compared with object.Equals (boxed).
/// </summary>
public static class UseEffectHook
{
    /// <summary>
    /// Registers a side-effect that runs after render. No cleanup.
    /// </summary>
    /// <param name="effect">The effect to run.</param>
    /// <param name="deps">
    /// Dependency array. null = run every render. Empty array = run once (mount only).
    /// Non-empty array = run when any element changes (compared via Equals).
    /// </param>
    public static void UseEffect(Action effect, object[]? deps = null)
    {
        UseEffect(() => { effect(); return null; }, deps);
    }

    /// <summary>
    /// Registers a side-effect that runs after render, with optional cleanup.
    /// The effect function may return a cleanup Action that runs before the next
    /// effect invocation or on unmount.
    /// </summary>
    /// <param name="effectWithCleanup">Effect function. Return null for no cleanup, or an Action to clean up.</param>
    /// <param name="deps">
    /// Dependency array. null = run every render. Empty array = run once (mount only).
    /// Non-empty array = run when any element changes.
    /// </param>
    public static void UseEffect(Func<Action?> effectWithCleanup, object[]? deps = null)
    {
        var ctx = HooksRuntime.Current;

        if (ctx.IsFirstRender)
        {
            // First render: create the effect state, queue the effect
            var state = new EffectState { Deps = CopyDeps(deps) };
            ctx.AddSlot(state);

            // Queue the effect to run after render+paint
            var capturedEffect = effectWithCleanup;
            var capturedState = state;
            Scheduler.QueueEffect(() =>
            {
                capturedState.Cleanup = capturedEffect();
            });
        }
        else
        {
            // Subsequent render: check if deps changed
            var state = (EffectState)ctx.ReadSlot()!;
            var oldDeps = state.Deps;

            if (deps == null || !DepsEqual(oldDeps, deps))
            {
                // Deps changed (or no deps = run every time): queue cleanup + re-run
                state.Deps = CopyDeps(deps);
                var capturedEffect = effectWithCleanup;
                var capturedState = state;

                Scheduler.QueueEffect(() =>
                {
                    // Run cleanup from previous effect
                    capturedState.Cleanup?.Invoke();
                    // Run the new effect
                    capturedState.Cleanup = capturedEffect();
                });
            }
            // If deps are equal, skip — effect doesn't re-run
        }
    }

    /// <summary>
    /// Compares two dependency arrays element-wise using object.Equals.
    /// Returns true if both are non-null, same length, and all elements are equal.
    /// </summary>
    internal static bool DepsEqual(object[]? a, object[]? b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == null && b[i] == null) continue;
            if (a[i] == null || b[i] == null) return false;
            if (!a[i]!.Equals(b[i])) return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a shallow copy of the deps array (so mutations don't affect stored deps).
    /// </summary>
    private static object[]? CopyDeps(object[]? deps)
    {
        if (deps == null) return null;
        var copy = new object[deps.Length];
        Array.Copy(deps, copy, deps.Length);
        return copy;
    }
}
