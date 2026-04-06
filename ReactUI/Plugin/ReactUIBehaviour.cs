using System;
using Reactor.Utilities.Attributes;
using UnityEngine;

namespace ReactUI.Plugin;

[RegisterInIl2Cpp]
public class ReactUIBehaviour : MonoBehaviour
{
    static ReactUIBehaviour? _instance;
    Rendering.RenderPipeline? _renderPipeline;
    int _frameCount;
    bool _initDone;


    public static ReactUIBehaviour? Instance => _instance;

    public ReactUIBehaviour(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        _instance = this;
        ReactUIPlugin.Logger.LogInfo("[ReactUI] Behaviour Awake()");
        try
        {
            _renderPipeline = new Rendering.RenderPipeline();
            _renderPipeline.Initialize();
            _initDone = true;
            ReactUIPlugin.Logger.LogInfo("[ReactUI] RenderPipeline initialized");
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
            _frameCount++;
            if (_frameCount == 1)
                ReactUIPlugin.Logger.LogInfo($"[ReactUI] First Update, roots={Core.Scheduler.GetRoots().Count}");

            // Poll all registered key toggles (F8, F9, etc.)
            Input.KeyToggle.Poll();


            // F11 toggles layout debug overlay
            if (UnityEngine.Input.GetKeyDown(KeyCode.F11))
                Rendering.LayoutDebugOverlay.Toggle();

            var roots = Core.Scheduler.GetRoots();
            Input.InputSystem.ProcessInputAll(roots);
            Core.Scheduler.FlushEffects();
        }
        catch (Exception ex)
        {
            if (_frameCount <= 5)
                ReactUIPlugin.Logger.LogError($"[ReactUI] Update FAILED: {ex}");
        }
    }

    private void LateUpdate()
    {
        if (!_initDone) return;
        try
        {
            Core.Scheduler.FlushRenders();
            Animation.TransitionEngine.Tick(Time.deltaTime);

            // Keep re-rendering while animations are in flight
            if (Animation.TransitionEngine.HasActiveAnimations)
                Core.Scheduler.ScheduleRenderAll();

            // Run layout pass
            var roots = Core.Scheduler.GetRoots();
            for (int i = 0; i < roots.Count; i++)
                Layout.LayoutEngine.ComputeLayout(roots[i], Screen.width, Screen.height);

            // Fire post-layout callbacks
            Core.Scheduler.FlushPostLayoutCallbacks();
        }
        catch (Exception ex)
        {
            if (_frameCount <= 5)
                ReactUIPlugin.Logger.LogError($"[ReactUI] LateUpdate FAILED: {ex}");
        }
    }

    private void OnGUI()
    {
        if (!_initDone || _renderPipeline == null) return;

        // Consume input events (click, key, scroll) when mouse is over a ReactUI element.
        // This prevents clicks/keys from passing through to the game.
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
            if ((_frameCount <= 3 || _frameCount % 300 == 0) && roots.Count > 0)
                ReactUIPlugin.Logger.LogInfo($"[ReactUI] OnGUI Repaint frame {_frameCount}, drawing {roots.Count} roots");

            for (int i = 0; i < roots.Count; i++)
            {
                _renderPipeline.BuildDrawCommands(roots[i]);
                _renderPipeline.Execute();
            }


            // Layout debug overlay (F11 to toggle)
            if (Rendering.LayoutDebugOverlay.Enabled)
            {
                for (int i = 0; i < roots.Count; i++)
                    Rendering.LayoutDebugOverlay.Draw(roots[i]);
            }
        }
        catch (Exception ex)
        {
            if (_frameCount <= 5)
                ReactUIPlugin.Logger.LogError($"[ReactUI] OnGUI FAILED: {ex}");
        }
    }
}
