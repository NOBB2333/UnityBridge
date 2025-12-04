namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/lib/all/listBackAll 接口的请求。</para>
/// </summary>
public class LangwellDocServerKnowledgeLibAllListBackAllRequest : CompanyApiRequest
{
}

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/lib/all/listBackAll 接口的响应。</para>
/// </summary>
public class LangwellDocServerKnowledgeLibAllListBackAllResponse : CompanyApiResponse
{
    public static class Types
    {
        public class KnowledgeLibInfo
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string? Id { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("dirId")]
            public string? DirectoryId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("libName")]
            public string? Name { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("libStatus")]
            public int? Status { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("libDesc")]
            public string? Description { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("entityType")]
            public string? EntityType { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("dataType")]
            public string? DataType { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("retrievalType")]
            public string? RetrievalType { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("ancestorsName")]
            public string? AncestorsName { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("allFileSize")]
            public decimal? AllFileSize { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("knCount")]
            public string? KnowledgeCount { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("fileNum")]
            public string? FileNumber { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("participateType")]
            public string? ParticipateType { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("tags")]
            public string? Tags { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("entryType")]
            public string? EntryType { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("splitType")]
            public string? SplitType { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("embeddingModel")]
            public string? EmbeddingModel { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("searchTopK")]
            public string? SearchTopK { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("searchScore")]
            public string? SearchScore { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("collection")]
            public string? Collection { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("visibleRange")]
            public string? VisibleRange { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("databaseId")]
            public string? DatabaseId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("isCollect")]
            public string? IsCollect { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("createBy")]
            public string? CreatedBy { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("lastSyncTime")]
            public string? LastSyncTime { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("setIdAndPath")]
            public string? SetIdAndPath { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("libBindLocalDetailList")]
            public string? BindLocalDetailList { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("bindPath")]
            public string? BindPath { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("createBy_text")]
            public string? CreatedByText { get; set; }
        }

        public class KnowledgeQuery
        {
            [System.Text.Json.Serialization.JsonPropertyName("knowledgeLibInfoVOS")]
            public IList<KnowledgeLibInfo>? KnowledgeLibraries { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("knowledgeKnInfoVOS")]
            public object? KnowledgeKnInfos { get; set; }
        }

        public class ResponseData
        {
            [System.Text.Json.Serialization.JsonPropertyName("enterpriseQueryVO")]
            public KnowledgeQuery? EnterpriseQuery { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("individualQueryVO")]
            public KnowledgeQuery? IndividualQuery { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("projectQueryVO")]
            public KnowledgeQuery? ProjectQuery { get; set; }
        }
    }

    /// <summary>
    /// 获取或设置响应数据。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public Types.ResponseData? Data { get; set; }
}

