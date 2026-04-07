using System;
using System.Collections.Generic;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using ReactUI.Core;
using ReactUI.Hooks;
using ReactUI.Style;

namespace ReactUI.Jsx;

/// <summary>
/// Bridge between Jint (JavaScript interpreter) and ReactUI's C# hooks/VNode system.
/// Sets up a Jint Engine with React-like globals (createElement, useState, useEffect, etc.)
/// and evaluates JSX component files.
/// </summary>
public class JintBridge
{
    private Engine _engine;
    private readonly Dictionary<string, Func<VNode>> _registeredComponents = new();
    private readonly Dictionary<string, Delegate> _registeredActions = new();

    /// <summary>Per-component stylesheet (from CSS files or style blocks).</summary>
    public StyleSheet? ComponentStyles { get; set; }

    public JintBridge()
    {
        _engine = CreateEngine();
    }

    /// <summary>
    /// Evaluate a transformed JS source and return the default export function
    /// as a C# Func&lt;VNode&gt; suitable for Scheduler.Mount.
    /// </summary>
    public Func<VNode>? Evaluate(string transformedJs, int componentId)
    {
        try
        {
            _engine = CreateEngine();
            _engine.Execute(transformedJs);

            var defaultExport = _engine.GetValue("__defaultExport__");
            if (defaultExport.IsUndefined() || defaultExport.IsNull())
                return null;

            // Must be callable
            if (!defaultExport.IsObject())
                return null;

            return () =>
            {
                HooksRuntime.BeginComponent(componentId);
                try
                {
                    var result = _engine.Invoke("__defaultExport__");
                    return UnwrapVNode(result) ?? new VNode("div");
                }
                finally
                {
                    HooksRuntime.EndComponent();
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Evaluation error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Pre-process the transformed JS to capture the default export.
    /// Replaces "export default X" with "__defaultExport__ = X".
    /// </summary>
    public static string PrepareSource(string transformedJs)
    {
        var source = transformedJs;

        int idx = source.IndexOf("export default", StringComparison.Ordinal);
        if (idx >= 0)
        {
            var after = source.Substring(idx + "export default".Length).TrimStart();

            if (after.StartsWith("function "))
            {
                string remaining = after.Substring("function ".Length);
                int parenIdx = remaining.IndexOf('(');
                string funcName = parenIdx > 0 ? remaining.Substring(0, parenIdx).Trim() : "anonymous";
                source = source.Substring(0, idx) + after;
                source += $"\nvar __defaultExport__ = {funcName};";
            }
            else
            {
                source = source.Substring(0, idx) + "var __defaultExport__ = " + after;
            }
        }

        return source;
    }

    public void RegisterComponent(string name, Func<VNode> factory) =>
        _registeredComponents[name] = factory;

    public void RegisterAction(string name, Delegate action) =>
        _registeredActions[name] = action;

    // ─── Engine setup ────────────────────────

    private Engine CreateEngine()
    {
        var engine = new Engine(options =>
        {
            options.TimeoutInterval(TimeSpan.FromMilliseconds(500));
            options.LimitMemory(16_000_000);
        });

        // Core React API — use a JS wrapper to collect variadic children into an array
        engine.SetValue("_ce", new Func<JsValue, JsValue, JsValue, JsValue>(CreateElementInternal));
        engine.Execute("function createElement(type, props) { var children = []; for (var i = 2; i < arguments.length; i++) children.push(arguments[i]); return _ce(type, props, children); }");
        engine.SetValue("Fragment", "__fragment__");

        // Hooks
        engine.SetValue("useState", new Func<JsValue, JsValue>(UseState));
        engine.SetValue("useEffect", new Action<JsValue, JsValue>(UseEffect));
        engine.SetValue("useMemo", new Func<JsValue, JsValue, JsValue>(UseMemo));
        engine.SetValue("useRef", new Func<JsValue, JsValue>(UseRef));
        engine.SetValue("useCallback", new Func<JsValue, JsValue, JsValue>(UseCallback));

        // Custom hooks
        engine.SetValue("useShared", new Func<JsValue, JsValue>(UseShared));
        engine.SetValue("useDrag", new Func<JsValue, JsValue, JsValue>(UseDrag));

        // Expose the csharp.xxx() function bridge
        BuildCSharpObject(engine);

        // Utilities
        engine.SetValue("log", new Action<JsValue>(Log));
        engine.SetValue("call", new Func<string, JsValue[], JsValue>(CallAction));

        return engine;
    }

    // ─── createElement ────────────────────────

    private JsValue CreateElementInternal(JsValue type, JsValue props, JsValue childrenArray)
    {
        string typeStr;

        if (type.IsString())
        {
            typeStr = type.AsString();
            if (typeStr == "__fragment__")
                typeStr = "div";
        }
        else
        {
            typeStr = "div";
        }

        var node = new VNode(typeStr);

        // Process props — collect className and style separately to merge correctly
        ReactUI.Style.Style? classStyle = null;
        ReactUI.Style.Style? inlineStyle = null;

        if (!props.IsNull() && !props.IsUndefined() && props.IsObject())
        {
            var propsObj = props.AsObject();
            foreach (var prop in propsObj.GetOwnProperties())
            {
                string key = prop.Key.AsString();
                var val = prop.Value.Value;
                if (val.IsUndefined()) continue;

                switch (key)
                {
                    case "key":
                        node.Key = val.ToString();
                        break;
                    case "style":
                        inlineStyle = ConvertStyle(val);
                        break;
                    case "className":
                        classStyle = ResolveClassName(val.ToString());
                        break;
                    case "onClick":
                        node.Props["onClick"] = WrapAction(val);
                        break;
                    case "onChange":
                        node.Props["onChange"] = WrapStringAction(val);
                        break;
                    case "value":
                        node.Props["value"] = val.ToObject();
                        break;
                    case "placeholder":
                        node.Props["placeholder"] = val.ToString();
                        break;
                    case "label":
                        if (typeStr == "button")
                            node.TextContent = val.ToString();
                        else
                            node.Props["label"] = val.ToString();
                        break;
                    case "min":
                        node.Props["min"] = (float)val.AsNumber();
                        break;
                    case "max":
                        node.Props["max"] = (float)val.AsNumber();
                        break;
                    default:
                        node.Props[key] = val.ToObject();
                        break;
                }
            }
        }

        // Merge className + inline style (inline wins over class, like CSS specificity)
        if (classStyle != null && inlineStyle != null)
            node.Style = classStyle.Merge(inlineStyle);
        else if (classStyle != null)
            node.Style = classStyle;
        else if (inlineStyle != null)
            node.Style = inlineStyle;

        // Process children from the collected array
        var childList = new List<VNode>();
        if (!childrenArray.IsNull() && !childrenArray.IsUndefined() && childrenArray.IsArray())
        {
            var arrObj = childrenArray.AsObject();
            var length = (int)arrObj.Get("length").AsNumber();
            for (int ci = 0; ci < length; ci++)
                FlattenChild(arrObj.Get(ci.ToString()), childList);
        }

        if (childList.Count > 0)
            node.Children = childList.ToArray();

        // Handle button with single text child
        if (typeStr == "button" && node.TextContent == null && childList.Count == 1 &&
            childList[0].Type == "text" && childList[0].TextContent != null)
        {
            node.TextContent = childList[0].TextContent;
            node.Children = null;
        }

        return JsValue.FromObject(_engine, node);
    }

    private void FlattenChild(JsValue child, List<VNode> list)
    {
        if (child.IsNull() || child.IsUndefined()) return;
        if (child.IsBoolean() && !child.AsBoolean()) return;

        // Array — flatten recursively (from .map())
        if (child.IsArray())
        {
            var arrObj = child.AsObject();
            var length = (int)arrObj.Get("length").AsNumber();
            for (int i = 0; i < length; i++)
                FlattenChild(arrObj.Get(i.ToString()), list);
            return;
        }

        // VNode (wrapped CLR object)
        var obj = child.ToObject();
        if (obj is VNode vnode)
        {
            list.Add(vnode);
            return;
        }

        // String or number → text node
        var text = child.ToString();
        if (!string.IsNullOrEmpty(text))
            list.Add(new VNode("text") { TextContent = text });
    }

    // ─── Style conversion ────────────────────────

    private ReactUI.Style.Style ConvertStyle(JsValue val)
    {
        if (!val.IsObject()) return new ReactUI.Style.Style();

        var obj = val.AsObject();
        var dict = new Dictionary<string, object?>();
        foreach (var prop in obj.GetOwnProperties())
        {
            string key = prop.Key.AsString();
            dict[key] = ConvertJsValueToObject(prop.Value.Value);
        }

        return StyleConverter.Convert(dict);
    }

    private object? ConvertJsValueToObject(JsValue val)
    {
        if (val.IsNull() || val.IsUndefined()) return null;
        if (val.IsBoolean()) return val.AsBoolean();
        if (val.IsNumber()) return val.AsNumber();
        if (val.IsString()) return val.AsString();

        if (val.IsArray())
        {
            var arrObj = val.AsObject();
            var length = (int)arrObj.Get("length").AsNumber();
            var list = new List<object?>();
            for (int i = 0; i < length; i++)
                list.Add(ConvertJsValueToObject(arrObj.Get(i.ToString())));
            return list;
        }

        if (val.IsObject())
        {
            var obj = val.AsObject();
            var dict = new Dictionary<string, object?>();
            foreach (var prop in obj.GetOwnProperties())
                dict[prop.Key.AsString()] = ConvertJsValueToObject(prop.Value.Value);
            return dict;
        }

        return val.ToObject();
    }

    private ReactUI.Style.Style ResolveClassName(string className)
    {
        if (ComponentStyles != null)
        {
            var local = ComponentStyles.Resolve(className);
            // Check if it has any content
            if (local.FontSize.HasValue || local.Background.HasValue || local.Padding.HasValue ||
                local.FlexDirection.HasValue || local.Color.HasValue || local.BorderRadius.HasValue)
                return local;
        }
        return GlobalStyles.Resolve(className);
    }

    // ─── Hooks ────────────────────────

    private JsValue UseState(JsValue initialValue)
    {
        var initial = initialValue.ToObject();
        var (value, setter) = UseStateHook.UseState(initial);

        var jsSetter = new Action<JsValue>(jsVal => setter(jsVal.ToObject()));

        var arr = new JsValue[]
        {
            JsValue.FromObject(_engine, value),
            JsValue.FromObject(_engine, jsSetter)
        };
        return _engine.Intrinsics.Array.Construct(arr);
    }

    private void UseEffect(JsValue fn, JsValue deps)
    {
        if (!fn.IsObject()) return;

        object[]? depArray = null;
        if (!deps.IsNull() && !deps.IsUndefined() && deps.IsArray())
        {
            var arrObj = deps.AsObject();
            var length = (int)arrObj.Get("length").AsNumber();
            depArray = new object[length];
            for (int i = 0; i < length; i++)
                depArray[i] = arrObj.Get(i.ToString()).ToObject()!;
        }

        // Capture the fn as a JsValue we can call later
        var fnRef = fn;
        UseEffectHook.UseEffect(() =>
        {
            try
            {
                var result = _engine.Invoke(fnRef);
                if (result.IsObject())
                {
                    var cleanupRef = result;
                    return () => { try { _engine.Invoke(cleanupRef); } catch { } };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Effect error: {ex.Message}");
            }
            return null;
        }, depArray);
    }

    private JsValue UseMemo(JsValue fn, JsValue deps)
    {
        if (!fn.IsObject()) return JsValue.Undefined;

        object[]? depArray = null;
        if (!deps.IsNull() && !deps.IsUndefined() && deps.IsArray())
        {
            var arrObj = deps.AsObject();
            var length = (int)arrObj.Get("length").AsNumber();
            depArray = new object[length];
            for (int i = 0; i < length; i++)
                depArray[i] = arrObj.Get(i.ToString()).ToObject()!;
        }

        var fnRef = fn;
        var result = UseMemoHook.UseMemo(() =>
        {
            try { return _engine.Invoke(fnRef).ToObject(); }
            catch { return null; }
        }, depArray);

        return JsValue.FromObject(_engine, result);
    }

    private JsValue UseRef(JsValue initialValue)
    {
        var r = UseRefHook.UseRef(initialValue.ToObject());
        var refObj = new JsObject(_engine);
        refObj.FastSetDataProperty("current", JsValue.FromObject(_engine, r.Current));
        return refObj;
    }

    private JsValue UseCallback(JsValue fn, JsValue deps)
    {
        if (!fn.IsObject()) return JsValue.Undefined;

        object[]? depArray = null;
        if (!deps.IsNull() && !deps.IsUndefined() && deps.IsArray())
        {
            var arrObj = deps.AsObject();
            var length = (int)arrObj.Get("length").AsNumber();
            depArray = new object[length];
            for (int i = 0; i < length; i++)
                depArray[i] = arrObj.Get(i.ToString()).ToObject()!;
        }

        var fnRef = fn;
        var result = UseCallbackHook.UseCallback(() =>
        {
            try { _engine.Invoke(fnRef); } catch { }
        }, depArray);

        return JsValue.FromObject(_engine, result);
    }

    // ─── useDrag ────────────────────────

    /// <summary>
    /// useDrag(initialX, initialY) → { x, y, setX, setY }
    /// Registers the component as draggable. Returns current position and setters.
    /// </summary>
    private JsValue UseDrag(JsValue initialX, JsValue initialY)
    {
        var ix = (float)initialX.AsNumber();
        var iy = (float)initialY.AsNumber();

        var (posX, setPosX) = UseStateHook.UseState(ix);
        var (posY, setPosY) = UseStateHook.UseState(iy);

        var capturedX = Convert.ToSingle(posX ?? 0);
        var capturedY = Convert.ToSingle(posY ?? 0);

        var ctx = HooksRuntime.Current;
        if (ctx != null)
        {
            Input.InputSystem.RegisterDraggable(
                ctx.ComponentId,
                () => capturedX, () => capturedY,
                v => setPosX(v), v => setPosY(v));
        }

        var result = new JsObject(_engine);
        result.FastSetDataProperty("x", JsValue.FromObject(_engine, capturedX));
        result.FastSetDataProperty("y", JsValue.FromObject(_engine, capturedY));
        result.FastSetDataProperty("setX", JsValue.FromObject(_engine, new Action<JsValue>(v => setPosX((float)v.AsNumber()))));
        result.FastSetDataProperty("setY", JsValue.FromObject(_engine, new Action<JsValue>(v => setPosY((float)v.AsNumber()))));
        return result;
    }

    // ─── Shared state (C# ↔ JSX) ────────────────────────

    private JsValue UseShared(JsValue keyVal)
    {
        var key = keyVal.AsString();

        // Subscribe this component to the shared key so C# updates trigger re-renders
        var ctx = HooksRuntime.Current;
        if (ctx != null)
            JsxInterop.Subscribe(key, ctx.ComponentId);

        var value = JsxInterop.Get(key);
        return value != null ? JsValue.FromObject(_engine, value) : JsValue.Null;
    }

    private void BuildCSharpObject(Engine engine)
    {
        var csharpObj = new JsObject(engine);

        foreach (var (name, func) in JsxInterop.ExposedFunctions)
        {
            var f = func; // capture
            csharpObj.FastSetDataProperty(name, JsValue.FromObject(engine, new Func<JsValue[], JsValue>(args =>
            {
                var csharpArgs = args.Select(a => a.ToObject()).ToArray();
                try
                {
                    var result = f.DynamicInvoke(csharpArgs);
                    return result != null ? JsValue.FromObject(engine, result) : JsValue.Undefined;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] csharp.{name}() error: {ex.Message}");
                    return JsValue.Undefined;
                }
            })));
        }

        // Also expose registered actions on the csharp object
        foreach (var (name, action) in _registeredActions)
        {
            var a = action;
            csharpObj.FastSetDataProperty(name, JsValue.FromObject(engine, new Func<JsValue[], JsValue>(args =>
            {
                var csharpArgs = args.Select(x => x.ToObject()).ToArray();
                try
                {
                    var result = a.DynamicInvoke(csharpArgs);
                    return result != null ? JsValue.FromObject(engine, result) : JsValue.Undefined;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] csharp.{name}() error: {ex.Message}");
                    return JsValue.Undefined;
                }
            })));
        }

        engine.SetValue("csharp", csharpObj);
    }

    // ─── Utilities ────────────────────────

    private void Log(JsValue msg) =>
        System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] {msg}");

    private JsValue CallAction(string name, JsValue[] args)
    {
        if (_registeredActions.TryGetValue(name, out var action))
        {
            var csharpArgs = args.Select(a => a.ToObject()).ToArray();
            try { return JsValue.FromObject(_engine, action.DynamicInvoke(csharpArgs)); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Action '{name}' error: {ex.Message}");
            }
        }
        return JsValue.Undefined;
    }

    private Action WrapAction(JsValue fn)
    {
        if (!fn.IsObject()) return () => { };
        var fnRef = fn;
        return () =>
        {
            try { _engine.Invoke(fnRef); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] onClick error: {ex.Message}");
            }
        };
    }

    private Action<string> WrapStringAction(JsValue fn)
    {
        if (!fn.IsObject()) return _ => { };
        var fnRef = fn;
        return val =>
        {
            try { _engine.Invoke(fnRef, val); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] onChange error: {ex.Message}");
            }
        };
    }

    private VNode? UnwrapVNode(JsValue val)
    {
        if (val.IsNull() || val.IsUndefined()) return null;
        return val.ToObject() as VNode;
    }
}
