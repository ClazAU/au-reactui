using System;
using Reactor.Utilities.Attributes;
using UnityEngine;

namespace ReactUI.Plugin;

[RegisterInIl2Cpp]
public class ReactUIBehaviour : MonoBehaviour
{
    static ReactUIBehaviour? _instance;
    Rendering.CanvasRenderer? _canvasRenderer;
    int _frameCount;
    bool _initDone;
    bool _debugVisible = true;

    public static ReactUIBehaviour? Instance => _instance;

    public ReactUIBehaviour(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        _instance = this;
        ReactUIPlugin.Logger.LogInfo("[ReactUI] Behaviour Awake()");
        try
        {
            _canvasRenderer = new Rendering.CanvasRenderer();
            _canvasRenderer.Initialize();
            _initDone = true;
            ReactUIPlugin.Logger.LogInfo("[ReactUI] CanvasRenderer initialized");
        }
        catch (Exception ex)
        {
            ReactUIPlugin.Logger.LogError($"[ReactUI] CanvasRenderer init FAILED: {ex}");
        }
    }

    private void Update()
    {
        if (!_initDone) return;
        try
        {
            _frameCount++;

            if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
            {
                _debugVisible = !_debugVisible;
                ReactUIPlugin.Logger.LogInfo($"[ReactUI] F9 pressed, visible={_debugVisible}");
                if (_canvasRenderer != null)
                    _canvasRenderer.SetVisible(_debugVisible);
            }

            if (_frameCount <= 3 || _frameCount % 300 == 0)
                ReactUIPlugin.Logger.LogInfo($"[ReactUI] Update frame {_frameCount}, roots={Core.Scheduler.GetRoots().Count}");

            var roots = Core.Scheduler.GetRoots();
            for (int i = 0; i < roots.Count; i++)
            {
                Input.InputSystem.ProcessInput(roots[i]);
            }
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

            // Run layout + canvas sync
            var roots = Core.Scheduler.GetRoots();
            for (int i = 0; i < roots.Count; i++)
            {
                Layout.LayoutEngine.ComputeLayout(roots[i], Screen.width, Screen.height);

                if (_canvasRenderer != null && _debugVisible)
                    _canvasRenderer.Render(roots[i]);
            }
        }
        catch (Exception ex)
        {
            if (_frameCount <= 5)
                ReactUIPlugin.Logger.LogError($"[ReactUI] LateUpdate FAILED: {ex}");
        }
    }

    private void OnDestroy()
    {
        _canvasRenderer?.Destroy();
    }
}
