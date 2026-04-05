using System;
using ReactUI.Core;

namespace ReactUI.Hooks;

/// <summary>
/// React-style useState hook. Returns the current value and a setter that triggers re-render.
/// IL2CPP safe: values are boxed as object internally.
/// </summary>
public static class UseStateHook
{
    /// <summary>
    /// Declares a state variable. On first render, initializes with the given value.
    /// On subsequent renders, returns the current stored value.
    /// The setter schedules a re-render of the owning component.
    /// </summary>
    // [Preserve] — add Il2CppInterop.Runtime.Attributes.Preserve to prevent IL2CPP stripping
    public static (T value, Action<T> setter) UseState<T>(T initialValue)
    {
        var ctx = HooksRuntime.Current;

        T value;
        if (ctx.IsFirstRender)
        {
            // First render: allocate a new slot with the initial value (boxed)
            ctx.AddSlot(initialValue);
            value = initialValue;
        }
        else
        {
            // Read the current value from the slot (boxed as object)
            var boxed = ctx.ReadSlot();
            if (boxed == null)
            {
                value = default!;
            }
            else
            {
                value = (T)boxed;
            }
        }

        // Capture the slot index and component ID for the setter closure
        var slotIndex = ctx.CurrentHookIndex - 1;
        var componentId = ctx.ComponentId;
        var capturedCtx = ctx;

        // The setter: writes the new value and schedules a re-render
        void Setter(T newValue)
        {
            capturedCtx.StateSlots[slotIndex] = newValue;
            Scheduler.ScheduleRender(componentId);
        }

        return (value, Setter);
    }
}
