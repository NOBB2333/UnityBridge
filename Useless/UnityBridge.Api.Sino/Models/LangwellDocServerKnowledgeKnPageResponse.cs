using System.Text.Json.Serialization;

namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-doc-server/knowledge/kn/page 接口的响应。</para>
/// </summary>
public class LangwellDocServerKnowledgeKnPageResponse : CompanyApiResponse
{
    public static class Types
    {
        public class PageResult
        {
            [JsonPropertyName("records")]
            public IList<KnowledgeFileRecord>? Records { get; set; }

            [JsonPropertyName("total")]
            public string? Total { get; set; }

            [JsonPropertyName("size")]
            public string? Size { get; set; }

            [JsonPropertyName("current")]
            public string? Current { get; set; }

            [JsonPropertyName("pages")]
            public string? Pages { get; set; }
        }

        public class KnowledgeFileRecord
        {
             [JsonPropertyName("id")]
             public string? Id { get; set; }
             
             [JsonPropertyName("knName")]
             public string? FileName { get; set; }

             [JsonPropertyName("knType")]
             public string? FileType { get; set; }

             [JsonPropertyName("knSize")]
             public double? FileSize { get; set; }

             [JsonPropertyName("knStatus")]
             public string? AnalyzeStatus { get; set; }

             [JsonPropertyName("tags")]
             public string? Tags { get; set; }
        }
    }

    /// <summary>
    /// 获取或设置分页数据。
    /// </summary>
    [JsonPropertyName("data")]
    public Types.PageResult? Data { get; set; }
}
