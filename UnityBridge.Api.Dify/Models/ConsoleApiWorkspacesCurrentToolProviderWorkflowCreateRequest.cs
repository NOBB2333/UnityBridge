namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// 创建工作流工具提供者
/// <para>表示 [POST] /console/api/workspaces/current/tool-provider/workflow/create 接口的请求。</para>
/// </summary>
public class ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置工具名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置工具描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 获取或设置图标信息。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon")]
    public WorkflowToolIcon? Icon { get; set; }

    /// <summary>
    /// 获取或设置工具标签。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// 获取或设置参数列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("parameters")]
    public List<WorkflowToolParameter>? Parameters { get; set; }

    /// <summary>
    /// 获取或设置标签列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("labels")]
    public List<string>? Labels { get; set; }

    /// <summary>
    /// 获取或设置隐私政策。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("privacy_policy")]
    public string? PrivacyPolicy { get; set; }

    /// <summary>
    /// 获取或设置工作流应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflow_app_id")]
    public string WorkflowAppId { get; set; } = string.Empty;
}

/// <summary>
/// 工作流工具图标。
/// </summary>
public class WorkflowToolIcon
{
    /// <summary>
    /// 获取或设置图标内容（emoji 或文本）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// 获取或设置图标背景色。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("background")]
    public string? Background { get; set; }
}

/// <summary>
/// 工作流工具参数。
/// </summary>
public class WorkflowToolParameter
{
    /// <summary>
    /// 获取或设置参数名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置参数描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 获取或设置参数表单类型（如 "llm"）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("form")]
    public string? Form { get; set; }
}

