namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps/{app_id}/export 接口的请求。</para>
/// </summary>
public class ConsoleApiAppsAppidExportRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置是否包含密钥。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool? IncludeSecret { get; set; }
}