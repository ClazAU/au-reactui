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

    public static VNode Button(Action onClick, Style.Style? style, params VNode[] children) =>
        ButtonElement.Create(onClick, style, children);

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

    public static VNode Portal(params VNode[] children) =>
        PortalElement.Create(children);
}
