using System;
using System.Collections.Generic;
using ReactUI.Hooks;

namespace ReactUI.Core;

/// <summary>
/// Standard React-style virtual DOM reconciler with keyed child diffing.
/// Diffs VNode trees and patches the committed UINode tree in-place.
/// </summary>
public static class Reconciler
{
    /// <summary>
    /// Main reconciliation entry point.
    /// Compares oldVNode vs newVNode and creates/updates/destroys UINodes under parent.
    /// </summary>
    /// <param name="parent">Parent UINode (null only for root).</param>
    /// <param name="oldVNode">Previous VNode (null on first mount).</param>
    /// <param name="newVNode">New VNode (null means remove).</param>
    /// <param name="existingNode">The existing UINode that corresponds to oldVNode (null if none).</param>
    /// <param name="index">Child index within parent.</param>
    /// <returns>The UINode for this position (null if destroyed).</returns>
    public static UINode? Reconcile(UINode? parent, VNode? oldVNode, VNode? newVNode, UINode? existingNode, int index)
    {
        // Case 1: CREATE — no old node, new node provided
        if (oldVNode == null && newVNode != null)
        {
            var node = CreateNode(newVNode, parent);
            if (parent != null)
            {
                if (index >= parent.Children.Count)
                    parent.Children.Add(node);
                else
                    parent.Children.Insert(index, node);
            }
            return node;
        }

        // Case 2: DESTROY — old node exists, new node is null
        if (newVNode == null && existingNode != null)
        {
            DestroyNode(existingNode);
            return null;
        }

        // Both null — nothing to do
        if (oldVNode == null || newVNode == null || existingNode == null)
            return null;

        // Case 3: Type changed (or different component) → destroy old, create new
        bool typeChanged = oldVNode.Type != newVNode.Type;
        bool componentChanged = oldVNode.Type == "__component" && newVNode.Type == "__component"
                                && oldVNode.ComponentId != newVNode.ComponentId;
        if (typeChanged || componentChanged)
        {
            var idx = parent != null ? parent.Children.IndexOf(existingNode) : index;
            DestroyNode(existingNode);
            var node = CreateNode(newVNode, parent);
            if (parent != null)
            {
                if (idx >= 0 && idx < parent.Children.Count)
                    parent.Children.Insert(idx, node);
                else
                    parent.Children.Add(node);
            }
            return node;
        }

        // Case 4: Same type → update in place
        UpdateNode(existingNode, newVNode);

        // Component nodes manage their own children via ReRenderComponent (called from UpdateNode).
        // Don't DiffChildren here — VNode.Children is null for components.
        if (newVNode.Type != "__component")
        {
            var oldChildren = oldVNode.Children ?? Array.Empty<VNode>();
            var newChildren = newVNode.Children ?? Array.Empty<VNode>();
            DiffChildren(existingNode, oldChildren, newChildren);
        }

        return existingNode;
    }

    /// <summary>
    /// Creates a new UINode from a VNode, recursively creating children.
    /// For component VNodes, invokes the render function with hooks context.
    /// </summary>
    public static UINode CreateNode(VNode vnode, UINode? parent)
    {
        // Handle component nodes
        if (vnode.Type == "__component" && vnode.RenderFunc != null)
        {
            return CreateComponentNode(vnode, parent);
        }

        // Handle component nodes with props-based render
        if (vnode.Type == "__component" && vnode.Props.ContainsKey("__render"))
        {
            return CreatePropsComponentNode(vnode, parent);
        }

        var node = new UINode(vnode.Type)
        {
            Key = vnode.Key,
            ComputedStyle = vnode.Style ?? new Style.Style(),
            Parent = parent,
            ComponentId = vnode.ComponentId,
            LastVNode = vnode,
        };

        // Recursively create children
        if (vnode.Children != null)
        {
            foreach (var childVNode in vnode.Children)
            {
                var childNode = CreateNode(childVNode, node);
                node.Children.Add(childNode);
            }
        }

        return node;
    }

