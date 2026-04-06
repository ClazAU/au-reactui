using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Reactor;
using ReactUI;
using ReactUI.Core;
using static ReactUI.UI;

namespace ReactUI.Example;

[BepInAutoPlugin("com.reactui.example", "ReactUI Example")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency("com.reactui.core")]
public partial class ExamplePlugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("[ReactUI.Example] Load() called");

        // Mount the C# demo panel (F9 to toggle)
        try
        {
            var handle = UI.Render(DemoPanel.Render);
            Log.LogInfo($"[ReactUI.Example] Mounted DemoPanel={handle.RootId}");
        }
        catch (Exception ex)
        {
            Log.LogError($"[ReactUI.Example] Mount FAILED: {ex}");
        }

        // Mount the JSX counter (hot-reloadable)
        try
        {
            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var uiDir = Path.Combine(pluginDir, "ui");

            if (Directory.Exists(uiDir))
            {
                // Watch for hot-reload
                UI.WatchJsx(uiDir);

                // Mount Counter.jsx if it exists
                var counterPath = Path.Combine(uiDir, "Counter.jsx");
                if (File.Exists(counterPath))
                {
                    var jsxHandle = UI.RenderJsx(counterPath);
                    Log.LogInfo($"[ReactUI.Example] Mounted Counter.jsx");
                }
            }
            else
            {
                Log.LogInfo($"[ReactUI.Example] No ui/ directory found at {uiDir}");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[ReactUI.Example] JSX mount: {ex}");
        }

        Log.LogInfo("[ReactUI.Example] F9=Demo panel | Edit ui/Counter.jsx for hot-reload");
    }
}
