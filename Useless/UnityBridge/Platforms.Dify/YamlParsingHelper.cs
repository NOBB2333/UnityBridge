namespace UnityBridge.Platforms.Dify;

/// <summary>
/// YAML 文件解析工具。
/// </summary>
public static class YamlParsingHelper
{
    /// <summary>
    /// 从 YAML 内容中提取指定属性值（在首个 app: 块内）。
    /// </summary>
    public static string? ExtractPrimaryAppProperty(string content, string propertyName)
    {
        using var reader = new StringReader(content);
        string? line;
        var appFound = false;
        var prefix = $"{propertyName}:";

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            if (trimmed.Equals("app:", StringComparison.OrdinalIgnoreCase))
            {
                if (!appFound)
                {
                    appFound = true;
                    continue;
                }
                else
                {
                    break; // 遇到第二个 app: 块，停止
                }
            }

            if (appFound && trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed.Substring(prefix.Length).Trim();
                // 移除引号
                return value.Trim('\'', '"');
            }
        }

        return null;
    }

    /// <summary>
    /// 提取首个 app 块的 mode。
    /// </summary>
    public static string? ExtractPrimaryAppMode(string content) 
        => ExtractPrimaryAppProperty(content, "mode");

    /// <summary>
    /// 提取首个 app 块的 name。
    /// </summary>
    public static string? ExtractPrimaryAppName(string content) 
        => ExtractPrimaryAppProperty(content, "name");

    /// <summary>
    /// 判断是否为工作流模式。
    /// </summary>
    public static bool IsWorkflowMode(string? mode)
        => string.Equals(mode, "workflow", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断是否为 Chat 类模式。
    /// </summary>
    public static bool IsChatMode(string? mode)
        => mode is not null && (
            mode.Contains("chat", StringComparison.OrdinalIgnoreCase) ||
            mode.Contains("agent", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 确定模式类别。
    /// </summary>
    public static string DetermineModeCategory(string? mode)
        => IsWorkflowMode(mode) ? "workflow" : IsChatMode(mode) ? "chat" : "other";

    /// <summary>
    /// 加载目录下所有 YAML 文件的信息。
    /// </summary>
    public static async Task<List<YamlAppFileInfo>> LoadYamlFileInfosAsync(string directory)
    {
        var result = new List<YamlAppFileInfo>();
        if (!Directory.Exists(directory))
            return result;

        var files = Directory.GetFiles(directory, "*.yml")
            .Concat(Directory.GetFiles(directory, "*.yaml"));

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var mode = ExtractPrimaryAppMode(content);
                var name = ExtractPrimaryAppName(content);

                result.Add(new YamlAppFileInfo
                {
                    FilePath = file,
                    FileName = Path.GetFileNameWithoutExtension(file),
                    Content = content,
                    Mode = mode,
                    Name = name,
                    Category = DetermineModeCategory(mode)
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load {file}: {ex.Message}");
            }
        }

        return result;
    }
}

/// <summary>
/// 表示解析后的 YAML 文件信息。
/// </summary>
public class YamlAppFileInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Mode { get; init; }
    public string? Name { get; init; }
    public string Category { get; init; } = "other";
}
