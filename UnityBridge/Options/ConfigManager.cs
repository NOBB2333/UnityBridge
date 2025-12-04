using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace UnityBridge.Options;

/// <summary>
/// 简单的 Options 管理器：启动时读取 <c>Configuration</c> 目录下的所有 json，
/// 允许通过节名称直接绑定为强类型对象。
/// </summary>
public static class ConfigManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = ConfigJsonSerializerContext.Default
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly Lazy<Dictionary<string, string>> _sections = new(LoadAllSections);

    private static Dictionary<string, string> LoadAllSections()
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var configDir = Path.Combine(baseDir, "Configuration");
            if (!Directory.Exists(configDir))
            {
                Console.Error.WriteLine($"配置目录不存在: {configDir}");
                return sections;
            }

            foreach (var file in Directory.EnumerateFiles(configDir, "*.json", SearchOption.TopDirectoryOnly))
            {
                var json = File.ReadAllText(file);
                using var document = JsonDocument.Parse(json, DocumentOptions);
                var root = document.RootElement;
                var fileKey = Path.GetFileNameWithoutExtension(file);

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in root.EnumerateObject())
                    {
                        sections[property.Name] = property.Value.GetRawText();
                    }

                    sections[fileKey] = root.GetRawText();
                }
                else
                {
                    sections[fileKey] = root.GetRawText();
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"扫描配置目录失败: {ex.Message}");
        }

        return sections;
    }

    [RequiresUnreferencedCode("Options 绑定需要完整的序列化上下文。")]
    private static T BindSection<T>(string sectionName) where T : new()
    {
        try
        {
            var sections = _sections.Value;
            if (!sections.TryGetValue(sectionName, out var raw))
            {
                Console.Error.WriteLine($"配置节 \"{sectionName}\" 未找到，请确认 Configuration 目录下的 json 文件命名及内容。");
                return new T();
            }

            return JsonSerializer.Deserialize<T>(raw, SerializerOptions) ?? new T();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"绑定配置节 \"{sectionName}\" 失败: {ex.Message}");
            return new T();
        }
    }

    private static readonly Lazy<EndpointOptions> _downloadOptions = new(() => BindSection<EndpointOptions>("Download"));
    private static readonly Lazy<EndpointOptions> _uploadOptions = new(() => BindSection<EndpointOptions>("Upload"));
    private static readonly Lazy<DifyMigrationOptions> _difyMigrationOptions = new(() => BindSection<DifyMigrationOptions>("DifyMigration"));
    private static readonly Lazy<SionWebAppOptions> _sionWebAppOptions = new(() => BindSection<SionWebAppOptions>("SionWebApp"));

    public static EndpointOptions Download => _downloadOptions.Value;
    public static EndpointOptions Upload => _uploadOptions.Value;
    public static DifyMigrationOptions DifyMigration => _difyMigrationOptions.Value;
    public static SionWebAppOptions SionWebApp => _sionWebAppOptions.Value;
}
