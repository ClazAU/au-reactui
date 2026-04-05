namespace ReactUI.Hooks;

/// <summary>
/// A typed context container, similar to React.createContext.
/// Provides a default value and tracks whether a Provider has been set up.
/// IL2CPP safe: no Reflection.Emit, no dynamic dispatch.
/// </summary>
public class Context<T>
{
    /// <summary>The value used when no Provider wraps the consuming component.</summary>
    public T DefaultValue;

    /// <summary>The current provided value (set by a Provider higher in the tree).</summary>
    internal T? CurrentValue;

    /// <summary>Whether a Provider is active for this context.</summary>
    internal bool HasProvider;

    public Context(T defaultValue)
    {
        DefaultValue = defaultValue;
        CurrentValue = defaultValue;
    }

    /// <summary>
    /// Sets the provided value. Call this from a Provider component's render.
    /// </summary>
    public void Provide(T value)
    {
        CurrentValue = value;
        HasProvider = true;
    }

    /// <summary>
    /// Clears the provider. Call on unmount of the Provider component.
    /// </summary>
    public void ClearProvider()
    {
        HasProvider = false;
        CurrentValue = DefaultValue;
    }
}

/// <summary>
/// React-style useContext hook. Reads the current value from a Context&lt;T&gt;.
/// Returns the provided value if a Provider is active, otherwise the default.
/// </summary>
public static class UseContextHook
{
    public static T UseContext<T>(Context<T> context)
    {
        // This doesn't consume a hook slot — it just reads the context value.
        // In a full implementation, this would also subscribe to context changes
        // and schedule a re-render when the provided value changes.
        return context.HasProvider ? context.CurrentValue! : context.DefaultValue;
    }
}
