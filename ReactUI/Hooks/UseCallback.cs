using System;

namespace ReactUI.Hooks;

/// <summary>
/// React-style useCallback hook. Sugar over UseMemo that memoizes a delegate.
/// Returns the same delegate instance as long as deps haven't changed,
/// preventing unnecessary re-renders of child components that compare callbacks by reference.
/// </summary>
public static class UseCallbackHook
{
    /// <summary>Memoizes a parameterless Action.</summary>
    public static Action UseCallback(Action callback, object[]? deps)
    {
        return UseMemoHook.UseMemo(() => callback, deps);
    }

    /// <summary>Memoizes an Action&lt;T&gt;.</summary>
    // [Preserve] — add Il2CppInterop.Runtime.Attributes.Preserve to prevent IL2CPP stripping
    public static Action<T> UseCallback<T>(Action<T> callback, object[]? deps)
    {
        return UseMemoHook.UseMemo(() => callback, deps);
    }

    /// <summary>Memoizes a Func&lt;T&gt;.</summary>
    public static Func<T> UseCallback<T>(Func<T> callback, object[]? deps)
    {
        return UseMemoHook.UseMemo(() => callback, deps);
    }

    /// <summary>Memoizes a Func&lt;T1, T2&gt;.</summary>
    public static Func<T1, T2> UseCallback<T1, T2>(Func<T1, T2> callback, object[]? deps)
    {
        return UseMemoHook.UseMemo(() => callback, deps);
    }
}
