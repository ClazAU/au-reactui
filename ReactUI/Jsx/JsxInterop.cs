using System;
using System.Collections.Generic;

namespace ReactUI.Jsx;

/// <summary>
/// Cross-references between C# and JSX. Three mechanisms:
///
/// 1. **Shared state** — C# pushes values, JSX reads them reactively:
///      C#:  UI.SetJsxState("playerName", player.Name);
///      JSX: const name = useShared('playerName');
///
/// 2. **Functions** — C# registers typed functions, JSX calls them naturally:
///      C#:  UI.ExposeToJsx("getPlayers", () => PlayerList.Select(p => p.Name).ToArray());
///      JSX: const players = csharp.getPlayers();
///
/// 3. **Props** — Pass C# data when mounting a JSX component:
///      C#:  UI.RenderJsx("Panel.jsx", new { playerName = "Steve", isHost = true });
///      JSX: function Panel({ playerName, isHost }) { ... }
/// </summary>
public static class JsxInterop
{
    // ─── Shared state ────────────────────────
    // C# writes, JSX reads via useShared('key'). When C# updates a value,
    // all JSX components using that key re-render automatically.

    private static readonly Dictionary<string, object?> _sharedState = new();
    private static readonly Dictionary<string, List<int>> _stateSubscribers = new(); // key → componentIds
    private static readonly object _lock = new();

    /// <summary>
    /// Set a shared value accessible from JSX via useShared('key').
    /// Triggers re-render of all JSX components that read this key.
    /// </summary>
    public static void Set(string key, object? value)
    {
        lock (_lock)
        {
            _sharedState[key] = value;

            // Schedule re-renders for all subscribers
            if (_stateSubscribers.TryGetValue(key, out var subs))
            {
                foreach (var componentId in subs)
                    Core.Scheduler.ScheduleRender(componentId);
            }
        }
    }

    /// <summary>
    /// Set multiple shared values at once (batched — one re-render per component).
    /// </summary>
    public static void Set(Dictionary<string, object?> values)
    {
        lock (_lock)
        {
            var dirtyComponents = new HashSet<int>();
            foreach (var (key, value) in values)
            {
                _sharedState[key] = value;
                if (_stateSubscribers.TryGetValue(key, out var subs))
                    foreach (var id in subs)
                        dirtyComponents.Add(id);
            }

            foreach (var id in dirtyComponents)
                Core.Scheduler.ScheduleRender(id);
        }
    }

    /// <summary>Get a shared value (used by the Jint bridge for useShared).</summary>
    internal static object? Get(string key)
    {
        lock (_lock)
        {
            return _sharedState.TryGetValue(key, out var val) ? val : null;
        }
    }

    /// <summary>Subscribe a component to a shared state key (called from useShared).</summary>
    internal static void Subscribe(string key, int componentId)
    {
        lock (_lock)
        {
            if (!_stateSubscribers.ContainsKey(key))
                _stateSubscribers[key] = new List<int>();

            var subs = _stateSubscribers[key];
            if (!subs.Contains(componentId))
                subs.Add(componentId);
        }
    }

    /// <summary>Unsubscribe a component from all shared state keys.</summary>
    internal static void Unsubscribe(int componentId)
    {
        lock (_lock)
        {
            foreach (var subs in _stateSubscribers.Values)
                subs.Remove(componentId);
        }
    }

    // ─── Exposed functions ────────────────────────
    // C# registers functions, JSX calls them via csharp.functionName()

    private static readonly Dictionary<string, Delegate> _functions = new();

    /// <summary>
    /// Expose a C# function to JSX. Callable as csharp.name() or csharp.name(arg1, arg2).
    /// </summary>
    public static void Expose(string name, Delegate function)
    {
        _functions[name] = function;
    }

    /// <summary>Expose a parameterless function.</summary>
    public static void Expose<TResult>(string name, Func<TResult> function) =>
        _functions[name] = function;

    /// <summary>Expose a function with one parameter.</summary>
    public static void Expose<T, TResult>(string name, Func<T, TResult> function) =>
        _functions[name] = function;

    /// <summary>Expose a void action.</summary>
    public static void Expose(string name, Action action) =>
        _functions[name] = action;

    /// <summary>Expose a void action with one parameter.</summary>
    public static void Expose<T>(string name, Action<T> action) =>
        _functions[name] = action;

    /// <summary>Get all exposed functions (used by Jint bridge to build the csharp object).</summary>
    internal static IReadOnlyDictionary<string, Delegate> ExposedFunctions => _functions;

    // ─── Props for component mounting ────────────────────────

    private static readonly Dictionary<string, Dictionary<string, object?>> _componentProps = new();

    /// <summary>
    /// Set initial props for a JSX component. The component receives them
    /// as function arguments: function MyPanel({ playerName, isHost }) { ... }
    /// </summary>
    internal static void SetProps(string filePath, Dictionary<string, object?> props)
    {
        _componentProps[filePath] = props;
    }

    /// <summary>Get props for a component (used by Jint bridge).</summary>
    internal static Dictionary<string, object?>? GetProps(string filePath)
    {
        return _componentProps.TryGetValue(filePath, out var props) ? props : null;
    }

    /// <summary>Clear all state (for testing).</summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _sharedState.Clear();
            _stateSubscribers.Clear();
            _functions.Clear();
            _componentProps.Clear();
        }
    }
}
