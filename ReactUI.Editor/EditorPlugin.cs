using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Reactor;
using ReactUI.Core;
using ReactUI.Editor.Components;
using ReactUI.Style;

namespace ReactUI.Editor;

/// <summary>
/// BepInEx plugin that provides an in-game JSX editor.
/// Toggle with F10.
/// </summary>
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
        // Register the editor styles globally
        GlobalStyles.Register(EditorStyles.Sheet);

        // Set the watch directory to a 'ui' folder next to this plugin DLL
        var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var uiDir = Path.Combine(pluginDir, "ui");
        if (!Directory.Exists(uiDir))
            Directory.CreateDirectory(uiDir);

        EditorRoot.SetWatchDirectory(uiDir);

        // Also start watching for hot-reload
        UI.WatchJsx(uiDir);

        Log.LogInfo("ReactUI Editor loaded. Press F10 to toggle.");
    }

    /// <summary>
    /// Called from ReactUIBehaviour (or a Harmony patch) each frame.
    /// Toggle editor visibility with F10.
    /// </summary>
    public void Update()
    {
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
