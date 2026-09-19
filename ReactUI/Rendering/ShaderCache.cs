using System.Collections.Generic;

namespace ReactUI.Rendering;

using Reactor.Utilities;
using UnityEngine;

/// <summary>
/// Loads and caches shaders and materials for the ReactUI rendering pipeline.
/// Shaders are loaded from an embedded AssetBundle via Reactor's AssetBundleManager.
/// </summary>
public static class ShaderCache
{
    private static readonly Dictionary<string, Shader> _shaders = new();
    private static readonly Dictionary<string, Material> _materials = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            LoadShaders("reactui");
        }
        catch (System.Exception ex)
        {
            Plugin.ReactUIPlugin.Logger.LogError($"[ReactUI] Failed to load shader bundle: {ex}");
        }

        try
        {
            // Shaders rebuilt since the main bundle was last built ship in this one and replace theirs by name. A
            // platform that has no such bundle keeps the main bundle's version.
            LoadShaders("reactui-image");
        }
        catch (System.Exception)
        {
        }

        Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] ShaderCache: SDFRect={HasShader("ReactUI/SDFRect")}, SDFText={HasShader("ReactUI/SDFText")}, Image={HasShader("ReactUI/Image")}, Blur={HasShader("ReactUI/KawaseBlur")}");
    }

    private static void LoadShaders(string bundleName)
    {
        var bundle = AssetBundleManager.Load(bundleName);
        foreach (var asset in bundle.LoadAllAssets(Il2CppInterop.Runtime.Il2CppType.Of<Shader>()))
        {
            var shader = asset.TryCast<Shader>();
            if (shader == null || string.IsNullOrEmpty(shader.name)) continue;

            _shaders[shader.name] = shader;
            _materials[shader.name] = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] Loaded shader: {shader.name} ({bundleName})");
        }
    }
    /// <summary>
    /// Get a cached material by shader name. Returns null if the shader wasn't loaded.
    /// </summary>
    public static Material? GetMaterial(string name)
    {
        if (!_initialized) Initialize();

        return _materials.TryGetValue(name, out var mat) ? mat : null;
    }

    /// <summary>
    /// Check if a specific shader was loaded successfully.
    /// </summary>
    public static bool HasShader(string name)
    {
        if (!_initialized) Initialize();
        return _shaders.ContainsKey(name);
    }
}
