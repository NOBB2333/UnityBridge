namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsResponse : DifyApiResponse
{
    public class Types
    {
        public class App
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
    }

    /// <summary>
    /// 获取或设置当前页码。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// 获取或设置每页数量。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>
    /// 获取或设置总数量。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// 获取或设置是否有更多数据。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    /// <summary>
    /// 获取或设置应用列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public Types.App[] Data { get; set; } = default!;
}