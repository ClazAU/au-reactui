using System;
using System.Collections.Generic;
using ReactUI.Core;
using ReactUI.Hooks;
using ReactUI.Elements;

namespace ReactUI;

/// <summary>
/// Top-level public API for ReactUI. Mod authors import this namespace
/// and call UI.Render(), UI.Div(), UI.UseState(), etc.
/// </summary>
public static class UI
{
    // --- Mount / Unmount ---

    /// <summary>Mount a root component. Returns a handle; Dispose it to unmount.</summary>
    public static RenderHandle Render(Func<VNode> rootComponent) => Scheduler.Mount(rootComponent);

    // --- Global Styles ---

    /// <summary>Register a stylesheet into the global registry. All components can use these class names.</summary>
    public static void RegisterStyles(Style.StyleSheet sheet) => Style.GlobalStyles.Register(sheet);

    /// <summary>Register a single named style globally.</summary>
    public static void RegisterStyle(string className, Style.Style style) => Style.GlobalStyles.Register(className, style);

    /// <summary>
    /// Resolve a className (space-separated) into a Style from the global registry.
    /// Use this as the style argument to element factories.
    /// Example: UI.Div(UI.ClassName("panel title"), ...)
    /// </summary>
    public static Style.Style ClassName(string className) => Style.GlobalStyles.Resolve(className);

    /// <summary>
    /// Resolve a className and merge with an inline style override.
    /// Inline style properties win over class properties.
    /// Example: UI.Div(UI.ClassName("panel", new Style { Opacity = 0.5f }), ...)
    /// </summary>
    public static Style.Style ClassName(string className, Style.Style inlineOverride) =>
        Style.GlobalStyles.Resolve(className, inlineOverride);

    // --- Context ---

    /// <summary>Create a typed context for passing values down the tree without prop drilling.</summary>
    public static Context<T> CreateContext<T>(T defaultValue) => new(defaultValue);

    // --- Component wrappers ---

    /// <summary>Wrap a parameterless render function as a component (enables hooks).</summary>
    public static Func<VNode> Component(Func<VNode> render) => ComponentFactory.Component(render);

    /// <summary>Wrap a props-based render function as a component (enables hooks).</summary>
    public static Func<TProps, VNode> Component<TProps>(Func<TProps, VNode> render) => ComponentFactory.Component(render);

    // --- Hooks (re-exported for convenience) ---

    public static (T value, Action<T> setter) UseState<T>(T initial) =>
        UseStateHook.UseState(initial);

    public static void UseEffect(Action effect, object[]? deps = null) =>
        UseEffectHook.UseEffect(effect, deps);

    public static void UseEffect(Func<Action?> effect, object[]? deps = null) =>
        UseEffectHook.UseEffect(effect, deps);

    public static T UseMemo<T>(Func<T> factory, object[]? deps = null) =>
        UseMemoHook.UseMemo(factory, deps);

    public static Ref<T> UseRef<T>(T? initial = default) =>
        UseRefHook.UseRef(initial);

    public static Action UseCallback(Action cb, object[]? deps = null) =>
        UseCallbackHook.UseCallback(cb, deps);

    public static T UseContext<T>(Context<T> ctx) =>
        UseContextHook.UseContext(ctx);

    // --- Element factories (short names) ---

    public static VNode Div(Style.Style? style = null, params VNode[] children) =>
        Elements.Div.Create(style, children);

    public static VNode Div(Style.Style? style, IEnumerable<VNode?> children) =>
        Elements.Div.Create(style, children);

    public static VNode Text(string content, Style.Style? style = null) =>
        TextElement.Create(content, style);

    public static VNode Button(string label, Action onClick, Style.Style? style = null) =>
        ButtonElement.Create(label, onClick, style);

    public static VNode Button(string label, Action onClick, Action onRightClick, Style.Style? style = null) =>
        ButtonElement.Create(label, onClick, onRightClick, style);

    public static VNode Button(Action onClick, Style.Style? style, params VNode[] children) =>
        ButtonElement.Create(onClick, style, children);

    public static VNode Button(Action onClick, Action onRightClick, Style.Style? style, params VNode[] children) =>
        ButtonElement.Create(onClick, onRightClick, style, children);

    public static VNode Input(string value, Action<string> onChange, Style.Style? style = null, string placeholder = "") =>
        InputElement.Create(value, onChange, style, placeholder);

    public static VNode Image(UnityEngine.Texture2D tex, Style.Style? style = null) =>
        ImageElement.Create(tex, style);

    public static VNode ScrollView(Style.Style? style = null, params VNode[] children) =>
        ScrollViewElement.Create(style, children);

    public static VNode Slider(float value, Action<float> onChange, float min = 0, float max = 1, Style.Style? style = null) =>
        SliderElement.Create(value, onChange, min, max, style);

