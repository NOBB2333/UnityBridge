namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [DELETE] /console/api/apps/{app_id}/api-keys/{key_id} 接口的请求。</para>
/// </summary>
public class ConsoleApiAppsAppidApikeysKeyidRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Key ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string KeyId { get; set; } = string.Empty;
}