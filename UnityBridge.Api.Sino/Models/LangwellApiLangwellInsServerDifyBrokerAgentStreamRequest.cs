namespace UnityBridge.Api.Sino.Models;

/// <summary>
/// <para>表示 [POST] /langwell-api/langwell-ins-server/dify/broker/agent/stream 接口的请求。</para>
/// </summary>
public class LangwellApiLangwellInsServerDifyBrokerAgentStreamRequest : CompanyApiRequest
{
    public class Types
    {
        public class DifyJsonData
        {
            /// <summary>
            /// 获取或设置输入数据。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("inputs")]
            public object Inputs { get; set; } = new object();

            /// <summary>
            /// 获取或设置文件列表。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("files")]
            public string[]? Files { get; set; }

            /// <summary>
            /// 获取或设置响应模式。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("response_mode")]
            public string ResponseMode { get; set; } = "streaming";

            /// <summary>
            /// 获取或设置用户 ID。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("user")]
            public string User { get; set; } = string.Empty;

            /// <summary>
            /// 获取或设置自动生成名称。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("auto_generate_name")]
            public string? AutoGenerateName { get; set; }

            /// <summary>
            /// 获取或设置查询内容。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("query")]
            public string Query { get; set; } = string.Empty;

            /// <summary>
            /// 获取或设置会话 ID。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("conversation_id")]
            public string? ConversationId { get; set; }
        }
    }

    /// <summary>
    /// 获取或设置实例 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("ins_id")]
    public string InsId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置业务类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("biz_type")]
    public string BizType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置业务 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("biz_id")]
    public string BizId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Agent ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置路径。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Dify JSON 数据。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("dify_json")]
    public Types.DifyJsonData DifyJson { get; set; } = new Types.DifyJsonData();

    /// <summary>
    /// 获取或设置查询内容。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置会话 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }
}