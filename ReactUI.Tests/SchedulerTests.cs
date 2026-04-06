using System;
using Xunit;
using ReactUI.Core;
using ReactUI.Hooks;

namespace ReactUI.Tests;

[Collection("SharedState")]
public class SchedulerTests : IDisposable
{
    public SchedulerTests()
    {
        Scheduler.Reset();
        HooksRuntime.Reset();
    }

    public void Dispose()
    {
        Scheduler.Reset();
        HooksRuntime.Reset();
    }

    // ──────────────────────────────────────────────
    //  Effects
    // ──────────────────────────────────────────────

    [Fact]
    public void QueueEffect_RunsOnFlush()
    {
        int ran = 0;
        Scheduler.QueueEffect(() => ran++);
        Assert.Equal(0, ran);

        Scheduler.FlushEffects();
        Assert.Equal(1, ran);
    }

    [Fact]
    public void QueueEffect_MultipleEffects_AllRunInOrder()
    {
        var order = new System.Collections.Generic.List<int>();
        Scheduler.QueueEffect(() => order.Add(1));
        Scheduler.QueueEffect(() => order.Add(2));
        Scheduler.QueueEffect(() => order.Add(3));

        Scheduler.FlushEffects();

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public void FlushEffects_ClearsQueue_SubsequentFlushIsNoOp()
    {
        int ran = 0;
        Scheduler.QueueEffect(() => ran++);
        Scheduler.FlushEffects();
        Assert.Equal(1, ran);

        Scheduler.FlushEffects(); // should be no-op
        Assert.Equal(1, ran);
    }

    // ──────────────────────────────────────────────
    //  PostLayout callbacks
    // ──────────────────────────────────────────────

    [Fact]
    public void PostLayoutCallback_RunsOnFlush()
    {
        int ran = 0;
        Scheduler.OnPostLayout(() => ran++);

        Scheduler.FlushPostLayoutCallbacks();
        Assert.Equal(1, ran);
    }

    [Fact]
    public void PostLayoutCallback_PersistsAcrossFlushes()
    {
        // PostLayout callbacks are persistent (not one-shot) — they run every flush
        int ran = 0;
        Scheduler.OnPostLayout(() => ran++);

        Scheduler.FlushPostLayoutCallbacks();
        Scheduler.FlushPostLayoutCallbacks();
        Assert.Equal(2, ran);
    }

    // ──────────────────────────────────────────────
    //  Reset
    // ──────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsEffectQueue()
    {
        int ran = 0;
        Scheduler.QueueEffect(() => ran++);

        Scheduler.Reset();
        Scheduler.FlushEffects();

        Assert.Equal(0, ran);
    }

    [Fact]
    public void Reset_PostLayoutCallbacksPersist()
    {
        // PostLayout callbacks are persistent and survive Reset
        int ran = 0;
        Scheduler.OnPostLayout(() => ran++);

        Scheduler.Reset();
        Scheduler.FlushPostLayoutCallbacks();

        Assert.Equal(1, ran);
    }

    // ──────────────────────────────────────────────
    //  GetRoots
    // ──────────────────────────────────────────────

    [Fact]
    public void GetRoots_InitiallyEmpty()
    {
        var roots = Scheduler.GetRoots();
        Assert.Empty(roots);
    }

    // ──────────────────────────────────────────────
    //  Mount / Unmount
    // ──────────────────────────────────────────────

    [Fact]
    public void Mount_CreatesRootNode()
    {
        var handle = Scheduler.Mount(() => new VNode("div"));

        Assert.NotNull(handle);
        Assert.NotNull(handle.RootNode);
        Assert.Single(Scheduler.GetRoots());
    }

    [Fact]
    public void Mount_RootNode_HasComponentType()
    {
        var handle = Scheduler.Mount(() => new VNode("div"));
        Assert.Equal("__component", handle.RootNode!.Type);
    }

    [Fact]
    public void Mount_RendersComponentSubtree()
    {
        var handle = Scheduler.Mount(() => new VNode("div")
        {
            Children = new[] { new VNode("span") }
        });

        // Root is __component, its child is div, which has a span child
        var div = handle.RootNode!.Children[0];
        Assert.Equal("div", div.Type);
        Assert.Single(div.Children);
        Assert.Equal("span", div.Children[0].Type);
    }

    [Fact]
    public void Unmount_RemovesRoot()
    {
        var handle = Scheduler.Mount(() => new VNode("div"));
        Assert.Single(Scheduler.GetRoots());

        Scheduler.Unmount(handle.RootId);
        Assert.Empty(Scheduler.GetRoots());
    }

    [Fact]
    public void RenderHandle_Dispose_Unmounts()
    {
        var handle = Scheduler.Mount(() => new VNode("div"));
        Assert.Single(Scheduler.GetRoots());

        handle.Dispose();
        Assert.Empty(Scheduler.GetRoots());
    }

    [Fact]
    public void Mount_MultipleRoots_AllTracked()
    {
        var h1 = Scheduler.Mount(() => new VNode("div"));
        var h2 = Scheduler.Mount(() => new VNode("span"));

        Assert.Equal(2, Scheduler.GetRoots().Count);

        h1.Dispose();
        Assert.Single(Scheduler.GetRoots());

        h2.Dispose();
        Assert.Empty(Scheduler.GetRoots());
    }

    // ──────────────────────────────────────────────
    //  ScheduleRender / FlushRenders
    // ──────────────────────────────────────────────

    [Fact]
    public void ScheduleRender_FlushRenders_ReRendersComponent()
    {
        int renderCount = 0;
        var handle = Scheduler.Mount(() =>
        {
            renderCount++;
            return new VNode("div");
        });

        Assert.Equal(1, renderCount);

        Scheduler.ScheduleRender(handle.RootNode!.ComponentId);
        Scheduler.FlushRenders();

        Assert.Equal(2, renderCount);
    }

    [Fact]
    public void FlushRenders_NoDirtyComponents_IsNoOp()
    {
        int renderCount = 0;
        var handle = Scheduler.Mount(() =>
        {
            renderCount++;
            return new VNode("div");
        });

        Assert.Equal(1, renderCount);

        Scheduler.FlushRenders(); // nothing dirty
        Assert.Equal(1, renderCount);
    }

    // ──────────────────────────────────────────────
    //  FindNodeByKey
    // ──────────────────────────────────────────────

    [Fact]
    public void FindNodeByKey_ExistingKey_ReturnsNode()
    {
        var handle = Scheduler.Mount(() => new VNode("div")
        {
            Children = new[] { new VNode("span") { Key = "mySpan" } }
        });

        var found = Scheduler.FindNodeByKey("mySpan");
        Assert.NotNull(found);
        Assert.Equal("span", found!.Type);
        Assert.Equal("mySpan", found.Key);
    }

    [Fact]
    public void FindNodeByKey_NonExistentKey_ReturnsNull()
    {
        var handle = Scheduler.Mount(() => new VNode("div"));

        var found = Scheduler.FindNodeByKey("noSuchKey");
        Assert.Null(found);
    }

    [Fact]
    public void FindNodeByKey_NoRoots_ReturnsNull()
    {
        var found = Scheduler.FindNodeByKey("anything");
        Assert.Null(found);
    }
}
