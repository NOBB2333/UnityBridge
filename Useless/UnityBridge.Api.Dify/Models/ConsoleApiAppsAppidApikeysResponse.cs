namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps/{app_id}/api-keys 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsAppidApikeysResponse : DifyApiResponse
{
    public class Types
    {
        public class ApiKey
        {
            /// <summary>
            /// 获取或设置 ID。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string Id { get; set; } = default!;

            /// <summary>
            /// 获取或设置类型。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string? Type { get; set; }

            /// <summary>
            /// 获取或设置 Token。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("token")]
            public string? Token { get; set; }

            /// <summary>
            /// 获取或设置最后使用时间（时间戳秒）。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("last_used_at")]
            public long? LastUsedAt { get; set; }

            /// <summary>
            /// 获取或设置创建时间（时间戳秒）。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("created_at")]
            public long? CreatedAt { get; set; }
        }
    }

    /// <summary>
    /// 获取或设置 API Key 列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public Types.ApiKey[] Data { get; set; } = default!;
}