    public static VNode Toggle(bool value, Action<bool> onChange, Style.Style? style = null) =>
        ToggleElement.Create(value, onChange, style);

    public static VNode Select(string value, Action<string> onChange, string[] options, Style.Style? style = null) =>
        SelectElement.Create(value, onChange, options, style);

    public static VNode Tooltip(string text, VNode child, Style.Style? style = null) =>
        TooltipElement.Create(text, child, style);

    public static VNode Grid(int cols, float cellW, float cellH, float gap, Style.Style? style, params VNode[] children) =>
        GridElement.Create(cols, cellW, cellH, gap, style, children);

    public static VNode KeyCapture(Action<UnityEngine.KeyCode> onCapture, Style.Style? style = null, string prompt = "Press a key...") =>
        KeyCaptureElement.Create(onCapture, style, prompt);

    /// <summary>
    /// Make a specific element (by key) a drag handle. Only mousedown on this element
    /// starts the drag. Use node.Key = "my-handle" then UI.DragHandle("my-handle", ...).
    /// </summary>
    public static void DragHandle(string elementKey, Func<float> getX, Func<float> getY, Action<float> setX, Action<float> setY) =>
        ReactUI.Input.InputSystem.RegisterDragHandle(elementKey, getX, getY, setX, setY);

    public static VNode Portal(params VNode[] children) =>
        PortalElement.Create(children);

    // --- JSX Hot Reload ---

    private static Jsx.HotReloader? _hotReloader;

    /// <summary>Get or create the hot reloader instance.</summary>
    private static Jsx.HotReloader HotReloader => _hotReloader ??= new Jsx.HotReloader();

    /// <summary>
    /// Mount a JSX component from a file path. The file is watched for changes
    /// and hot-reloaded automatically.
    /// </summary>
    public static RenderHandle? RenderJsx(string filePath) => HotReloader.Mount(filePath);

    /// <summary>
    /// Start watching a directory for .jsx and .css file changes.
    /// All files in the directory are eligible for hot reload.
    /// </summary>
    public static void WatchJsx(string directory) => HotReloader.Watch(directory);

    /// <summary>
    /// Register a C# component that can be used from JSX via createElement.
    /// </summary>
    public static void RegisterJsxComponent(string name, Func<VNode> factory) =>
        HotReloader.RegisterComponent(name, factory);

    // --- JSX ↔ C# Interop ---

    /// <summary>
    /// Set a shared value readable from JSX via useShared('key').
    /// Automatically re-renders any JSX component reading this key.
    /// Example: UI.SetJsxState("playerName", player.Name);
    /// </summary>
    public static void SetJsxState(string key, object? value) => Jsx.JsxInterop.Set(key, value);

    /// <summary>
    /// Set multiple shared values at once (batched — one re-render per component).
    /// </summary>
    public static void SetJsxState(Dictionary<string, object?> values) => Jsx.JsxInterop.Set(values);

    /// <summary>
    /// Expose a C# function to JSX. Callable as csharp.name() from JSX.
    /// Example: UI.ExposeToJsx("getPlayers", () => players.Select(p => p.Name).ToArray());
    /// </summary>
    public static void ExposeToJsx(string name, Delegate function) => Jsx.JsxInterop.Expose(name, function);

    /// <inheritdoc cref="ExposeToJsx(string, Delegate)"/>
    public static void ExposeToJsx<TResult>(string name, Func<TResult> function) => Jsx.JsxInterop.Expose(name, function);

    /// <inheritdoc cref="ExposeToJsx(string, Delegate)"/>
    public static void ExposeToJsx<T, TResult>(string name, Func<T, TResult> function) => Jsx.JsxInterop.Expose(name, function);

    /// <inheritdoc cref="ExposeToJsx(string, Delegate)"/>
    public static void ExposeToJsx(string name, Action action) => Jsx.JsxInterop.Expose(name, action);

    /// <inheritdoc cref="ExposeToJsx(string, Delegate)"/>
    public static void ExposeToJsx<T>(string name, Action<T> action) => Jsx.JsxInterop.Expose(name, action);

    /// <summary>
    /// Register a C# action that can be called from JSX via call('name', args).
    /// </summary>
    public static void RegisterAction(string name, Delegate action) =>
        HotReloader.RegisterAction(name, action);

    /// <summary>
    /// Force reload a specific JSX or CSS file. Useful when FileSystemWatcher
    /// is unreliable.
    /// </summary>
    public static void ReloadJsx(string filePath) => HotReloader.Reload(filePath);

    /// <summary>
    /// Drain the hot-reload queue. Called automatically from ReactUIBehaviour.Update()
    /// but can also be called manually.
    /// </summary>
    public static void TickJsx() => _hotReloader?.Tick();
}
