using System;
using System.Collections.Generic;

namespace ReactUI.Core;

/// <summary>
/// Factory for creating component wrappers that enable hooks.
/// Each call to Component() allocates a unique component ID.
/// The returned Func produces a VNode of type "__component" that the Reconciler
/// knows to invoke with the hooks runtime active.
/// </summary>
public static class ComponentFactory
{
    private static int _nextId = 1000; // Start above 0 to avoid collision with root IDs

    /// <summary>
    /// Wraps a parameterless render function as a component (enables hooks).
    /// Usage: var MyPanel = ComponentFactory.Component(() => { ... return vnode; });
    /// Then call MyPanel() to get the VNode.
    /// </summary>
    public static Func<VNode> Component(Func<VNode> render)
    {
        var id = _nextId++;
        return () =>
        {
            var node = new VNode("__component")
            {
                ComponentId = id,
                RenderFunc = render,
            };
            return node;
        };
    }

    /// <summary>
    /// Wraps a render function that takes props as a component.
    /// Usage: var MyButton = ComponentFactory.Component&lt;ButtonProps&gt;((props) => { ... return vnode; });
    /// Then call MyButton(new ButtonProps { ... }) to get the VNode.
    ///
    /// IL2CPP note: The props are boxed as object internally. The Func&lt;TProps, VNode&gt; is stored
    /// as a Func&lt;object, VNode&gt; wrapper to avoid generic virtual dispatch issues under IL2CPP.
    /// </summary>
    // [Preserve] — add Il2CppInterop.Runtime.Attributes.Preserve if available to prevent stripping
    public static Func<TProps, VNode> Component<TProps>(Func<TProps, VNode> render)
    {
        var id = _nextId++;
        return (props) =>
        {
            // Create a wrapped render func that captures the props — IL2CPP safe (no Reflection.Emit)
            var capturedProps = props;
            var capturedRender = render;
            Func<VNode> boundRender = () => capturedRender(capturedProps);

            var node = new VNode("__component")
            {
                ComponentId = id,
                RenderFunc = boundRender,
            };

            // Also store the original render and props for reconciliation (so re-render with new props works)
            node.Props["__render"] = render;
            node.Props["__props"] = props;
            node.Props["__renderWrapped"] = boundRender;

            return node;
        };
    }
}
