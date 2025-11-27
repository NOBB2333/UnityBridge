namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/apps/{app_id}/api-keys 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsAppidApikeysCreateResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置 API Key 信息。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public ConsoleApiAppsAppidApikeysResponse.Types.ApiKey Data { get; set; } = default!;
}