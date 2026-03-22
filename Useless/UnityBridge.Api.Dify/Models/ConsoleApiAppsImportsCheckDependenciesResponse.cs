namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps/imports/{import_id}/check-dependencies 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsImportsCheckDependenciesResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置泄露的依赖项列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("leaked_dependencies")]
    public List<LeakedDependency>? LeakedDependencies { get; set; }
}

/// <summary>
/// 表示一个泄露的依赖项。
/// </summary>
public class LeakedDependency
{
    /// <summary>
    /// 获取或设置依赖类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 获取或设置依赖值。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public DependencyValue? Value { get; set; }

    /// <summary>
    /// 获取或设置当前标识符。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("current_identifier")]
    public string? CurrentIdentifier { get; set; }
}

/// <summary>
/// 表示依赖项的值。
/// </summary>
public class DependencyValue
{
    /// <summary>
    /// 获取或设置市场插件唯一标识符。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("marketplace_plugin_unique_identifier")]
    public string? MarketplacePluginUniqueIdentifier { get; set; }

    /// <summary>
    /// 获取或设置版本号。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public string? Version { get; set; }
}

