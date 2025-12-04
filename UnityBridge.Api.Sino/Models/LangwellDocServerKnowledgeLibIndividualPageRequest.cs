namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/lib/individual/page 接口的请求。</para>
/// </summary>
public class LangwellDocServerKnowledgeLibIndividualPageRequest : CompanyApiRequest
{
    public static class Types
    {
        public class Filter
        {
            /// <summary>
            /// 获取或设置知识库名称。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("libName")]
            public string? LibName { get; set; }
        }
    }

    /// <summary>
    /// 获取或设置页码（从 1 开始）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("pageNum")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 获取或设置页大小。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 获取或设置筛选条件。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("entity")]
    public Types.Filter Entity { get; set; } = new Types.Filter();
}

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/lib/individual/page 接口的响应。</para>
/// </summary>
public class LangwellDocServerKnowledgeLibIndividualPageResponse : CompanyApiResponse
{
    public static class Types
    {
        public class Record
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

        public class PageResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("records")]
            public IList<Record>? Records { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("total")]
            public string? Total { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("size")]
            public string? Size { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("current")]
            public string? Current { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("pages")]
            public string? Pages { get; set; }
        }
    }

    /// <summary>
    /// 获取或设置分页数据。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public Types.PageResult? Data { get; set; }
}

