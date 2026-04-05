using System;
using System.Collections.Generic;
using ReactUI.Hooks;

namespace ReactUI.Core;

/// <summary>
/// Batched render scheduler. Components mark themselves dirty via ScheduleRender;
/// FlushRenders re-invokes their render functions, reconciles, and triggers layout + repaint.
/// Effects are queued and flushed after the render+paint cycle.
/// </summary>
public static class Scheduler
{
    private static readonly HashSet<int> _dirtyComponents = new();
    private static bool _renderScheduled;
    private static readonly List<Action> _pendingEffects = new();
    private static readonly List<Action> _postLayoutCallbacks = new();
    private static readonly Dictionary<int, ComponentEntry> _componentTrees = new();
    private static readonly List<RenderHandle> _roots = new();
    private static int _nextRootId;

    /// <summary>Internal record of a mounted component's VNode and committed UINode.</summary>
    private class ComponentEntry
    {
        public VNode Tree;
        public UINode Committed;
        public ComponentEntry(VNode tree, UINode committed) { Tree = tree; Committed = committed; }
    }

    /// <summary>
    /// Marks a component as needing re-render. Call this from state setters.
    /// </summary>
    public static void ScheduleRender(int componentId)
    {
        _dirtyComponents.Add(componentId);
        _renderScheduled = true;
    }

    /// <summary>
    /// Marks ALL registered components as dirty. Used by KeyToggle to ensure toggles take effect immediately.
    /// </summary>
    public static void ScheduleRenderAll()
    {
        foreach (var id in _componentTrees.Keys)
            _dirtyComponents.Add(id);
        _renderScheduled = true;
    }

    /// <summary>
    /// Flushes all pending renders. Re-invokes dirty components' render functions,
    /// reconciles against the committed tree, and updates layout.
    /// Call this once per frame from the plugin's Update loop.
    /// </summary>
    private static int _flushDepth;

    public static void FlushRenders()
    {
        if (!_renderScheduled) return;
        _renderScheduled = false;
        _flushDepth++;
        try
        {

        // Snapshot dirty set (components may schedule more renders during reconciliation)
        var dirty = new List<int>(_dirtyComponents);
        _dirtyComponents.Clear();

        foreach (var componentId in dirty)
        {
            if (!_componentTrees.TryGetValue(componentId, out var entry))
                continue;

            var oldVNode = entry.Tree;
            var committedNode = entry.Committed;

            // Re-render: get the render function from the old VNode
            Func<VNode>? renderFunc = oldVNode.RenderFunc;
            if (renderFunc == null)
            {
                // Props-based component
                if (oldVNode.Props.TryGetValue("__renderWrapped", out var wrapped) && wrapped is Func<VNode> fn)
                    renderFunc = fn;
                else if (oldVNode.Props.TryGetValue("__render", out var renderObj) &&
                         oldVNode.Props.TryGetValue("__props", out var propsObj) &&
                         renderObj is Func<object, VNode> objRender)
                {
                    var props = propsObj;
                    renderFunc = () => objRender(props!);
                }
            }

            if (renderFunc == null) continue;

            // Begin hooks context and re-render
            HooksRuntime.BeginComponent(componentId);
            VNode newRendered;
            try
            {
                newRendered = renderFunc();
            }
            finally
            {
                HooksRuntime.EndComponent();
            }

            // Reconcile the component's child
            var oldChild = committedNode.Children.Count > 0 ? committedNode.Children[0] : null;
            var oldChildVNode = oldChild?.LastVNode;

            Reconciler.Reconcile(committedNode, oldChildVNode, newRendered, oldChild, 0);

            // Update the stored VNode (keep the component VNode, but the rendered output changed)
            committedNode.HookContext = HooksRuntime.GetContext(componentId);
        }

        // TODO: Run layout pass here once Layout layer is implemented
        // Layout.FlexLayout.Compute(rootNode, viewportWidth, viewportHeight);

        // After render + layout, flush effects
        FlushEffects();

        // Check if any components became dirty during effect execution (cap depth to prevent stack overflow)
        if (_renderScheduled && _flushDepth < 5)
        {
            FlushRenders();
        }
        }
        finally
        {
            _flushDepth--;
        }
    }

