namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// 获取工作流工具提供者
/// <para>表示 [GET] /console/api/workspaces/current/tool-provider/workflow/get 接口的请求。</para>
/// </summary>
public class ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest : DifyApiRequest
{
    /// <summary>
    /// 获取或设置工作流应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string WorkflowAppId { get; set; } = string.Empty;
}

