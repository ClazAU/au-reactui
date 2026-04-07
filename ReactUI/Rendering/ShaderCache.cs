using System.Collections.Generic;

namespace ReactUI.Rendering;

using UnityEngine;

/// <summary>
/// Loads and caches shaders and materials for the ReactUI rendering pipeline.
/// Attempts to find pre-compiled shaders by name first, then falls back to
/// embedded ShaderLab source compiled at runtime.
/// </summary>
public static class ShaderCache
{
    private static readonly Dictionary<string, Shader> _shaders = new();
    private static readonly Dictionary<string, Material> _materials = new();
    private static bool _initialized;

    // Fallback shader/material for when SDF shaders are unavailable
    private static Material? _fallbackMaterial;

    private static AssetBundle? _bundle;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Try loading shaders from AssetBundle first (works in IL2CPP builds)
        TryLoadAssetBundle();

        // Fallback: try Shader.Find / runtime compilation
        TryCreateShader("ReactUI/SDFRect", SdfRectShaderSource.Source);
        TryCreateShader("ReactUI/SDFText", SdfTextShaderSource.Source);
        TryCreateShader("ReactUI/Image", ImageShaderSource.Source);
        TryCreateShader("ReactUI/KawaseBlur", KawaseBlurShaderSource.Source);

        Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] ShaderCache: SDFRect={HasShader("ReactUI/SDFRect")}, SDFText={HasShader("ReactUI/SDFText")}, Image={HasShader("ReactUI/Image")}, Blur={HasShader("ReactUI/KawaseBlur")}");

        // Build fallback material using a guaranteed built-in shader
        var fallbackShader = Shader.Find("Hidden/Internal-Colored");
        if (fallbackShader != null)
        {
            _fallbackMaterial = new Material(fallbackShader);
            _fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;
            _fallbackMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _fallbackMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _fallbackMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _fallbackMaterial.SetInt("_ZWrite", 0);
        }
    }

    private static void TryLoadAssetBundle()
    {
        // Look for reactui.assets next to the plugin DLL
        var pluginDir = System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (pluginDir == null) return;

        var bundlePath = System.IO.Path.Combine(pluginDir, "reactui.assets");
        if (!System.IO.File.Exists(bundlePath))
        {
            Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] No AssetBundle at {bundlePath}");
            return;
        }

        try
        {
            _bundle = AssetBundle.LoadFromFile(bundlePath);
            if (_bundle == null)
            {
                Plugin.ReactUIPlugin.Logger.LogWarning("[ReactUI] AssetBundle.LoadFromFile returned null");
                return;
            }

            // Load all shaders from the bundle (non-generic for IL2CPP compatibility)
            var allAssets = _bundle.LoadAllAssets(Il2CppInterop.Runtime.Il2CppType.Of<Shader>());
            foreach (var asset in allAssets)
            {
                var shader = asset.TryCast<Shader>();
                if (shader != null && !string.IsNullOrEmpty(shader.name))
                {
                    _shaders[shader.name] = shader;
                    var mat = new Material(shader);
                    mat.hideFlags = HideFlags.HideAndDontSave;
                    _materials[shader.name] = mat;
                    Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] Loaded shader from bundle: {shader.name}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.ReactUIPlugin.Logger.LogError($"[ReactUI] AssetBundle load failed: {ex}");
        }
    }

    private static void TryCreateShader(string name, string source)
    {
        // First try finding a pre-compiled shader (works if shaders are in an AssetBundle or Resources)
        var shader = Shader.Find(name);

        if (shader == null && !string.IsNullOrEmpty(source))
        {
            // Attempt runtime compilation from ShaderLab source
            // This works in Unity Editor but may not work in IL2CPP builds
            // In builds, shaders must be pre-compiled in an AssetBundle
            try
            {
                shader = ShaderLabCompile(source);
            }
            catch (System.Exception)
            {
                // Silently fall through to fallback
            }
        }

        if (shader != null)
        {
            _shaders[name] = shader;
            var mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            _materials[name] = mat;
        }
    }

    /// <summary>
    /// Attempt to compile a shader from ShaderLab source at runtime.
    /// Uses the internal ShaderUtil API which is available in editor only.
    /// Returns null if compilation fails or API is unavailable.
    /// </summary>
    private static Shader? ShaderLabCompile(string source)
    {
        // Unity does not expose a public API for runtime shader compilation.
        // In IL2CPP builds, we rely on shaders being pre-compiled.
        // This is a placeholder that returns null; the fallback path handles it.
        return null;
    }

    /// <summary>
    /// Get a cached material by shader name.
    /// Falls back to the internal colored shader if the requested shader isn't available.
    /// </summary>
    public static Material GetMaterial(string name)
    {
        if (!_initialized) Initialize();

        if (_materials.TryGetValue(name, out var mat))
            return mat;

        // Return cached fallback
        if (_fallbackMaterial == null)
        {
            _fallbackMaterial = new Material(Shader.Find("Hidden/Internal-Colored")!);
            _fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;
            _fallbackMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _fallbackMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _fallbackMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _fallbackMaterial.SetInt("_ZWrite", 0);
        }
        return _fallbackMaterial;
    }

    /// <summary>
    /// Check if a specific shader was loaded successfully (not using fallback).
    /// </summary>
    public static bool HasShader(string name)
    {
        if (!_initialized) Initialize();
        return _shaders.ContainsKey(name);
    }

    /// <summary>
    /// Get the fallback material (solid color quads, no SDF).
    /// </summary>
    public static Material GetFallbackMaterial()
    {
        if (!_initialized) Initialize();
        return _fallbackMaterial ?? new Material(Shader.Find("Hidden/Internal-Colored")!);
    }
}
