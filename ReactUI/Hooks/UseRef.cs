namespace ReactUI.Hooks;

/// <summary>
/// A mutable reference container, similar to React's useRef.
/// The Ref object itself is stable across renders; only .Current changes.
/// </summary>
public class Ref<T>
{
    public T? Current;

    public Ref() { }
    public Ref(T? initial) { Current = initial; }
}

/// <summary>
/// React-style useRef hook. Returns a stable Ref&lt;T&gt; that persists across renders.
/// Mutating .Current does NOT trigger a re-render (unlike useState).
/// IL2CPP safe: the Ref is stored as a boxed object in the hook slot.
/// </summary>
public static class UseRefHook
{
    // [Preserve] — add Il2CppInterop.Runtime.Attributes.Preserve to prevent IL2CPP stripping
    public static Ref<T> UseRef<T>(T? initial = default)
    {
        var ctx = HooksRuntime.Current;

        if (ctx.IsFirstRender)
        {
            var r = new Ref<T>(initial);
            ctx.AddSlot(r);
            return r;
        }

        return (Ref<T>)ctx.ReadSlot()!;
    }
}
