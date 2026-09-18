namespace Rebirth.Content;

/// <summary>扫描 content/scenes 目录加载 Resource，避免 Main 逐条写死路径。</summary>
public static class ContentDirectory
{
    /// <summary>加载目录下所有可转为 T 的资源，按路径排序保证结果稳定。</summary>
    public static List<T> LoadAll<T>(string directory, string extension = ".tres") where T : class
    {
        var result = new List<T>();
        foreach (var path in ListResourcePaths(directory, extension))
        {
            var loaded = ResourceLoader.Load(path, "", ResourceLoader.CacheMode.Ignore);
            if (loaded is T typed)
            {
                result.Add(typed);
            }
        }

        return result;
    }

    /// <summary>本局只需一份时使用：空则报错，多份时取路径排序后的第一份。</summary>
    public static T? RequireOne<T>(string directory, string extension = ".tres") where T : Resource
    {
        var all = LoadAll<T>(directory, extension);
        if (all.Count == 0)
        {
            GameLog.Error($"未找到 {typeof(T).Name}：{directory}");
            return null;
        }

        if (all.Count > 1)
        {
            GameLog.Warn($"目录有 {all.Count} 个 {typeof(T).Name}，本局使用 {all[0].ResourcePath}");
        }

        return all[0];
    }

    /// <summary>在目录（不递归）中找根节点脚本匹配的 PackedScene，用于 player/enemy 预制体。</summary>
    public static PackedScene? FindSceneByRootScript(string directory, string scriptFileName)
    {
        foreach (var path in ListResourcePaths(directory, ".tscn"))
        {
            var scene = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore);
            if (scene != null && RootScriptEndsWith(scene, scriptFileName))
            {
                return scene;
            }
        }

        GameLog.Error($"未找到根脚本为 {scriptFileName} 的场景：{directory}");
        return null;
    }

    static List<string> ListResourcePaths(string directory, string extension)
    {
        var paths = new List<string>();
        foreach (var fileName in DirAccess.GetFilesAt(directory))
        {
            // 导出包里资源会带 .remap，去掉后才能交给 ResourceLoader
            var resourceName = fileName.EndsWith(".remap", StringComparison.OrdinalIgnoreCase)
                ? fileName[..^".remap".Length]
                : fileName;
            if (!resourceName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            paths.Add($"{directory}/{resourceName}");
        }

        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    static bool RootScriptEndsWith(PackedScene scene, string scriptFileName)
    {
        var state = scene.GetState();
        if (state.GetNodeCount() == 0)
        {
            return false;
        }

        for (var i = 0; i < state.GetNodePropertyCount(0); i++)
        {
            if (state.GetNodePropertyName(0, i) != "script")
            {
                continue;
            }

            var path = ScriptPath(state.GetNodePropertyValue(0, i));
            return path.EndsWith(scriptFileName, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    static string ScriptPath(Variant value)
    {
        if (value.AsGodotObject() is Resource resource && !string.IsNullOrEmpty(resource.ResourcePath))
        {
            return resource.ResourcePath;
        }

        return value.AsString();
    }
}
