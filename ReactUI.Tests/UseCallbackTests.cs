using System;
using Xunit;
using ReactUI.Hooks;
using ReactUI.Core;

namespace ReactUI.Tests;

[Collection("SharedState")]
public class UseCallbackTests : IDisposable
{
    public UseCallbackTests()
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
    //  UseCallback (Action)
    // ──────────────────────────────────────────────

    [Fact]
    public void UseCallback_Action_ReturnsSameInstanceWhenDepsUnchanged()
    {
        Action cb1, cb2;

        HooksRuntime.BeginComponent(1);
        cb1 = UseCallbackHook.UseCallback(() => { }, new object[] { "dep" });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cb2 = UseCallbackHook.UseCallback(() => { }, new object[] { "dep" });
        HooksRuntime.EndComponent();

        Assert.Same(cb1, cb2);
    }

    [Fact]
    public void UseCallback_Action_ReturnsNewInstanceWhenDepsChange()
    {
        Action cb1, cb2;

        HooksRuntime.BeginComponent(1);
        cb1 = UseCallbackHook.UseCallback(() => { }, new object[] { 1 });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cb2 = UseCallbackHook.UseCallback(() => { }, new object[] { 2 });
        HooksRuntime.EndComponent();

        Assert.NotSame(cb1, cb2);
    }

    [Fact]
    public void UseCallback_Action_ReturnedCallbackIsInvokable()
    {
        int called = 0;

        HooksRuntime.BeginComponent(1);
        var cb = UseCallbackHook.UseCallback(() => { called++; }, new object[] { "x" });
        HooksRuntime.EndComponent();

        cb();
        Assert.Equal(1, called);
    }

    // ──────────────────────────────────────────────
    //  UseCallback (Func<T>)
    // ──────────────────────────────────────────────

    [Fact]
    public void UseCallback_FuncT_ReturnsSameInstanceWhenDepsUnchanged()
    {
        Func<int> cb1, cb2;

        HooksRuntime.BeginComponent(1);
        cb1 = UseCallbackHook.UseCallback(() => 42, new object[] { "a" });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cb2 = UseCallbackHook.UseCallback(() => 99, new object[] { "a" });
        HooksRuntime.EndComponent();

        Assert.Same(cb1, cb2);
        Assert.Equal(42, cb2()); // cached version returns original value
    }

    [Fact]
    public void UseCallback_FuncT_ReturnsNewInstanceWhenDepsChange()
    {
        Func<int> cb1, cb2;

        HooksRuntime.BeginComponent(1);
        cb1 = UseCallbackHook.UseCallback(() => 42, new object[] { 1 });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cb2 = UseCallbackHook.UseCallback(() => 99, new object[] { 2 });
        HooksRuntime.EndComponent();

        Assert.NotSame(cb1, cb2);
        Assert.Equal(99, cb2());
    }

    // ──────────────────────────────────────────────
    //  UseCallback (Action<T>)
    // ──────────────────────────────────────────────

    [Fact]
    public void UseCallback_ActionT_ReturnsSameInstanceWhenDepsUnchanged()
    {
        Action<string> cb1, cb2;

        HooksRuntime.BeginComponent(1);
        cb1 = UseCallbackHook.UseCallback<string>(_ => { }, new object[] { "dep" });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cb2 = UseCallbackHook.UseCallback<string>(_ => { }, new object[] { "dep" });
        HooksRuntime.EndComponent();

        Assert.Same(cb1, cb2);
    }

    [Fact]
    public void UseCallback_ActionT_ReturnsNewInstanceWhenDepsChange()
    {
        Action<string> cb1, cb2;
        string captured = "";

        HooksRuntime.BeginComponent(1);
        cb1 = UseCallbackHook.UseCallback<string>(s => captured = s, new object[] { 1 });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cb2 = UseCallbackHook.UseCallback<string>(s => captured = s + "!", new object[] { 2 });
        HooksRuntime.EndComponent();

        Assert.NotSame(cb1, cb2);
        cb2("hello");
        Assert.Equal("hello!", captured);
    }

    // ──────────────────────────────────────────────
    //  UseCallback (Func<T1, T2>)
    // ──────────────────────────────────────────────

    [Fact]
    public void UseCallback_FuncT1T2_ReturnsSameInstanceWhenDepsUnchanged()
    {
        Func<int, string> cb1, cb2;

        HooksRuntime.BeginComponent(1);
        cb1 = UseCallbackHook.UseCallback<int, string>(x => x.ToString(), new object[] { "dep" });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cb2 = UseCallbackHook.UseCallback<int, string>(x => "new" + x, new object[] { "dep" });
        HooksRuntime.EndComponent();

        Assert.Same(cb1, cb2);
        Assert.Equal("42", cb2(42)); // cached version
    }

    // ──────────────────────────────────────────────
    //  Null deps
    // ──────────────────────────────────────────────

    [Fact]
    public void UseCallback_NullDeps_ReturnsNewInstanceEveryRender()
    {
        Action cb1, cb2;

        HooksRuntime.BeginComponent(1);
        cb1 = UseCallbackHook.UseCallback(() => { }, null);
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cb2 = UseCallbackHook.UseCallback(() => { }, null);
        HooksRuntime.EndComponent();

        Assert.NotSame(cb1, cb2);
    }

    // ──────────────────────────────────────────────
    //  Multiple callbacks in same component
    // ──────────────────────────────────────────────

    [Fact]
    public void UseCallback_MultipleInSameComponent_IndependentSlots()
    {
        Action cbA1, cbA2, cbB1, cbB2;

        HooksRuntime.BeginComponent(1);
        cbA1 = UseCallbackHook.UseCallback(() => { }, new object[] { "a" });
        cbB1 = UseCallbackHook.UseCallback(() => { }, new object[] { "b" });
        HooksRuntime.EndComponent();

        HooksRuntime.BeginComponent(1);
        cbA2 = UseCallbackHook.UseCallback(() => { }, new object[] { "a" }); // unchanged
        cbB2 = UseCallbackHook.UseCallback(() => { }, new object[] { "b2" }); // changed
        HooksRuntime.EndComponent();

        Assert.Same(cbA1, cbA2);   // A unchanged
        Assert.NotSame(cbB1, cbB2); // B changed
    }
}
