using Xunit;
using ReactUI.Hooks;
using ReactUI.Core;

namespace ReactUI.Tests;

[Collection("SharedState")]
public class UseContextTests : System.IDisposable
{
    public UseContextTests()
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
    //  Context<T> basics
    // ──────────────────────────────────────────────

    [Fact]
    public void Context_DefaultValue_IsSetByConstructor()
    {
        var ctx = new Context<int>(42);
        Assert.Equal(42, ctx.DefaultValue);
    }

    [Fact]
    public void Context_Provide_SetsValue()
    {
        var ctx = new Context<string>("default");
        ctx.Provide("provided");

        HooksRuntime.BeginComponent(1);
        var value = UseContextHook.UseContext(ctx);
        HooksRuntime.EndComponent();

        Assert.Equal("provided", value);
    }

    [Fact]
    public void Context_WithoutProvider_ReturnsDefaultValue()
    {
        var ctx = new Context<int>(99);

        HooksRuntime.BeginComponent(1);
        var value = UseContextHook.UseContext(ctx);
        HooksRuntime.EndComponent();

        Assert.Equal(99, value);
    }

    [Fact]
    public void Context_ClearProvider_RevertsToDefault()
    {
        var ctx = new Context<string>("default");
        ctx.Provide("provided");
        ctx.ClearProvider();

        HooksRuntime.BeginComponent(1);
        var value = UseContextHook.UseContext(ctx);
        HooksRuntime.EndComponent();

        Assert.Equal("default", value);
    }

    [Fact]
    public void Context_ProvideMultipleTimes_UsesLatest()
    {
        var ctx = new Context<int>(0);
        ctx.Provide(10);
        ctx.Provide(20);
        ctx.Provide(30);

        HooksRuntime.BeginComponent(1);
        var value = UseContextHook.UseContext(ctx);
        HooksRuntime.EndComponent();

        Assert.Equal(30, value);
    }

    // ──────────────────────────────────────────────
    //  Multiple contexts
    // ──────────────────────────────────────────────

    [Fact]
    public void MultipleContexts_IndependentValues()
    {
        var themeCtx = new Context<string>("light");
        var sizeCtx = new Context<int>(14);

        themeCtx.Provide("dark");

        HooksRuntime.BeginComponent(1);
        var theme = UseContextHook.UseContext(themeCtx);
        var size = UseContextHook.UseContext(sizeCtx);
        HooksRuntime.EndComponent();

        Assert.Equal("dark", theme);  // provided
        Assert.Equal(14, size);        // default (no provider)
    }

    // ──────────────────────────────────────────────
    //  Nullable/reference types
    // ──────────────────────────────────────────────

    [Fact]
    public void Context_NullableReferenceType_ProvideNull()
    {
        var ctx = new Context<string?>("default");
        ctx.Provide(null);

        HooksRuntime.BeginComponent(1);
        var value = UseContextHook.UseContext(ctx);
        HooksRuntime.EndComponent();

        Assert.Null(value);
    }

    [Fact]
    public void Context_ReferenceType_DefaultNull()
    {
        var ctx = new Context<string?>(null);

        HooksRuntime.BeginComponent(1);
        var value = UseContextHook.UseContext(ctx);
        HooksRuntime.EndComponent();

        Assert.Null(value);
    }
}
