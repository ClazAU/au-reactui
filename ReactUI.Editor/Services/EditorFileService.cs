using System;
using System.IO;
using System.Linq;

namespace ReactUI.Editor.Services;

/// <summary>
/// File I/O for the editor. Lists, reads, and writes .jsx/.css files.
/// </summary>
public static class EditorFileService
{
    /// <summary>
    /// List all .jsx and .css files in a directory (recursively).
    /// Returns paths relative to the directory.
    /// </summary>
    public static string[] ListFiles(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<string>();

        var jsxFiles = Directory.GetFiles(directory, "*.jsx", SearchOption.AllDirectories);
        var cssFiles = Directory.GetFiles(directory, "*.css", SearchOption.AllDirectories);

        return jsxFiles.Concat(cssFiles)
            .Select(f => Path.GetRelativePath(directory, f).Replace('\\', '/'))
            .OrderBy(f => f)
            .ToArray();
    }

    /// <summary>Read file content.</summary>
    public static string ReadFile(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    /// <summary>Write file content and trigger hot-reload.</summary>
    public static void WriteFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, content);
    }

    /// <summary>Create a new file with starter template.</summary>
    public static string CreateNewFile(string directory, string name)
    {
        if (!name.EndsWith(".jsx"))
            name += ".jsx";

        var path = Path.Combine(directory, name);
        var componentName = Path.GetFileNameWithoutExtension(name);
        componentName = char.ToUpper(componentName[0]) + componentName.Substring(1);

        var template = $@"function {componentName}() {{
  const [count, setCount] = useState(0);

  return (
    <div style={{{{ padding: 20, background: '#1a1a2e', borderRadius: 12, gap: 12 }}}}>
      <div style={{{{ fontSize: 18, fontWeight: 700, color: '#e0e0e0' }}}}>
        {componentName}
      </div>
      <div style={{{{ color: '#7c3aed' }}}}>
        {{'Count: ' + count}}
      </div>
      <button onClick={{() => setCount(count + 1)}} style={{{{ padding: [6, 12], background: '#7c3aed', color: '#fff', borderRadius: 6 }}}}>
        +1
      </button>
    </div>
  );
}}

export default {componentName};
";
        File.WriteAllText(path, template);
        return path;
    }
}
