namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/apps/{app_id}/workflows/publish 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsAppidWorkflowsPublishResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置结果。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("result")]
    public string? Result { get; set; }

    /// <summary>
    /// 获取或设置创建时间戳。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }
}

