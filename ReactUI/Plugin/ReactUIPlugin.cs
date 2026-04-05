using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Reactor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReactUI.Plugin;

[BepInAutoPlugin("com.reactui.core", "ReactUI")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class ReactUIPlugin : BasePlugin
{
    internal static ManualLogSource Logger = null!;
    static bool _initialized;

    public override void Load()
    {
        Logger = Log;
        Log.LogInfo("[ReactUI] Load() called — waiting for scene to create GameObject");

        // Patch PassiveButton to block game clicks when hovering ReactUI
        var harmony = new HarmonyLib.Harmony("com.reactui.core");
        harmony.PatchAll(typeof(ReactUIPlugin).Assembly);
        Log.LogInfo("[ReactUI] Harmony patches applied");

        // Defer creation until a scene is loaded, so DontDestroyOnLoad works reliably
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_initialized) return;
        _initialized = true;

        Logger.LogInfo($"[ReactUI] Scene '{scene.name}' loaded — creating GameObject now");

        try
        {
            var go = new GameObject("ReactUI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var comp = go.AddComponent<ReactUIBehaviour>();
            Logger.LogInfo($"[ReactUI] GameObject created, AddComponent={comp != null}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"[ReactUI] Init FAILED: {ex}");
        }
    }
}
