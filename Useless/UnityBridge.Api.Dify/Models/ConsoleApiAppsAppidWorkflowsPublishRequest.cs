namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// 发布 工作流 的功能，就是点击发布按钮
/// <para>表示 [POST] /console/api/apps/{app_id}/workflows/publish 接口的请求。</para>
/// </summary>
public class ConsoleApiAppsAppidWorkflowsPublishRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置标记名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("marked_name")]
    public string? MarkedName { get; set; }

    /// <summary>
    /// 获取或设置标记注释。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("marked_comment")]
    public string? MarkedComment { get; set; }
}

