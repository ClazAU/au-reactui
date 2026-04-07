using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Reactor;
using ReactUI.Core;
using ReactUI.Editor.Components;
using ReactUI.Plugin;
using ReactUI.Style;

namespace ReactUI.Editor;

[BepInAutoPlugin("com.reactui.editor", "ReactUI Editor")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency("com.reactui.core")]
public partial class EditorPlugin : BasePlugin
{
    private RenderHandle? _editorHandle;
    private bool _visible;

    public override void Load()
    {
        GlobalStyles.Register(EditorStyles.Sheet);

        var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var uiDir = Path.Combine(pluginDir, "ui");
        if (!Directory.Exists(uiDir))
            Directory.CreateDirectory(uiDir);

        EditorRoot.SetWatchDirectory(uiDir);
        UI.WatchJsx(uiDir);

        // Register for per-frame updates via ReactUIBehaviour
        ReactUIBehaviour.OnUpdate += OnUpdate;

        Log.LogInfo("ReactUI Editor loaded. Press F10 to toggle.");
    }

    private void OnUpdate()
    {
        // Drag/resize tracking
        WindowDragResize.Tick();

        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F10))
        {
            _visible = !_visible;

            if (_visible && _editorHandle == null)
            {
                _editorHandle = UI.Render(EditorRoot.Render);
            }
            else if (!_visible && _editorHandle != null)
            {
                _editorHandle.Dispose();
                _editorHandle = null;
            }
        }
    }
}
