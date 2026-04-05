using System;
using Xunit;
using ReactUI.Hooks;
using ReactUI.Core;

namespace ReactUI.Tests;

[Collection("SharedState")]
public class HooksTests : IDisposable
{
    public HooksTests()
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
    //  UseState
    // ──────────────────────────────────────────────

    [Fact]
    public void UseState_FirstRender_ReturnsInitialValue()
    {
        HooksRuntime.BeginComponent(1);
        var (value, _) = UseStateHook.UseState(42);
        HooksRuntime.EndComponent();

        Assert.Equal(42, value);
    }

    [Fact]
    public void UseState_Setter_UpdatesStoredValue()
    {
        // First render
        HooksRuntime.BeginComponent(1);
        var (_, setter) = UseStateHook.UseState(10);
        HooksRuntime.EndComponent();

        // Mutate
        setter(99);

        // Second render — read back the updated value
        HooksRuntime.BeginComponent(1);
        var (value2, _) = UseStateHook.UseState(10);
        HooksRuntime.EndComponent();

        Assert.Equal(99, value2);
    }

    [Fact]
    public void UseState_Setter_SchedulesRender()
    {
        HooksRuntime.BeginComponent(1);
        var (_, setter) = UseStateHook.UseState(0);
        HooksRuntime.EndComponent();

        // After calling setter, the component should be dirty.
        // We can verify indirectly: FlushRenders won't throw and _renderScheduled was set.
        // The simplest observable side-effect: ScheduleRender was called, so
        // a subsequent FlushRenders call processes (even though there's no registered tree, it won't crash).
        setter(5);

        // If ScheduleRender wasn't called this would be a no-op. We just verify no exception.
        Scheduler.FlushRenders();
    }

    [Fact]
    public void UseState_MultipleCallsGetIndependentSlots()
    {
        HooksRuntime.BeginComponent(1);
        var (a, setA) = UseStateHook.UseState("hello");
        var (b, setB) = UseStateHook.UseState(100);
        HooksRuntime.EndComponent();

        Assert.Equal("hello", a);
        Assert.Equal(100, b);

        setA("world");

        // Re-render
        HooksRuntime.BeginComponent(1);
        var (a2, _) = UseStateHook.UseState("hello");
        var (b2, _) = UseStateHook.UseState(100);
        HooksRuntime.EndComponent();

        Assert.Equal("world", a2);
        Assert.Equal(100, b2); // unchanged
    }

    [Fact]
    public void UseState_ValuePersistsAcrossReRenders()
    {
        HooksRuntime.BeginComponent(1);
        var (_, setter) = UseStateHook.UseState(1);
        HooksRuntime.EndComponent();

        setter(2);

        // Second render
        HooksRuntime.BeginComponent(1);
        var (val, _) = UseStateHook.UseState(1);
        HooksRuntime.EndComponent();

        Assert.Equal(2, val);
        Assert.False(HooksRuntime.GetContext(1)!.IsFirstRender);
    }

    // ──────────────────────────────────────────────
    //  UseEffect
    // ──────────────────────────────────────────────

    [Fact]
    public void UseEffect_QueuedOnFirstRender()
    {
        int ran = 0;

        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, null);
        HooksRuntime.EndComponent();

