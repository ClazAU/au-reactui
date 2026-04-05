using System;
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
        try
        {
            var handle = UI.Render(DemoPanel.Render);
            Log.LogInfo($"[ReactUI.Example] Mounted root, handle={handle.RootId}, rootNode={handle.RootNode?.Type}");
            Log.LogInfo($"[ReactUI.Example] Root children count: {handle.RootNode?.Children.Count}");
        }
        catch (Exception ex)
        {
            Log.LogError($"[ReactUI.Example] Mount FAILED: {ex}");
        }
        Log.LogInfo("[ReactUI.Example] Press F9 to toggle demo panel");
    }
}
