using System;
using Reactor.Utilities.Attributes;
using UnityEngine;

namespace ReactUI.Plugin;

[RegisterInIl2Cpp]
public class ReactUIBehaviour : MonoBehaviour
{
    static ReactUIBehaviour? _instance;
    Rendering.RenderPipeline? _renderPipeline;
    bool _initDone;

    public static ReactUIBehaviour? Instance => _instance;

    /// <summary>Called every Update frame. Register callbacks for plugin-level per-frame logic.</summary>
    public static event Action? OnUpdate;

    public ReactUIBehaviour(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        _instance = this;
        try
        {
            _renderPipeline = new Rendering.RenderPipeline();
            _renderPipeline.Initialize();
            _initDone = true;
        }
        catch (Exception ex)
        {
            ReactUIPlugin.Logger.LogError($"[ReactUI] RenderPipeline init FAILED: {ex}");
        }
    }

    private void Update()
    {
        if (!_initDone) return;
        try
        {
            Input.KeyToggle.Poll();

            if (UnityEngine.Input.GetKeyDown(KeyCode.F11))
                Rendering.LayoutDebugOverlay.Toggle();

            // Tick JSX hot-reloader (drains file-change queue)
            UI.TickJsx();

            // Fire registered update callbacks (used by editor plugin, etc.)
            OnUpdate?.Invoke();

            var roots = Core.Scheduler.GetRoots();
            Input.InputSystem.ProcessInputAll(roots);
            Core.Scheduler.FlushEffects();
        }
        catch (Exception ex)
        {
            ReactUIPlugin.Logger.LogError($"[ReactUI] Update: {ex}");
        }
    }

    private void LateUpdate()
    {
        if (!_initDone) return;
        try
        {
            Core.Scheduler.FlushRenders();
            Animation.TransitionEngine.Tick(Time.deltaTime);

            if (Animation.TransitionEngine.HasActiveAnimations)
                Core.Scheduler.ScheduleRenderAll();

            Rendering.UIScale.Update();
            var roots = Core.Scheduler.GetRoots();
            for (int i = 0; i < roots.Count; i++)
                Layout.LayoutEngine.ComputeLayout(roots[i], Rendering.UIScale.LogicalWidth, Rendering.UIScale.LogicalHeight);

            Core.Scheduler.FlushPostLayoutCallbacks();
        }
        catch (Exception ex)
        {
            ReactUIPlugin.Logger.LogError($"[ReactUI] LateUpdate: {ex}");
        }
    }

    private void OnGUI()
    {
        if (!_initDone || _renderPipeline == null) return;

        var evtType = Event.current.type;
        if (Input.InputSystem.BlockGameInput &&
            evtType != EventType.Repaint && evtType != EventType.Layout)
        {
            Event.current.Use();
            return;
        }

        if (evtType != EventType.Repaint) return;
        try
        {
            var roots = Core.Scheduler.GetRoots();
            for (int i = 0; i < roots.Count; i++)
            {
                _renderPipeline.BuildDrawCommands(roots[i]);
                _renderPipeline.Execute();
            }

            if (Rendering.LayoutDebugOverlay.Enabled)
            {
                for (int i = 0; i < roots.Count; i++)
                    Rendering.LayoutDebugOverlay.Draw(roots[i]);
            }
        }
        catch (Exception ex)
        {
            ReactUIPlugin.Logger.LogError($"[ReactUI] OnGUI: {ex}");
        }
    }
}
