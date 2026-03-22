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
            public class Types
            {
                public class FileItem
                {
                    /// <summary>
                    /// 获取或设置文件类型。
                    /// </summary>
                    [System.Text.Json.Serialization.JsonPropertyName("type")]
                    public string? Type { get; set; }

                    /// <summary>
                    /// 获取或设置文件传输方式。
                    /// </summary>
                    [System.Text.Json.Serialization.JsonPropertyName("transfer_method")]
                    public string? TransferMethod { get; set; }

                    /// <summary>
                    /// 获取或设置上传后的文件 ID。
                    /// </summary>
                    [System.Text.Json.Serialization.JsonPropertyName("upload_file_id")]
                    public string? UploadFileId { get; set; }
                }
            }

            /// <summary>
            /// 获取或设置输入数据。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("inputs")]
            public object Inputs { get; set; } = new object();

            /// <summary>
            /// 获取或设置文件列表。
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName("files")]
            public IList<Types.FileItem>? Files { get; set; }

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
    [System.Text.Json.Serialization.JsonPropertyName("insId")]
    public string InsId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置业务类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("bizType")]
    public string BizType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置业务 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("bizId")]
    public string BizId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Agent ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
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

    /// <summary>
    /// 获取或设置操作类型（如 "writing" 用于写作功能）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("action")]
    public string? Action { get; set; }
}

/// <summary>
/// <para>写作功能的输入数据模型。</para>
/// </summary>
public class WritingInputs
{
    /// <summary>
    /// 获取或设置回复内容。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("reply")]
    public string? Reply { get; set; }

    /// <summary>
    /// 获取或设置文档文件列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("docFiles")]
    public IList<object>? DocFiles { get; set; }

    /// <summary>
    /// 获取或设置个人知识库 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("personalLibs")]
    public string? PersonalLibs { get; set; }

    /// <summary>
    /// 获取或设置 Token。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("Token")]
    public string? Token { get; set; }

    /// <summary>
    /// 获取或设置租户 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tenantid")]
    public string? TenantId { get; set; }

    /// <summary>
    /// 获取或设置输出模板。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("outputTemplate")]
    public string? OutputTemplate { get; set; }

    /// <summary>
    /// 获取或设置长度要求（如 "不超过200字"）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("length")]
    public string? Length { get; set; }

    /// <summary>
    /// 获取或设置语言（如 "中文(简体)"）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("lang")]
    public string? Language { get; set; }

    /// <summary>
    /// 获取或设置语气风格（如 "详细的"）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tone")]
    public string? Tone { get; set; }

    /// <summary>
    /// 获取或设置选中的知识库数组。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("selectKnowledgeArr")]
    public IList<object>? SelectedKnowledgeArray { get; set; }

    /// <summary>
    /// 获取或设置本地文件列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("localFile")]
    public IList<object>? LocalFiles { get; set; }
}

/// <summary>
/// <para>对话功能的输入数据模型。</para>
/// </summary>
public class ChatInputs
{
    /// <summary>
    /// 获取或设置查询内容。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// 获取或设置个人知识库 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("personalLibs")]
    public string? PersonalLibs { get; set; }

    /// <summary>
    /// 获取或设置知识库 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("knIds")]
    public string? KnowledgeIds { get; set; }

    /// <summary>
    /// 获取或设置 Token。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("Token")]
    public string? Token { get; set; }

    /// <summary>
    /// 获取或设置租户 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tenantid")]
    public string? TenantId { get; set; }
}