    /// <summary>
    /// Creates a UINode for a component VNode (one with RenderFunc).
    /// Sets up hooks context, invokes the render, and reconciles the output.
    /// </summary>
    private static UINode CreateComponentNode(VNode vnode, UINode? parent)
    {
        var componentId = vnode.ComponentId;

        // Begin hooks context for this component
        HooksRuntime.BeginComponent(componentId);
        VNode rendered;
        try
        {
            rendered = vnode.RenderFunc!();
        }
        finally
        {
            HooksRuntime.EndComponent();
        }

        // Create a wrapper UINode to hold the component's identity and hook context
        var node = new UINode(vnode.Type)
        {
            Key = vnode.Key,
            Parent = parent,
            ComponentId = componentId,
            HookContext = HooksRuntime.GetContext(componentId),
            LastVNode = vnode,
        };

        // Recursively create the rendered subtree as this component's single child
        if (rendered != null)
        {
            var childNode = CreateNode(rendered, node);
            node.Children.Add(childNode);
        }

        // Register tree for scheduler
        Scheduler.RegisterComponentTree(componentId, vnode, node);

        return node;
    }

    /// <summary>
    /// Creates a UINode for a props-based component VNode (created via Component&lt;TProps&gt;).
    /// </summary>
    private static UINode CreatePropsComponentNode(VNode vnode, UINode? parent)
    {
        var componentId = vnode.ComponentId;

        // Build the render func from props
        // The __render prop is a Func<TProps, VNode> stored as object
        // The __props prop is the TProps value stored as object
        // We can't invoke generically under IL2CPP, so we store a wrapped Func<VNode> at call site
        // For now, we check if there's a __renderWrapped that was pre-bound
        Func<VNode>? renderFunc = null;
        if (vnode.Props.TryGetValue("__renderWrapped", out var wrapped) && wrapped is Func<VNode> fn)
        {
            renderFunc = fn;
        }
        else if (vnode.Props.TryGetValue("__render", out var renderObj) && vnode.Props.TryGetValue("__props", out var propsObj))
        {
            // Store a pre-bound version for future reconciliation
            // IL2CPP safe: we capture the boxed delegate and invoke via known interface
            var render = renderObj;
            var props = propsObj;
            // We need the caller to have wrapped this — fall back to invoking as Func<object, VNode>
            if (render is Func<object, VNode> objRender)
            {
                renderFunc = () => objRender(props!);
            }
        }

        if (renderFunc == null)
        {
            // Fallback: create an empty node
            return new UINode("__component") { Key = vnode.Key, Parent = parent, ComponentId = componentId, LastVNode = vnode };
        }

        HooksRuntime.BeginComponent(componentId);
        VNode rendered;
        try
        {
            rendered = renderFunc();
        }
        finally
        {
            HooksRuntime.EndComponent();
        }

        var node = new UINode(vnode.Type)
        {
            Key = vnode.Key,
            Parent = parent,
            ComponentId = componentId,
            HookContext = HooksRuntime.GetContext(componentId),
            LastVNode = vnode,
        };

        if (rendered != null)
        {
            var childNode = CreateNode(rendered, node);
            node.Children.Add(childNode);
        }

        Scheduler.RegisterComponentTree(componentId, vnode, node);

        return node;
    }

    /// <summary>
    /// Destroys a UINode: removes it from parent, cleans up hook contexts, runs effect cleanups.
    /// Recurses into children depth-first.
    /// </summary>
    public static void DestroyNode(UINode node)
    {
        // Depth-first destroy children
        for (int i = node.Children.Count - 1; i >= 0; i--)
        {
            DestroyNode(node.Children[i]);
        }
        node.Children.Clear();

        // Clean up hooks (runs effect cleanups)
        if (node.HookContext != null)
        {
            // Run any pending effect cleanups
            foreach (var slot in node.HookContext.StateSlots)
            {
                if (slot is EffectState effectState && effectState.Cleanup != null)
                {
                    try { effectState.Cleanup(); }
                    catch (Exception) { /* swallow cleanup errors */ }
                }
            }
            HooksRuntime.RemoveContext(node.ComponentId);
            node.HookContext = null;
        }

        // Unregister from scheduler
        Scheduler.UnregisterComponentTree(node.ComponentId);

        // Remove from parent
        node.Parent?.Children.Remove(node);
        node.Parent = null;
    }

    /// <summary>
    /// Updates an existing UINode's style and props from a new VNode (same type).
    /// </summary>
    public static void UpdateNode(UINode node, VNode newVNode)
    {
        node.Key = newVNode.Key;
        node.ComputedStyle = newVNode.Style ?? new Style.Style();
        node.ComponentId = newVNode.ComponentId;
        node.LastVNode = newVNode;

        // If this is a component node, re-render it
        if (newVNode.Type == "__component")
        {
            ReRenderComponent(node, newVNode);
        }
    }

