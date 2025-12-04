namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/workspaces/current/tool-provider/workflow/get 接口的响应。</para>
/// </summary>
public class ConsoleApiWorkspacesCurrentToolProviderWorkflowGetResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置工具名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 获取或设置工具标签。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// 获取或设置工作流工具 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflow_tool_id")]
    public string? WorkflowToolId { get; set; }

    /// <summary>
    /// 获取或设置工作流应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflow_app_id")]
    public string? WorkflowAppId { get; set; }

    /// <summary>
    /// 获取或设置图标信息。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon")]
    public WorkflowToolIcon? Icon { get; set; }

    /// <summary>
    /// 获取或设置工具描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 获取或设置参数列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("parameters")]
    public List<WorkflowToolParameter>? Parameters { get; set; }

    /// <summary>
    /// 获取或设置工具详细信息。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tool")]
    public WorkflowToolDetail? Tool { get; set; }

    /// <summary>
    /// 获取或设置是否已同步。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("synced")]
    public bool? Synced { get; set; }

    /// <summary>
    /// 获取或设置隐私政策。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("privacy_policy")]
    public string? PrivacyPolicy { get; set; }
}

/// <summary>
/// 工作流工具详细信息。
/// </summary>
public class WorkflowToolDetail
{
    /// <summary>
    /// 获取或设置作者。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// 获取或设置工具名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 获取或设置多语言标签。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public Dictionary<string, string>? Label { get; set; }

    /// <summary>
    /// 获取或设置多语言描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }

    /// <summary>
    /// 获取或设置参数列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("parameters")]
    public List<WorkflowToolParameterDetail>? Parameters { get; set; }

    /// <summary>
    /// 获取或设置标签列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("labels")]
    public List<string>? Labels { get; set; }

    /// <summary>
    /// 获取或设置输出架构。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("output_schema")]
    public object? OutputSchema { get; set; }
}

/// <summary>
/// 工作流工具参数详细信息。
/// </summary>
public class WorkflowToolParameterDetail
{
    /// <summary>
    /// 获取或设置参数名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 获取或设置多语言标签。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public Dictionary<string, string>? Label { get; set; }

    /// <summary>
    /// 获取或设置多语言占位符。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("placeholder")]
    public Dictionary<string, string>? Placeholder { get; set; }

    /// <summary>
    /// 获取或设置作用域。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// 获取或设置是否自动生成。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("auto_generate")]
    public bool? AutoGenerate { get; set; }

    /// <summary>
    /// 获取或设置模板。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("template")]
    public string? Template { get; set; }

    /// <summary>
    /// 获取或设置是否必需。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public bool? Required { get; set; }

    /// <summary>
    /// 获取或设置默认值。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("default")]
    public object? Default { get; set; }

    /// <summary>
    /// 获取或设置最小值。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("min")]
    public object? Min { get; set; }

    /// <summary>
    /// 获取或设置最大值。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("max")]
    public object? Max { get; set; }

    /// <summary>
    /// 获取或设置精度。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("precision")]
    public object? Precision { get; set; }

    /// <summary>
    /// 获取或设置选项列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("options")]
    public List<object>? Options { get; set; }

    /// <summary>
    /// 获取或设置参数类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 获取或设置多语言人工描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("human_description")]
    public Dictionary<string, string>? HumanDescription { get; set; }

    /// <summary>
    /// 获取或设置表单类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("form")]
    public string? Form { get; set; }

    /// <summary>
    /// 获取或设置 LLM 描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("llm_description")]
    public string? LlmDescription { get; set; }

    /// <summary>
    /// 获取或设置输入架构。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("input_schema")]
    public object? InputSchema { get; set; }
}

