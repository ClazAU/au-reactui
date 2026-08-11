using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Reactor;

namespace ReactUI.Plugin;

[BepInAutoPlugin("com.reactui.core", "ReactUI")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class ReactUIPlugin : BasePlugin
{
    internal static ManualLogSource Logger = null!;

    public override void Load()
    {
        Logger = Log;

        try
        {
            ReactUIBootstrap.Initialize();
        }
        catch (Exception ex)
        {
            Log.LogError($"[ReactUI] Init FAILED: {ex}");
        }
    }
}
