using System.Text.Json.Serialization;

namespace UnityBridge.Options;

/// <summary>
/// 对应 <c>Configuration/DifyMigration.json</c> 的整体结构。
/// </summary>
public class DifyMigrationOptions
{
    [JsonPropertyName("Host")]
    public EndpointOptions? Host { get; set; }

    [JsonPropertyName("NewHost")]
    public EndpointOptions? NewHost { get; set; }
}



/// <summary>
/// 通用 HTTP 端点配置（Upload/Download 等共用）。
/// 支持 Headers 字典或兼容旧的 HeaderSpec 字符串。
/// </summary>
public class EndpointOptions
{
    [JsonPropertyName("BaseUrl")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("Headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// 兼容旧的 HeaderSpec 字符串写法（可选）。
    /// </summary>
    [JsonPropertyName("HeaderSpec")]
    public string? HeaderSpec { get; set; }
}
