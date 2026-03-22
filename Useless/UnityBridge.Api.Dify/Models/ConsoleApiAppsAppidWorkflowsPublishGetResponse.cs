using System.Text.Json;

namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps/{app_id}/workflows/publish 接口的响应。</para>
/// <para>该接口返回复杂的工作流结构，使用 JsonElement 存储原始数据以便手动解析。</para>
/// </summary>
public class ConsoleApiAppsAppidWorkflowsPublishGetResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置原始 JSON 数据。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    /// <summary>
    /// 获取或设置原始响应文本。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? RawText { get; set; }
}

