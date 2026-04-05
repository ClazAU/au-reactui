using System;
using System.Linq;
using Xunit;
using ReactUI.Core;
using ReactUI.Hooks;

namespace ReactUI.Tests;

[Collection("SharedState")]
public class ReconcilerTests : IDisposable
{
    public ReconcilerTests()
    {
        Scheduler.Reset();
        HooksRuntime.Reset();
    }

    public void Dispose()
    {
        Scheduler.Reset();
        HooksRuntime.Reset();
    }

    // ── Helpers ──────────────────────────────────

    static VNode V(string type, params VNode[] children) =>
        new(type) { Children = children.Length > 0 ? children : null };

    static VNode VK(string type, string key, params VNode[] children) =>
        new(type) { Key = key, Children = children.Length > 0 ? children : null };

    // ──────────────────────────────────────────────
    //  CreateNode
    // ──────────────────────────────────────────────

    [Fact]
    public void CreateNode_CreatesUINodeWithMatchingType()
    {
        var vnode = V("div");
        var node = Reconciler.CreateNode(vnode, null);

        Assert.Equal("div", node.Type);
    }

    [Fact]
    public void CreateNode_RecursivelyCreatesChildren()
    {
        var vnode = V("div", V("span"), V("text"));
        var node = Reconciler.CreateNode(vnode, null);

        Assert.Equal(2, node.Children.Count);
        Assert.Equal("span", node.Children[0].Type);
        Assert.Equal("text", node.Children[1].Type);
    }

    [Fact]
    public void CreateNode_SetsParentReferences()
    {
        var vnode = V("div", V("span"));
        var node = Reconciler.CreateNode(vnode, null);

        Assert.Null(node.Parent);
        Assert.Same(node, node.Children[0].Parent);
    }

    [Fact]
    public void CreateNode_ComponentNode_InvokesRenderAndCreatesSubtree()
    {
        int renderCount = 0;
        var compVNode = new VNode("__component")
        {
            ComponentId = 100,
            RenderFunc = () =>
            {
                renderCount++;
                return V("div", V("span"));
            }
        };

        var node = Reconciler.CreateNode(compVNode, null);

        Assert.Equal(1, renderCount);
        Assert.Equal("__component", node.Type);
        Assert.Single(node.Children);             // single child from render
        Assert.Equal("div", node.Children[0].Type);
        Assert.Single(node.Children[0].Children); // grandchild
        Assert.Equal("span", node.Children[0].Children[0].Type);
    }

    [Fact]
    public void CreateNode_ComponentNode_SetsUpHookContext()
    {
        var compVNode = new VNode("__component")
        {
            ComponentId = 200,
            RenderFunc = () =>
            {
                UseStateHook.UseState(0);
                return V("div");
            }
        };

        var node = Reconciler.CreateNode(compVNode, null);

        Assert.NotNull(node.HookContext);
        Assert.Equal(200, node.HookContext!.ComponentId);
        Assert.Single(node.HookContext.StateSlots); // one useState slot
    }

    // ──────────────────────────────────────────────
    //  DestroyNode
    // ──────────────────────────────────────────────

    [Fact]
    public void DestroyNode_RemovesFromParentChildren()
    {
        var parent = Reconciler.CreateNode(V("div", V("span")), null);
        Assert.Single(parent.Children);

        var child = parent.Children[0];
        Reconciler.DestroyNode(child);

        Assert.Empty(parent.Children);
    }

    [Fact]
    public void DestroyNode_RecursivelyDestroysChildren()
    {
        var vnode = V("div", V("a", V("b")));
        var node = Reconciler.CreateNode(vnode, null);
        var a = node.Children[0];
        var b = a.Children[0];

        Reconciler.DestroyNode(node);

        Assert.Empty(node.Children);
        Assert.Empty(a.Children);
        Assert.Null(b.Parent);
    }

    [Fact]
    public void DestroyNode_CleansUpHookContext()
    {
        var compVNode = new VNode("__component")
        {
            ComponentId = 300,
            RenderFunc = () =>
            {
                UseStateHook.UseState(0);
                return V("div");
            }
        };

        var node = Reconciler.CreateNode(compVNode, null);
        Assert.NotNull(node.HookContext);

        Reconciler.DestroyNode(node);

        Assert.Null(node.HookContext);
        Assert.Null(HooksRuntime.GetContext(300));
    }

    [Fact]
    public void DestroyNode_RunsEffectCleanupFunctions()
    {
        int cleanupRan = 0;

        var compVNode = new VNode("__component")
        {
            ComponentId = 400,
            RenderFunc = () =>
            {
                UseEffectHook.UseEffect(() =>
                {
                    return () => { cleanupRan++; };
                }, Array.Empty<object>());
                return V("div");
            }
        };

        var node = Reconciler.CreateNode(compVNode, null);
        Scheduler.FlushEffects(); // run the effect so cleanup gets stored

        Reconciler.DestroyNode(node);

        Assert.Equal(1, cleanupRan);
    }

    // ──────────────────────────────────────────────
    //  Reconcile
    // ──────────────────────────────────────────────

    [Fact]
    public void Reconcile_NullToNew_CreatesNode()
    {
        var parent = Reconciler.CreateNode(V("root"), null);
        var newVNode = V("child");

        var result = Reconciler.Reconcile(parent, null, newVNode, null, 0);

        Assert.NotNull(result);
        Assert.Equal("child", result!.Type);
        Assert.Single(parent.Children);
    }

