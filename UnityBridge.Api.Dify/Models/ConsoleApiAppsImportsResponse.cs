namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/apps/imports 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsImportsResponse : DifyApiResponse
{
    // 响应数据结构可能与 App 详情类似，或者只是简单的成功消息
    // 根据 Rust 代码，它包含 data 字段，但具体结构未定义，暂时留空或使用 object
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public object? Data { get; set; }
}