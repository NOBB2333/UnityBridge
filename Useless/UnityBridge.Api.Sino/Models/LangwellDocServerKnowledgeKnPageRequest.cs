using System.Text.Json.Serialization;

namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/kn/page 接口的请求。</para>
/// </summary>
public class LangwellDocServerKnowledgeKnPageRequest : CompanyApiRequest
{
    public static class Types
    {
        public class Filter
        {
            /// <summary>
            /// 获取或设置知识库 ID。
            /// </summary>
            [JsonPropertyName("libId")]
            public string? LibraryId { get; set; }

            /// <summary>
            /// 获取或设置文件标题/名称筛选。
            /// </summary>
            [JsonPropertyName("knName")]
            public string? Title { get; set; }
        }
    }

    /// <summary>
    /// 获取或设置页码（从 1 开始）。
    /// </summary>
    [JsonPropertyName("pageNum")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 获取或设置页大小。
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 获取或设置筛选条件。
    /// </summary>
    [JsonPropertyName("entity")]
    public Types.Filter Entity { get; set; } = new Types.Filter();
}