    /// <summary>
    /// Runs all pending useEffect callbacks queued during the last render pass.
    /// </summary>
    public static void FlushEffects()
    {
        if (_pendingEffects.Count == 0) return;

        // Snapshot and clear to allow effects to queue more effects
        var effects = new List<Action>(_pendingEffects);
        _pendingEffects.Clear();

        foreach (var effect in effects)
        {
            try
            {
                effect();
            }
            catch (Exception)
            {
                // Swallow effect errors to prevent one bad effect from blocking others.
                // TODO: add error boundary / logging support
            }
        }
    }

    /// <summary>
    /// Queues an effect to run after the current render+paint cycle.
    /// Called by the UseEffect hook.
    /// </summary>
    public static void QueueEffect(Action effect)
    {
        _pendingEffects.Add(effect);
    }

    // --- Root management ---

    /// <summary>
    /// Returns the root UINodes of all mounted render handles.
    /// Used by the plugin behaviour to drive input and rendering each frame.
    /// </summary>
    /// <summary>Find a UINode by key in all roots.</summary>
    public static UINode? FindNodeByKey(string key)
    {
        foreach (var handle in _roots)
        {
            if (handle.RootNode != null)
            {
                var found = FindByKey(handle.RootNode, key);
                if (found != null) return found;
            }
        }
        return null;
    }

    private static UINode? FindByKey(UINode node, string key)
    {
        if (node.Key == key) return node;
        for (int i = 0; i < node.Children.Count; i++)
        {
            var found = FindByKey(node.Children[i], key);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>Register a callback to run after layout each frame.</summary>
    public static void OnPostLayout(Action callback) => _postLayoutCallbacks.Add(callback);

    /// <summary>Run and clear all post-layout callbacks.</summary>
    public static void FlushPostLayoutCallbacks()
    {
        for (int i = 0; i < _postLayoutCallbacks.Count; i++)
        {
            try { _postLayoutCallbacks[i](); } catch { }
        }
    }

    public static List<UINode> GetRoots()
    {
        var roots = new List<UINode>();
        foreach (var handle in _roots)
        {
            if (handle.RootNode != null)
                roots.Add(handle.RootNode);
        }
        return roots;
    }

    /// <summary>
    /// Mounts a root component. Returns a RenderHandle that can be Disposed to unmount.
    /// </summary>
    public static RenderHandle Mount(Func<VNode> rootComponent)
    {
        var rootId = _nextRootId++;
        var componentVNode = new VNode("__component")
        {
            ComponentId = rootId,
            RenderFunc = rootComponent,
        };

        // Create the committed tree by initial reconciliation
        var rootNode = Reconciler.CreateNode(componentVNode, null);

        var handle = new RenderHandle { RootId = rootId, RootNode = rootNode };
        _roots.Add(handle);

        return handle;
    }

    /// <summary>
    /// Unmounts a root, destroying its entire committed tree.
    /// </summary>
    public static void Unmount(int rootId)
    {
        var handle = _roots.Find(r => r.RootId == rootId);
        if (handle == null) return;

        if (handle.RootNode != null)
        {
            Reconciler.DestroyNode(handle.RootNode);
            handle.RootNode = null;
        }

        _roots.Remove(handle);
    }

    // --- Internal registration (called by Reconciler) ---

    internal static void RegisterComponentTree(int componentId, VNode tree, UINode committed)
    {
        _componentTrees[componentId] = new ComponentEntry(tree, committed);
    }

    internal static void UnregisterComponentTree(int componentId)
    {
        _componentTrees.Remove(componentId);
    }

    /// <summary>
    /// Resets all scheduler state. Useful for testing or full teardown.
    /// </summary>
    public static void Reset()
    {
        _dirtyComponents.Clear();
        _renderScheduled = false;
        _pendingEffects.Clear();
        _componentTrees.Clear();
        foreach (var root in _roots)
        {
            if (root.RootNode != null)
                Reconciler.DestroyNode(root.RootNode);
        }
        _roots.Clear();
        _nextRootId = 0;
    }
}

/// <summary>
/// Handle returned from Scheduler.Mount. Dispose to unmount the root.
/// </summary>
public class RenderHandle : IDisposable
{
    public int RootId;
    public UINode? RootNode;

    public void Dispose()
    {
        Scheduler.Unmount(RootId);
    }
}