    /// <summary>
    /// Re-renders a component node: invokes its render function and reconciles the output.
    /// </summary>
    private static void ReRenderComponent(UINode node, VNode newVNode)
    {
        Func<VNode>? renderFunc = newVNode.RenderFunc;

        // Check for props-based render
        if (renderFunc == null)
        {
            if (newVNode.Props.TryGetValue("__renderWrapped", out var wrapped) && wrapped is Func<VNode> fn)
                renderFunc = fn;
            else if (newVNode.Props.TryGetValue("__render", out var renderObj) &&
                     newVNode.Props.TryGetValue("__props", out var propsObj) &&
                     renderObj is Func<object, VNode> objRender)
            {
                var props = propsObj;
                renderFunc = () => objRender(props!);
            }
        }

        if (renderFunc == null) return;

        var componentId = newVNode.ComponentId;
        HooksRuntime.BeginComponent(componentId);
        VNode rendered;
        try
        {
            rendered = renderFunc();
        }
        finally
        {
            HooksRuntime.EndComponent();
        }

        node.HookContext = HooksRuntime.GetContext(componentId);

        // Reconcile the single child
        var oldChild = node.Children.Count > 0 ? node.Children[0] : null;
        var oldChildVNode = oldChild?.LastVNode;

        if (rendered != null)
        {
            var result = Reconcile(node, oldChildVNode, rendered, oldChild, 0);
            // Ensure the child list is correct
            if (oldChild == null && result != null)
            {
                // Already added by Reconcile
            }
        }
        else if (oldChild != null)
        {
            DestroyNode(oldChild);
        }

        Scheduler.RegisterComponentTree(componentId, newVNode, node);
    }

    /// <summary>
    /// Diffs two arrays of child VNodes and patches the parent's UINode children.
    /// Uses keyed reconciliation when keys are present, index-based fallback otherwise.
    /// </summary>
    private static void DiffChildren(UINode parent, VNode[] oldChildren, VNode[] newChildren)
    {
        // Check if any children have keys
        bool hasKeys = false;
        foreach (var c in newChildren)
        {
            if (c.Key != null) { hasKeys = true; break; }
        }
        if (!hasKeys)
        {
            foreach (var c in oldChildren)
            {
                if (c.Key != null) { hasKeys = true; break; }
            }
        }

        if (hasKeys)
            DiffChildrenKeyed(parent, oldChildren, newChildren);
        else
            DiffChildrenIndexed(parent, oldChildren, newChildren);
    }

    /// <summary>
    /// Index-based child diffing: compares children at matching indices.
    /// </summary>
    private static void DiffChildrenIndexed(UINode parent, VNode[] oldChildren, VNode[] newChildren)
    {
        int maxLen = Math.Max(oldChildren.Length, newChildren.Length);

        // We need to be careful about indices shifting during removal.
        // Process in order, adjusting for the current children list state.
        var resultChildren = new List<UINode>(newChildren.Length);

        for (int i = 0; i < maxLen; i++)
        {
            var oldVNode = i < oldChildren.Length ? oldChildren[i] : null;
            var newVNode = i < newChildren.Length ? newChildren[i] : null;
            var existingNode = i < parent.Children.Count ? parent.Children[i] : null;

            // Temporarily detach from parent to avoid Reconcile's insert logic conflicting
            // We'll rebuild the children list afterwards
            var result = ReconcileDetached(parent, oldVNode, newVNode, existingNode);
            if (result != null)
            {
                result.Parent = parent;
                resultChildren.Add(result);
            }
        }

        // Replace children list
        parent.Children.Clear();
        parent.Children.AddRange(resultChildren);
    }

