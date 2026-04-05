using System;

namespace ReactUI.Hooks;

/// <summary>
/// State stored in a hook slot for each UseMemo call.
/// </summary>
internal class MemoState
{
    public object? CachedValue;
    public object[]? Deps;
}

/// <summary>
/// React-style useMemo hook. Memoizes an expensive computation, recomputing only when deps change.
/// IL2CPP safe: cached value boxed as object.
/// </summary>
public static class UseMemoHook
{
    /// <summary>
    /// Returns a memoized value. The factory is only re-invoked when deps change.
    /// </summary>
    /// <param name="factory">The computation to memoize.</param>
    /// <param name="deps">Dependency array. Recomputes when any element changes. Null = recompute every render.</param>
    // [Preserve] — add Il2CppInterop.Runtime.Attributes.Preserve to prevent IL2CPP stripping
    public static T UseMemo<T>(Func<T> factory, object[]? deps)
    {
        var ctx = HooksRuntime.Current;

        if (ctx.IsFirstRender)
        {
            // First render: compute and store
            var value = factory();
            var state = new MemoState
            {
                CachedValue = value,
                Deps = CopyDeps(deps),
            };
            ctx.AddSlot(state);
            return value;
        }
        else
        {
            var state = (MemoState)ctx.ReadSlot()!;

            if (deps == null || !UseEffectHook.DepsEqual(state.Deps, deps))
            {
                // Deps changed — recompute
                var value = factory();
                state.CachedValue = value;
                state.Deps = CopyDeps(deps);
                return value;
            }

            // Deps unchanged — return cached
            if (state.CachedValue == null)
                return default!;
            return (T)state.CachedValue;
        }
    }

    private static object[]? CopyDeps(object[]? deps)
    {
        if (deps == null) return null;
        var copy = new object[deps.Length];
        Array.Copy(deps, copy, deps.Length);
        return copy;
    }
}
