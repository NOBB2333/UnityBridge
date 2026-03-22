namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-ins-server/dify/broker/formData 接口的请求。</para>
/// </summary>
public class LangwellInsServerDifyBrokerFormDataRequest : CompanyApiRequest
{
    /// <summary>
    /// 获取或设置目标路径。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; } = "/files/upload";

    /// <summary>
    /// 获取或设置 Agent ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置用户 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置知识库名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("libName")]
    public string? LibraryName { get; set; }

    /// <summary>
    /// 获取或设置知识库描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("libDesc")]
    public string? LibraryDescription { get; set; }

    /// <summary>
    /// 获取或设置标记。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("flag")]
    public string Flag { get; set; } = "file";

    /// <summary>
    /// 获取或设置文件路径。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? FilePath { get; set; }

    /// <summary>
    /// 获取或设置文件名。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? FileName { get; set; }

    /// <summary>
    /// 获取或设置文件内容类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// 获取或设置文件字节数组。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public byte[]? FileBytes { get; set; }
}

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-ins-server/dify/broker/formData 接口的响应。</para>
/// </summary>
public class LangwellInsServerDifyBrokerFormDataResponse : CompanyApiResponse
{
    public static class Types
    {
        public class FileDescriptor
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string? Id { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string? Name { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("size")]
            public long? Size { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("extension")]
            public string? Extension { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("mime_type")]
            public string? MimeType { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("created_by")]
            public string? CreatedBy { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("created_at")]
            public long? CreatedAt { get; set; }
        }
    }

    /// <summary>
    /// 获取或设置响应数据。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public Types.FileDescriptor? Data { get; set; }
}

