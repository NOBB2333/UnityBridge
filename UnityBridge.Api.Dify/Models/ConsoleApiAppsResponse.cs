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
            /// 获取或设置标签列表（可能为字符串或对象，保持 JsonElement 以兼容不同结构）。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("tags")]
            public System.Text.Json.JsonElement[]? Tags { get; set; }

            /// <summary>
            /// 获取或设置最大并发请求数。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("max_active_requests")]
            public int? MaxActiveRequests { get; set; }

            /// <summary>
            /// 获取或设置模型配置。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("model_config")]
            public System.Text.Json.JsonElement? ModelConfig { get; set; }

            /// <summary>
            /// 获取或设置工作流信息。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("workflow")]
            public WorkflowInfo? Workflow { get; set; }

            /// <summary>
            /// 获取或设置是否使用图标作为答案图标。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("use_icon_as_answer_icon")]
            public bool? UseIconAsAnswerIcon { get; set; }

            /// <summary>
            /// 获取或设置创建者。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("created_by")]
            public string? CreatedBy { get; set; }

            /// <summary>
            /// 获取或设置创建时间戳。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("created_at")]
            public long? CreatedAt { get; set; }

            /// <summary>
            /// 获取或设置更新者。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("updated_by")]
            public string? UpdatedBy { get; set; }

            /// <summary>
            /// 获取或设置更新时间戳。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
            public long? UpdatedAt { get; set; }

            /// <summary>
            /// 获取或设置访问模式。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("access_mode")]
            public string? AccessMode { get; set; }

            /// <summary>
            /// 获取或设置创建者姓名。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("create_user_name")]
            public string? CreateUserName { get; set; }

            /// <summary>
            /// 获取或设置作者姓名。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("author_name")]
            public string? AuthorName { get; set; }

            /// <summary>
            /// 表示工作流信息。
            /// </summary>
            public class WorkflowInfo
            {
                [System.Text.Json.Serialization.JsonPropertyName("id")]
                public string? Id { get; set; }

                [System.Text.Json.Serialization.JsonPropertyName("created_by")]
                public string? CreatedBy { get; set; }

                [System.Text.Json.Serialization.JsonPropertyName("created_at")]
                public long? CreatedAt { get; set; }

                [System.Text.Json.Serialization.JsonPropertyName("updated_by")]
                public string? UpdatedBy { get; set; }

                [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
                public long? UpdatedAt { get; set; }
            }
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