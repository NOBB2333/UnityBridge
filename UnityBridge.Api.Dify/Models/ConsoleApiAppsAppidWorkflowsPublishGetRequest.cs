namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// 获取已经发布的工作流 具体信息
/// <para>表示 [GET] /console/api/apps/{app_id}/workflows/publish 接口的请求。</para>
/// </summary>
public class ConsoleApiAppsAppidWorkflowsPublishGetRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AppId { get; set; } = string.Empty;
}

