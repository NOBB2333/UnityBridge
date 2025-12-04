namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/lib/individual/add 接口的请求。</para>
/// </summary>
public class LangwellDocServerKnowledgeLibIndividualAddRequest : CompanyApiRequest
{
    /// <summary>
    /// 获取或设置知识库名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("libName")]
    public string LibName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置数据类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("dataType")]
    public string DataType { get; set; } = "text";

    /// <summary>
    /// 获取或设置知识库描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("libDesc")]
    public string? Description { get; set; }

    /// <summary>
    /// 获取或设置实体类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("entityType")]
    public string EntityType { get; set; } = "individual";

    /// <summary>
    /// 获取或设置检索类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retrievalType")]
    public string RetrievalType { get; set; } = "vector";
}

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/lib/individual/add 接口的响应。</para>
/// </summary>
public class LangwellDocServerKnowledgeLibIndividualAddResponse : CompanyApiResponse
{
    /// <summary>
    /// 获取或设置新建的知识库 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public string? LibraryId { get; set; }
}

