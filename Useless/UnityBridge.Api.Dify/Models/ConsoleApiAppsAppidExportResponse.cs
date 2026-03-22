namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps/{app_id}/export 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsAppidExportResponse : DifyApiResponse
{
    /// <summary>
    /// 获取 YAML 内容。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// 获取原始响应文本（如果不是 JSON 格式）。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? RawText { get; set; }
}