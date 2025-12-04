namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/file/individual/upload 接口的请求。</para>
/// </summary>
public class LangwellDocServerKnowledgeFileIndividualUploadRequest : CompanyApiRequest
{
    /// <summary>
    /// 获取或设置目标知识库 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("libId")]
    public string LibraryId { get; set; } = string.Empty;

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

    /// <summary>
    /// 获取或设置额外的表单字段。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IDictionary<string, string>? ExtraFormFields { get; set; }
}

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/file/individual/upload 接口的响应。</para>
/// </summary>
public class LangwellDocServerKnowledgeFileIndividualUploadResponse : CompanyApiResponse
{
    /// <summary>
    /// 获取或设置上传结果。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public object? Data { get; set; }
}