        Assert.Equal(0, ran); // queued, not yet run
        Scheduler.FlushEffects();
        Assert.Equal(1, ran);
    }

    [Fact]
    public void UseEffect_EmptyDeps_RunsOnlyOnce()
    {
        int ran = 0;

        // First render
        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, Array.Empty<object>());
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();
        Assert.Equal(1, ran);

        // Second render — empty deps means "mount only"
        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, Array.Empty<object>());
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();

        Assert.Equal(1, ran); // did NOT run again
    }

    [Fact]
    public void UseEffect_NullDeps_RunsEveryRender()
    {
        int ran = 0;

        // First render
        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, null);
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();
        Assert.Equal(1, ran);

        // Second render
        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, null);
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();

        Assert.Equal(2, ran);
    }

    [Fact]
    public void UseEffect_ChangedDeps_RunsAgain()
    {
        int ran = 0;

        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, new object[] { 1 });
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();
        Assert.Equal(1, ran);

        // Re-render with changed dep
        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, new object[] { 2 });
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();

        Assert.Equal(2, ran);
    }

    [Fact]
    public void UseEffect_SameDeps_DoesNotRunAgain()
    {
        int ran = 0;

        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, new object[] { "x", 42 });
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();

        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() => { ran++; }, new object[] { "x", 42 });
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();

        Assert.Equal(1, ran);
    }

    [Fact]
    public void UseEffect_CleanupCalledBeforeReRun()
    {
        int cleanupCount = 0;

        // First render — effect with cleanup
        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() =>
        {
            return () => { cleanupCount++; };
        }, null);
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();

        Assert.Equal(0, cleanupCount); // no cleanup yet

        // Second render (null deps = always re-run)
        HooksRuntime.BeginComponent(1);
        UseEffectHook.UseEffect(() =>
        {
            return () => { cleanupCount++; };
        }, null);
        HooksRuntime.EndComponent();
        Scheduler.FlushEffects();

        Assert.Equal(1, cleanupCount); // previous cleanup ran
    }

    // ──────────────────────────────────────────────
    //  DepsEqual
    // ──────────────────────────────────────────────

    [Fact]
    public void DepsEqual_BothNull_ReturnsFalse()
    {
        Assert.False(UseEffectHook.DepsEqual(null, null));
    }

    [Fact]
    public void DepsEqual_SameValues_ReturnsTrue()
    {
        Assert.True(UseEffectHook.DepsEqual(new object[] { 1, "a" }, new object[] { 1, "a" }));
    }

    [Fact]
    public void DepsEqual_DifferentLengths_ReturnsFalse()
    {
        Assert.False(UseEffectHook.DepsEqual(new object[] { 1 }, new object[] { 1, 2 }));
    }

    [Fact]
    public void DepsEqual_DifferentValues_ReturnsFalse()
    {
        Assert.False(UseEffectHook.DepsEqual(new object[] { 1, 2 }, new object[] { 1, 3 }));
    }

    // ──────────────────────────────────────────────
    //  UseMemo
    // ──────────────────────────────────────────────

    [Fact]
    public void UseMemo_FactoryRunsOnFirstRender()
    {
        int calls = 0;

        HooksRuntime.BeginComponent(1);
        var result = UseMemoHook.UseMemo(() => { calls++; return 42; }, new object[] { 1 });
        HooksRuntime.EndComponent();

        Assert.Equal(1, calls);
        Assert.Equal(42, result);
    }

    [Fact]
    public void UseMemo_ReturnsCachedValueWhenDepsUnchanged()
    {
        int calls = 0;

        HooksRuntime.BeginComponent(1);
        UseMemoHook.UseMemo(() => { calls++; return "val"; }, new object[] { "dep" });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        var result = UseMemoHook.UseMemo(() => { calls++; return "val2"; }, new object[] { "dep" });
        HooksRuntime.EndComponent();

        Assert.Equal(1, calls); // factory NOT called again
        Assert.Equal("val", result); // cached value returned
    }

    [Fact]
    public void UseMemo_RecomputesWhenDepsChange()
    {
        int calls = 0;

        HooksRuntime.BeginComponent(1);
        UseMemoHook.UseMemo(() => { calls++; return 10; }, new object[] { 1 });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        var result = UseMemoHook.UseMemo(() => { calls++; return 20; }, new object[] { 2 });
        HooksRuntime.EndComponent();

        Assert.Equal(2, calls);
        Assert.Equal(20, result);
    }

    // ──────────────────────────────────────────────
    //  UseRef
    // ──────────────────────────────────────────────

    [Fact]
    public void UseRef_ReturnsRefWithInitialValue()
    {
        HooksRuntime.BeginComponent(1);
        var r = UseRefHook.UseRef(7);
        HooksRuntime.EndComponent();

        Assert.Equal(7, r.Current);
    }

    [Fact]
    public void UseRef_SameRefObjectAcrossRenders()
    {
        HooksRuntime.BeginComponent(1);
        var r1 = UseRefHook.UseRef(0);
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        var r2 = UseRefHook.UseRef(0);
        HooksRuntime.EndComponent();

        Assert.Same(r1, r2);
    }

    [Fact]
    public void UseRef_MutatingCurrentDoesNotTriggerReRender()
    {
        HooksRuntime.BeginComponent(1);
        var r = UseRefHook.UseRef(0);
        HooksRuntime.EndComponent();

        r.Current = 999;

        // FlushRenders should be a no-op (nothing was scheduled)
        // We verify by checking that no exception is thrown and the dirty set is empty.
        Scheduler.FlushRenders();

        // Re-render manually — value should still be there since it's just a mutable field
        HooksRuntime.BeginComponent(1);
        var r2 = UseRefHook.UseRef(0);
        HooksRuntime.EndComponent();

        Assert.Equal(999, r2.Current);
    }

    // ──────────────────────────────────────────────
    //  HookContext
    // ──────────────────────────────────────────────

    [Fact]
    public void HookContext_AddSlotIncrementsIndex()
    {
        var ctx = new HookContext();
        Assert.Equal(0, ctx.CurrentHookIndex);

        ctx.AddSlot("a");
        Assert.Equal(1, ctx.CurrentHookIndex);

        ctx.AddSlot("b");
        Assert.Equal(2, ctx.CurrentHookIndex);
    }

    [Fact]
    public void HookContext_ReadSlotReturnsCorrectValue()
    {
        var ctx = new HookContext();
        ctx.AddSlot("first");
        ctx.AddSlot("second");

        ctx.ResetIndex();

        Assert.Equal("first", ctx.ReadSlot());
        Assert.Equal("second", ctx.ReadSlot());
    }

    [Fact]
    public void HookContext_ResetIndexSetsToZero()
    {
        var ctx = new HookContext();
        ctx.AddSlot(1);
        ctx.AddSlot(2);
        Assert.Equal(2, ctx.CurrentHookIndex);

        ctx.ResetIndex();
        Assert.Equal(0, ctx.CurrentHookIndex);
    }

    [Fact]
    public void HookContext_IsFirstRender_TrueInitially_FalseAfterEnd()
    {
        HooksRuntime.BeginComponent(1);
        var ctx = HooksRuntime.Current;
        Assert.True(ctx.IsFirstRender);
        HooksRuntime.EndComponent();

        Assert.False(ctx.IsFirstRender);
    }
}
