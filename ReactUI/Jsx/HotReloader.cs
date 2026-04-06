using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ReactUI.Core;
using ReactUI.Style;

namespace ReactUI.Jsx;

/// <summary>
/// Watches .jsx and .css files for changes and hot-reloads them.
/// FileSystemWatcher fires on a background thread; all mutations
/// are marshaled to the main thread via a ConcurrentQueue.
/// </summary>
public class HotReloader : IDisposable
{
    private FileSystemWatcher? _jsxWatcher;
    private FileSystemWatcher? _cssWatcher;
    private readonly ConcurrentQueue<Action> _mainThreadQueue = new();
    private readonly Dictionary<string, JsxComponent> _components = new();
    private readonly Dictionary<string, List<JsxComponent>> _cssToComponents = new();
    private readonly JintBridge _bridge;
    private Timer? _debounceTimer;
    private readonly HashSet<string> _pendingReloads = new();
    private readonly object _debounceLock = new();

    private const int DebounceMs = 150;

    public HotReloader()
    {
        _bridge = new JintBridge();
    }

    /// <summary>
    /// Start watching a directory for .jsx and .css file changes.
    /// </summary>
    public void Watch(string directory)
    {
        if (!Directory.Exists(directory))
        {
            System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Watch directory not found: {directory}");
            return;
        }

        _jsxWatcher = new FileSystemWatcher(directory, "*.jsx")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _jsxWatcher.Changed += OnFileChanged;
        _jsxWatcher.Created += OnFileChanged;

        _cssWatcher = new FileSystemWatcher(directory, "*.css")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _cssWatcher.Changed += OnFileChanged;
        _cssWatcher.Created += OnFileChanged;

        System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Watching: {directory}");
    }

    /// <summary>
    /// Mount a JSX file. Returns a RenderHandle.
    /// If the file has a matching .css file (same name), it's auto-loaded.
    /// </summary>
    public RenderHandle? Mount(string filePath)
    {
        filePath = Path.GetFullPath(filePath);

        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] File not found: {filePath}");
            return null;
        }

        var component = new JsxComponent(filePath);

        // Check for matching CSS file
        var cssPath = Path.ChangeExtension(filePath, ".css");
        if (File.Exists(cssPath))
        {
            component.CssFilePath = cssPath;
            component.StyleSheet = CssParser.Parse(File.ReadAllText(cssPath));

            if (!_cssToComponents.ContainsKey(cssPath))
                _cssToComponents[cssPath] = new List<JsxComponent>();
            _cssToComponents[cssPath].Add(component);
        }

        // Load and evaluate
        var renderFunc = LoadComponent(component);
        if (renderFunc == null)
        {
            System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Failed to load: {filePath}");
            return null;
        }

        // Mount with the scheduler
        component.Handle = Scheduler.Mount(renderFunc);
        _components[filePath] = component;

        System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Mounted: {Path.GetFileName(filePath)}");
        return component.Handle;
    }

    /// <summary>
    /// Force reload a specific file. Can be called from C# as fallback
    /// when FileSystemWatcher is unreliable.
    /// </summary>
    public void Reload(string filePath)
    {
        filePath = Path.GetFullPath(filePath);

        if (filePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
        {
            ReloadCss(filePath);
            return;
        }

        if (!_components.TryGetValue(filePath, out var component))
            return;

        ReloadComponent(component);
    }

    /// <summary>
    /// Drain the main-thread queue. Call this from ReactUIBehaviour.Update().
    /// </summary>
    public void Tick()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Tick error: {ex.Message}");
            }
        }
    }

    /// <summary>Register a C# component callable from JSX.</summary>
    public void RegisterComponent(string name, Func<VNode> factory) =>
        _bridge.RegisterComponent(name, factory);

    /// <summary>Register a C# action callable from JSX via call().</summary>
    public void RegisterAction(string name, Delegate action) =>
        _bridge.RegisterAction(name, action);

    public void Dispose()
    {
        _jsxWatcher?.Dispose();
        _cssWatcher?.Dispose();
        _debounceTimer?.Dispose();

        foreach (var component in _components.Values)
            component.Handle?.Dispose();

        _components.Clear();
    }

    // ─── Private ────────────────────────

    private Func<VNode>? LoadComponent(JsxComponent component)
    {
        try
        {
            var source = File.ReadAllText(component.FilePath);
            var transformed = JsxTransformer.Transform(source);
            var prepared = JintBridge.PrepareSource(transformed);
            component.TransformedSource = prepared;

            // Set component-local styles
            _bridge.ComponentStyles = component.StyleSheet;

            return _bridge.Evaluate(prepared, component.ComponentId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Load error ({Path.GetFileName(component.FilePath)}): {ex.Message}");
            return null;
        }
    }

    private void ReloadComponent(JsxComponent component)
    {
        try
        {
            // Reload CSS if present
            if (component.CssFilePath != null && File.Exists(component.CssFilePath))
                component.StyleSheet = CssParser.Parse(File.ReadAllText(component.CssFilePath));

            var renderFunc = LoadComponent(component);
            if (renderFunc == null) return;

            if (component.Handle != null)
            {
                // Hot swap: update the existing mount's render function and re-render
                var rootNode = component.Handle.RootNode;
                if (rootNode?.LastVNode != null)
                {
                    rootNode.LastVNode.RenderFunc = renderFunc;
                    Scheduler.ScheduleRender(component.Handle.RootId);
                    Scheduler.FlushRenders();
                    System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Hot reloaded: {Path.GetFileName(component.FilePath)}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] Reload error ({Path.GetFileName(component.FilePath)}): {ex.Message}");
        }
    }

    private void ReloadCss(string cssPath)
    {
        if (!_cssToComponents.TryGetValue(cssPath, out var components))
            return;

        try
        {
            var newSheet = CssParser.Parse(File.ReadAllText(cssPath));

            foreach (var component in components)
            {
                component.StyleSheet = newSheet;
                ReloadComponent(component);
            }

            System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] CSS reloaded: {Path.GetFileName(cssPath)}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReactUI JSX] CSS reload error: {ex.Message}");
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        lock (_debounceLock)
        {
            _pendingReloads.Add(e.FullPath);
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                HashSet<string> paths;
                lock (_debounceLock)
                {
                    paths = new HashSet<string>(_pendingReloads);
                    _pendingReloads.Clear();
                }

                foreach (var path in paths)
                {
                    var p = path; // capture for closure
                    _mainThreadQueue.Enqueue(() => Reload(p));
                }
            }, null, DebounceMs, Timeout.Infinite);
        }
    }
}
