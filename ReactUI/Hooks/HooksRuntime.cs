using System.Collections.Generic;

namespace ReactUI.Hooks;

/// <summary>
/// Per-component hook state. Stores all hook slots (state, effects, memos, refs) as boxed objects.
/// IL2CPP safe: no Reflection.Emit, no dynamic — all generics box to object.
/// </summary>
public class HookContext
{
    /// <summary>The component this context belongs to.</summary>
    public int ComponentId;

    /// <summary>
    /// Ordered list of hook slots. Each UseState/UseEffect/UseMemo/UseRef call occupies one slot.
    /// Values are boxed as object for IL2CPP safety.
    /// </summary>
    public List<object?> StateSlots = new();

    /// <summary>Current hook index, incremented as hooks are called during render.</summary>
    public int CurrentHookIndex;

    /// <summary>True during the first render of this component (before EndComponent is called).</summary>
    public bool IsFirstRender = true;

    /// <summary>Reads the value at the current slot and advances the index.</summary>
    public object? ReadSlot()
    {
        return StateSlots[CurrentHookIndex++];
    }

    /// <summary>Overwrites the most recently read slot (CurrentHookIndex - 1).</summary>
    public void WriteSlot(object? value)
    {
        StateSlots[CurrentHookIndex - 1] = value;
    }

    /// <summary>Appends a new slot with an initial value and advances the index. Used on first render only.</summary>
    public void AddSlot(object? initial)
    {
        StateSlots.Add(initial);
        CurrentHookIndex++;
    }

    /// <summary>Resets the hook index to 0 before a render pass.</summary>
    public void ResetIndex()
    {
        CurrentHookIndex = 0;
    }
}

/// <summary>
/// Global hooks runtime. Manages the context stack so hooks know which component they belong to.
/// Thread-safety note: Among Us is single-threaded, so no locking needed.
/// </summary>
public static class HooksRuntime
{
    private static readonly Stack<HookContext> _contextStack = new();
    private static readonly Dictionary<int, HookContext> _contexts = new();

    /// <summary>
    /// Activates the hooks context for a component before its render function is called.
    /// Creates a new context on first render.
    /// </summary>
    public static void BeginComponent(int componentId)
    {
        if (!_contexts.TryGetValue(componentId, out var ctx))
        {
            ctx = new HookContext { ComponentId = componentId };
            _contexts[componentId] = ctx;
        }
        ctx.ResetIndex();
        _contextStack.Push(ctx);
    }

    /// <summary>
    /// Deactivates the current hooks context after render completes.
    /// Marks the component as no longer on its first render.
    /// </summary>
    public static void EndComponent()
    {
        var ctx = _contextStack.Pop();
        ctx.IsFirstRender = false;
    }

    /// <summary>
    /// Returns the currently active hooks context (top of stack).
    /// Hooks call this to access their slots.
    /// </summary>
    public static HookContext Current => _contextStack.Peek();

    /// <summary>
    /// Gets the context for a given component ID (or null if not found).
    /// Used by the reconciler to attach HookContext to UINodes.
    /// </summary>
    public static HookContext? GetContext(int componentId)
    {
        _contexts.TryGetValue(componentId, out var ctx);
        return ctx;
    }

    /// <summary>
    /// Removes and discards the hooks context for a component (on unmount).
    /// </summary>
    public static void RemoveContext(int componentId)
    {
        _contexts.Remove(componentId);
    }

    /// <summary>
    /// Resets all hooks state. Useful for testing or full teardown.
    /// </summary>
    public static void Reset()
    {
        _contextStack.Clear();
        _contexts.Clear();
    }
}
