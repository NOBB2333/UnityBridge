namespace UnityBridge.Api.Sino.Events;

/// <summary>
/// 表示 Agent Stream 事件。
/// </summary>
public class AgentStreamEvent
{
    /// <summary>
    /// 获取或设置事件类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>
    /// 获取或设置事件数据。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// 获取或设置 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? Id { get; set; }
}