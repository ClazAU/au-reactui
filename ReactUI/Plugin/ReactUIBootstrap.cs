using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReactUI.Plugin;

/// <summary>brings ReactUI up for hosts that merge the library instead of taking the plugin</summary>
public static class ReactUIBootstrap
{
    public const string HarmonyId = "com.reactui.core";

    private static bool patched;
    private static bool hooked;
    private static bool created;

    public static void Initialize()
    {
        ApplyPatches();

        if (hooked) return;
        hooked = true;

        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
    }

    private static void ApplyPatches()
    {
        if (patched) return;
        patched = true;

        // patched by type rather than PatchAll — merged into a host assembly, PatchAll would pick
        // up the host's own patches and apply them a second time under this id
        var harmony = new Harmony(HarmonyId);
        harmony.CreateClassProcessor(typeof(PassiveButtonClickDownPatch)).Patch();
        harmony.CreateClassProcessor(typeof(PassiveButtonClickUpPatch)).Patch();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (created) return;
        created = true;

        var host = new GameObject("ReactUI");
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.AddComponent<ReactUIBehaviour>();
    }
}
