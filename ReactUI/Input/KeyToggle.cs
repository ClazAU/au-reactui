using System.Collections.Generic;
using UnityEngine;

namespace ReactUI.Input;

/// <summary>
/// Global key toggle registry. Components register a key, and check IsVisible each render.
/// Keys are polled every frame from ReactUIBehaviour.Update.
/// </summary>
public static class KeyToggle
{
    static readonly Dictionary<KeyCode, bool> _toggles = new();

    /// <summary>
    /// Poll all registered keys. Call once per frame from Update.
    /// </summary>
    public static void Poll()
    {
        foreach (var key in new List<KeyCode>(_toggles.Keys))
        {
            if (UnityEngine.Input.GetKeyDown(key))
            {
                _toggles[key] = !_toggles[key];
                // Schedule re-render for ALL registered components so toggles take effect immediately
                Core.Scheduler.ScheduleRenderAll();
            }
        }
    }

    /// <summary>
    /// Register a toggle key with initial visibility. Returns current state.
    /// </summary>
    public static bool Get(KeyCode key, bool defaultVisible = true)
    {
        if (!_toggles.ContainsKey(key))
            _toggles[key] = defaultVisible;
        return _toggles[key];
    }
}
