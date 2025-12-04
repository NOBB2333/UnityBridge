using System.Text.Json.Serialization;

namespace UnityBridge.Options;

/// <summary>
/// 对应 <c>Configuration/SionWebApp.json</c> 配置。
/// </summary>
public class SionWebAppOptions
{
    /// <summary>
    /// 获取或设置 WoWeb 主机配置。
    /// </summary>
    [JsonPropertyName("Host")]
    public SionWebAppHostOptions? Host { get; set; }
}

/// <summary>
/// WoWeb 专用 host 配置，支持 Company API 所需的各类 Header。
/// </summary>
public class SionWebAppHostOptions
{
    [JsonPropertyName("BaseUrl")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("Token")]
    public string? Token { get; set; }

    [JsonPropertyName("OverToken")]
    public string? OverToken { get; set; }

    [JsonPropertyName("TenantId")]
    public string? TenantId { get; set; }

    [JsonPropertyName("Origin")]
    public string? Origin { get; set; }

    [JsonPropertyName("Referer")]
    public string? Referer { get; set; }

    [JsonPropertyName("AcceptLanguage")]
    public string? AcceptLanguage { get; set; }

    [JsonPropertyName("UserAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 其他需要直接附加到 FlurlClient 的额外请求头。
    /// </summary>
    [JsonPropertyName("Headers")]
    public Dictionary<string, string>? Headers { get; set; }
}

