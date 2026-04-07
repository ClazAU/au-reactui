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
            var bundle = AssetBundleManager.Load("reactui");

            var allAssets = bundle.LoadAllAssets(Il2CppInterop.Runtime.Il2CppType.Of<Shader>());
            foreach (var asset in allAssets)
            {
                var shader = asset.TryCast<Shader>();
                if (shader != null && !string.IsNullOrEmpty(shader.name))
                {
                    _shaders[shader.name] = shader;
                    var mat = new Material(shader);
                    mat.hideFlags = HideFlags.HideAndDontSave;
                    _materials[shader.name] = mat;
                    Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] Loaded shader: {shader.name}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.ReactUIPlugin.Logger.LogError($"[ReactUI] Failed to load shader bundle: {ex}");
        }

        Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] ShaderCache: SDFRect={HasShader("ReactUI/SDFRect")}, SDFText={HasShader("ReactUI/SDFText")}, Image={HasShader("ReactUI/Image")}, Blur={HasShader("ReactUI/KawaseBlur")}");
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
