namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [POST] /console/api/workspaces/current/tool-provider/workflow/update 接口的请求。</para>
/// </summary>
public class ConsoleApiWorkspacesCurrentToolProviderWorkflowUpdateRequest
{
    /// <summary>
    /// 获取或设置工具名称（不能包含特殊字符）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

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
    /// 获取或设置工作流工具 ID（更新时使用）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflow_tool_id")]
    public string? WorkflowToolId { get; set; }
}