    /// <summary>
    /// Keyed child diffing: uses a map of old keys to reuse/reorder nodes efficiently.
    /// </summary>
    private static void DiffChildrenKeyed(UINode parent, VNode[] oldChildren, VNode[] newChildren)
    {
        // Build map of old keyed children
        var oldKeyMap = new Dictionary<string, (VNode vnode, UINode? uiNode)>();
        var oldUnkeyed = new List<(VNode vnode, UINode? uiNode)>();

        for (int i = 0; i < oldChildren.Length; i++)
        {
            var oldVNode = oldChildren[i];
            var uiNode = i < parent.Children.Count ? parent.Children[i] : null;

            if (oldVNode.Key != null)
                oldKeyMap[oldVNode.Key] = (oldVNode, uiNode);
            else
                oldUnkeyed.Add((oldVNode, uiNode));
        }

        var resultChildren = new List<UINode>(newChildren.Length);
        var usedOldKeys = new HashSet<string>();
        int unkeyedIndex = 0;

        for (int i = 0; i < newChildren.Length; i++)
        {
            var newVNode = newChildren[i];

            if (newVNode.Key != null && oldKeyMap.TryGetValue(newVNode.Key, out var oldEntry))
            {
                // Matched by key — reconcile
                usedOldKeys.Add(newVNode.Key);
                var result = ReconcileDetached(parent, oldEntry.vnode, newVNode, oldEntry.uiNode);
                if (result != null)
                {
                    result.Parent = parent;
                    resultChildren.Add(result);
                }
            }
            else if (newVNode.Key == null && unkeyedIndex < oldUnkeyed.Count)
            {
                // Unkeyed fallback
                var oldEntry2 = oldUnkeyed[unkeyedIndex++];
                var result = ReconcileDetached(parent, oldEntry2.vnode, newVNode, oldEntry2.uiNode);
                if (result != null)
                {
                    result.Parent = parent;
                    resultChildren.Add(result);
                }
            }
            else
            {
                // No match — create new
                var node = CreateNode(newVNode, parent);
                resultChildren.Add(node);
            }
        }

        // Destroy old keyed nodes that weren't reused
        foreach (var kvp in oldKeyMap)
        {
            if (!usedOldKeys.Contains(kvp.Key) && kvp.Value.uiNode != null)
            {
                DestroyNode(kvp.Value.uiNode);
            }
        }

        // Destroy remaining old unkeyed nodes
        for (int i = unkeyedIndex; i < oldUnkeyed.Count; i++)
        {
            if (oldUnkeyed[i].uiNode != null)
                DestroyNode(oldUnkeyed[i].uiNode);
        }

        parent.Children.Clear();
        parent.Children.AddRange(resultChildren);
    }

    /// <summary>
    /// Reconcile without modifying parent.Children directly — returns the resulting node.
    /// Used by DiffChildren implementations that rebuild the children list themselves.
    /// </summary>
    private static UINode? ReconcileDetached(UINode parent, VNode? oldVNode, VNode? newVNode, UINode? existingNode)
    {
        // CREATE
        if (oldVNode == null && newVNode != null)
        {
            return CreateNode(newVNode, parent);
        }

        // DESTROY
        if (newVNode == null && existingNode != null)
        {
            // Detach from parent first to avoid Remove during iteration
            existingNode.Parent = null;
            DestroyNodeWithoutRemove(existingNode);
            return null;
        }

        if (oldVNode == null || newVNode == null || existingNode == null)
            return null;

        // Type changed (or different component) → destroy old, create new
        bool typeChanged = oldVNode.Type != newVNode.Type;
        bool componentChanged = oldVNode.Type == "__component" && newVNode.Type == "__component"
                                && oldVNode.ComponentId != newVNode.ComponentId;
        if (typeChanged || componentChanged)
        {
            existingNode.Parent = null;
            DestroyNodeWithoutRemove(existingNode);
            return CreateNode(newVNode, parent);
        }

        // Same type → update in place
        UpdateNode(existingNode, newVNode);

        // Component nodes manage their own children via ReRenderComponent (called from UpdateNode).
        // Don't DiffChildren here — VNode.Children is null for components.
        if (newVNode.Type != "__component")
        {
            var oldCh = oldVNode.Children ?? Array.Empty<VNode>();
            var newCh = newVNode.Children ?? Array.Empty<VNode>();
            DiffChildren(existingNode, oldCh, newCh);
        }

        return existingNode;
    }

    /// <summary>
    /// Destroys a node without removing it from parent.Children (caller manages the list).
    /// </summary>
    private static void DestroyNodeWithoutRemove(UINode node)
    {
        for (int i = node.Children.Count - 1; i >= 0; i--)
        {
            DestroyNode(node.Children[i]);
        }
        node.Children.Clear();

        if (node.HookContext != null)
        {
            foreach (var slot in node.HookContext.StateSlots)
            {
                if (slot is EffectState effectState && effectState.Cleanup != null)
                {
                    try { effectState.Cleanup(); }
                    catch (Exception) { /* swallow */ }
                }
            }
            HooksRuntime.RemoveContext(node.ComponentId);
            node.HookContext = null;
        }

        Scheduler.UnregisterComponentTree(node.ComponentId);
    }
}
