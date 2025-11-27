namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps/{app_id} 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsAppidResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    /// <summary>
    /// 获取或设置应用名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// 获取或设置应用描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 获取或设置应用模式。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// 获取或设置图标类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon_type")]
    public string? IconType { get; set; }

    /// <summary>
    /// 获取或设置图标。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// 获取或设置图标背景。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon_background")]
    public string? IconBackground { get; set; }

    /// <summary>
    /// 获取或设置图标 URL。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    /// <summary>
    /// 获取或设置标签列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public string[] Tags { get; set; } = default!;
}