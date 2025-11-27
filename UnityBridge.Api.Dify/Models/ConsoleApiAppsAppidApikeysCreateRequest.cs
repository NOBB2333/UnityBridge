namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/apps/{app_id}/api-keys 接口的请求。</para>
/// </summary>
public class ConsoleApiAppsAppidApikeysCreateRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AppId { get; set; } = string.Empty;
}