    [Fact]
    public void Reconcile_OldToNull_DestroysNode()
    {
        var parent = Reconciler.CreateNode(V("root", V("child")), null);
        var child = parent.Children[0];
        var oldVNode = child.LastVNode;

        var result = Reconciler.Reconcile(parent, oldVNode, null, child, 0);

        Assert.Null(result);
        Assert.Empty(parent.Children);
    }

    [Fact]
    public void Reconcile_SameType_DifferentStyle_UpdatesStyle()
    {
        var oldV = new VNode("div") { Style = new Style.Style { Opacity = 1.0f } };
        var parent = Reconciler.CreateNode(V("root"), null);
        var node = Reconciler.CreateNode(oldV, parent);
        parent.Children.Add(node);

        var newV = new VNode("div") { Style = new Style.Style { Opacity = 0.5f } };
        var result = Reconciler.Reconcile(parent, oldV, newV, node, 0);

        Assert.Same(node, result); // updated in place
        Assert.Equal(0.5f, result!.ComputedStyle.Opacity);
    }

    [Fact]
    public void Reconcile_DifferentType_DestroysOldCreatesNew()
    {
        var oldV = V("div");
        var parent = Reconciler.CreateNode(V("root"), null);
        var node = Reconciler.CreateNode(oldV, parent);
        parent.Children.Add(node);

        var newV = V("span");
        var result = Reconciler.Reconcile(parent, oldV, newV, node, 0);

        Assert.NotNull(result);
        Assert.Equal("span", result!.Type);
        Assert.NotSame(node, result); // new object
    }

    [Fact]
    public void Reconcile_ChildrenAdded()
    {
        var oldV = V("div");
        var parent = Reconciler.CreateNode(V("root"), null);
        var node = Reconciler.CreateNode(oldV, parent);
        parent.Children.Add(node);

        var newV = V("div", V("a"), V("b"));
        Reconciler.Reconcile(parent, oldV, newV, node, 0);

        Assert.Equal(2, node.Children.Count);
        Assert.Equal("a", node.Children[0].Type);
        Assert.Equal("b", node.Children[1].Type);
    }

    [Fact]
    public void Reconcile_ChildrenRemoved()
    {
        var oldV = V("div", V("a"), V("b"));
        var parent = Reconciler.CreateNode(V("root"), null);
        var node = Reconciler.CreateNode(oldV, parent);
        parent.Children.Add(node);

        Assert.Equal(2, node.Children.Count);

        var newV = V("div");
        Reconciler.Reconcile(parent, oldV, newV, node, 0);

        Assert.Empty(node.Children);
    }

    [Fact]
    public void Reconcile_KeyedChildren_ReorderPreservesNodes()
    {
        var oldV = V("div", VK("span", "a"), VK("span", "b"), VK("span", "c"));
        var parent = Reconciler.CreateNode(V("root"), null);
        var node = Reconciler.CreateNode(oldV, parent);
        parent.Children.Add(node);

        // Grab references before reconcile
        var nodeA = node.Children.First(c => c.Key == "a");
        var nodeB = node.Children.First(c => c.Key == "b");
        var nodeC = node.Children.First(c => c.Key == "c");

        // Reorder: c, a, b
        var newV = V("div", VK("span", "c"), VK("span", "a"), VK("span", "b"));
        Reconciler.Reconcile(parent, oldV, newV, node, 0);

        Assert.Equal(3, node.Children.Count);
        Assert.Same(nodeC, node.Children[0]);
        Assert.Same(nodeA, node.Children[1]);
        Assert.Same(nodeB, node.Children[2]);
    }

    [Fact]
    public void Reconcile_KeyedChildren_InsertPreservesOthers()
    {
        var oldV = V("div", VK("span", "a"), VK("span", "c"));
        var parent = Reconciler.CreateNode(V("root"), null);
        var node = Reconciler.CreateNode(oldV, parent);
        parent.Children.Add(node);

        var nodeA = node.Children.First(c => c.Key == "a");
        var nodeC = node.Children.First(c => c.Key == "c");

        // Insert "b" between a and c
        var newV = V("div", VK("span", "a"), VK("span", "b"), VK("span", "c"));
        Reconciler.Reconcile(parent, oldV, newV, node, 0);

        Assert.Equal(3, node.Children.Count);
        Assert.Same(nodeA, node.Children[0]);
        Assert.Equal("b", node.Children[1].Key); // newly created
        Assert.Same(nodeC, node.Children[2]);
    }

    [Fact]
    public void Reconcile_KeyedChildren_RemoveDestroysOnlyThatOne()
    {
        var oldV = V("div", VK("span", "a"), VK("span", "b"), VK("span", "c"));
        var parent = Reconciler.CreateNode(V("root"), null);
        var node = Reconciler.CreateNode(oldV, parent);
        parent.Children.Add(node);

        var nodeA = node.Children.First(c => c.Key == "a");
        var nodeC = node.Children.First(c => c.Key == "c");

        // Remove "b"
        var newV = V("div", VK("span", "a"), VK("span", "c"));
        Reconciler.Reconcile(parent, oldV, newV, node, 0);

        Assert.Equal(2, node.Children.Count);
        Assert.Same(nodeA, node.Children[0]);
        Assert.Same(nodeC, node.Children[1]);
    }
}
